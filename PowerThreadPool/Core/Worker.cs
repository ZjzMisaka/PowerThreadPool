using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    internal class Worker : IDisposable
    {
        internal bool _disposed = false;

        internal Thread _thread;

        private string _id;
        internal string ID { get => _id; set => _id = value; }

        internal int _workerState = WorkerStates.Idle;
        internal int _gettedLock = WorkerGettedFlags.Unlocked;
        internal int _workHeld = WorkHeldFlags.NotHeld;

        private IConcurrentPriorityCollection<string> _waitingWorkIDPriorityCollection;
        private ConcurrentDictionary<string, WorkBase> _waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();

        private System.Timers.Timer _timeoutTimer;
        private System.Timers.Timer _killTimer;

        private ManualResetEvent _runSignal = new ManualResetEvent(false);
        private string _workID;
        internal string WorkID { get => _workID; set => _workID = value; }
        private WorkBase _work;
        internal WorkBase Work { get => _work; set => _work = value; }
        private bool _killFlag = false;
        internal int _stealingLock = WorkerStealingFlags.Unlocked;

        private PowerPool _powerPool;

        private bool _longRunning = true;
        internal bool LongRunning { get => _longRunning; set => _longRunning = value; }

        private int _waitingWorkCount = 0;
        internal int WaitingWorkCount
        {
            get
            {
                return _waitingWorkCount;
            }
        }

        internal Worker(PowerPool powerPool)
        {
            InitKillTimer(powerPool);

            _powerPool = powerPool;
            _id = Guid.NewGuid().ToString();

            if (powerPool.PowerPoolOption.QueueType == QueueType.FIFO)
            {
                _waitingWorkIDPriorityCollection = new ConcurrentPriorityQueue<string>();
            }
            else
            {
                _waitingWorkIDPriorityCollection = new ConcurrentPriorityStack<string>();
            }

            _thread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        _runSignal.WaitOne();

                        if (_killFlag)
                        {
                            return;
                        }

                        powerPool.OnWorkStarted(_work.ID);

                        ExecuteResultBase executeResult = null;
                        do
                        {
                            executeResult = ExecuteWork();

                            if (executeResult.Status == Status.Stopped)
                            {
                                powerPool.InvokeWorkStoppedEvent(executeResult);
                            }
                            else
                            {
                                powerPool.InvokeWorkEndedEvent(executeResult);
                            }
                            _work.InvokeCallback(powerPool, executeResult, powerPool.PowerPoolOption);
                        } while (_work.ShouldImmediateRetry(executeResult));

                        if (_work.ShouldRequeue(executeResult))
                        {
                            Interlocked.Increment(ref powerPool._waitingWorkCount);
                            powerPool.SetWork(_work);
                        }
                        else
                        {
                            powerPool.WorkCallbackEnd(_work, executeResult.Status);

                            if (_work.WaitSignal != null)
                            {
                                _work.WaitSignal.Set();
                            }
                        }

                        if (_work.LongRunning)
                        {
                            Interlocked.Decrement(ref powerPool._longRunningWorkerCount);
                            LongRunning = false;
                        }

                        AssignWork();
                    }
                }
                catch (ThreadInterruptedException ex)
                {
                    Interlocked.Exchange(ref _gettedLock, WorkerGettedFlags.Disabled);
                    int origWorkState = Interlocked.Exchange(ref _workerState, WorkerStates.ToBeDisposed);

                    if (_work.LongRunning)
                    {
                        Interlocked.Decrement(ref powerPool._longRunningWorkerCount);
                        LongRunning = false;
                    }

                    if (origWorkState == WorkerStates.Running)
                    {
                        Interlocked.Decrement(ref powerPool._runningWorkerCount);
                    }

                    if (powerPool._aliveWorkerDic.TryRemove(ID, out _))
                    {
                        Interlocked.Decrement(ref powerPool._aliveWorkerCount);
                        powerPool._aliveWorkerList = powerPool._aliveWorkerDic.Values;
                    }
                    if (powerPool._idleWorkerDic.TryRemove(ID, out _))
                    {
                        Interlocked.Decrement(ref powerPool._idleWorkerCount);
                    }

                    ExecuteResultBase executeResult = _work.SetExecuteResult(null, ex, Status.ForceStopped);
                    executeResult.ID = _work.ID;
                    powerPool.InvokeWorkStoppedEvent(executeResult);

                    if (!ex.Data.Contains("ThrowedWhenExecuting"))
                    {
                        ex.Data.Add("ThrowedWhenExecuting", false);
                    }
                    _work.InvokeCallback(powerPool, executeResult, powerPool.PowerPoolOption);

                    powerPool.WorkCallbackEnd(_work, Status.Failed);

                    bool hasWaitingWork = false;
                    IEnumerable<WorkBase> waitingWorkList = _waitingWorkDic.Values;
                    foreach (WorkBase work in waitingWorkList)
                    {
                        powerPool.SetWork(work);
                        hasWaitingWork = true;
                    }

                    if (_work.WaitSignal != null)
                    {
                        _work.WaitSignal.Set();
                    }

                    if (!hasWaitingWork)
                    {
                        powerPool.CheckPoolIdle();
                    }
                }
            });
            _thread.Start();
        }

        private void InitKillTimer(PowerPool powerPool)
        {
            if (powerPool.PowerPoolOption.DestroyThreadOption != null)
            {
                _killTimer = new System.Timers.Timer(powerPool.PowerPoolOption.DestroyThreadOption.KeepAliveTime);
                _killTimer.AutoReset = false;
                _killTimer.Elapsed += OnKillTimerElapsed;
            }
        }

        private ExecuteResultBase ExecuteWork()
        {
            ExecuteResultBase executeResult;
            DateTime runDateTime = DateTime.Now;
            try
            {
                object result = _work.Execute();
                executeResult = _work.SetExecuteResult(result, null, Status.Succeed);
                executeResult.StartDateTime = runDateTime;
            }
            catch (ThreadInterruptedException ex)
            {
                ex.Data.Add("ThrowedWhenExecuting", true);
                throw;
            }
            catch (WorkStopException ex)
            {
                executeResult = _work.SetExecuteResult(null, ex, Status.Stopped);
            }
            catch (Exception ex)
            {
                executeResult = _work.SetExecuteResult(null, ex, Status.Failed);
                _powerPool.OnWorkErrorOccurred(ex, EventArguments.ErrorFrom.WorkLogic, executeResult);
            }
            SpinWait.SpinUntil(() =>
            {
                return _workHeld == WorkHeldFlags.NotHeld;
            });
            _work.Worker = null;
            executeResult.ID = _work.ID;

            return executeResult;
        }

        internal void ForceStop(bool cancelOtherWorks)
        {
            if (_workerState == WorkerStates.Running)
            {
                if (cancelOtherWorks)
                {
                    Cancel();
                }
                _thread.Interrupt();
            }
        }

        internal void WaitForResume()
        {
            _work.PauseSignal.WaitOne();
        }

        internal void Resume()
        {
            foreach (WorkBase workToResume in _waitingWorkDic.Values)
            {
                if (workToResume.IsPausing)
                {
                    workToResume.IsPausing = false;
                    workToResume.PauseSignal.Set();
                }
            }
            if (_work.IsPausing)
            {
                _work.IsPausing = false;
                _work.PauseSignal.Set();
            }
        }

        internal void SetWork(WorkBase work, bool resetted)
        {
            int originalWorkerState;
            _waitingWorkDic[work.ID] = work;
            _powerPool.SetWorkOwner(work);
            _waitingWorkIDPriorityCollection.Set(work.ID, work.WorkPriority);
            work.Worker = this;
            Interlocked.Increment(ref _waitingWorkCount);
            originalWorkerState = Interlocked.CompareExchange(ref _workerState, WorkerStates.Running, WorkerStates.Idle);

            if (_killTimer != null)
            {
                _killTimer.Stop();
            }

            if (!resetted)
            {
                Interlocked.Exchange(ref _gettedLock, WorkerGettedFlags.Unlocked);
            }

            if (originalWorkerState == WorkerStates.Idle)
            {
                Interlocked.Increment(ref _powerPool._runningWorkerCount);
                AssignWork();
            }
        }

        internal List<WorkBase> Steal(int count)
        {
            List<WorkBase> stolenList = new List<WorkBase>();

            bool isContinue = true;
            while (stolenList.Count < count && isContinue)
            {
                isContinue = false;

                string stolenWorkID;
                stolenWorkID = _waitingWorkIDPriorityCollection.Get();

                if (stolenWorkID != null)
                {
                    if (_waitingWorkDic.TryRemove(stolenWorkID, out WorkBase stolenWork))
                    {
                        Interlocked.Decrement(ref _waitingWorkCount);
                        stolenWork.Worker = null;
                        stolenList.Add(stolenWork);

                        isContinue = true;
                    }
                }
            }

            return stolenList;
        }

        private void AssignWork()
        {
            while (true)
            {
                WorkBase work = null;
                string waitingWorkID = _waitingWorkIDPriorityCollection.Get();

                if (waitingWorkID == null && _powerPool._aliveWorkerCount <= _powerPool.PowerPoolOption.MaxThreads)
                {
                    StealWorksFromOtherWorker(ref waitingWorkID, ref work);
                }

                if (waitingWorkID == null)
                {
                    if (TurnToIdle(ref waitingWorkID, ref work))
                    {
                        return;
                    }
                }

                if (work == null && waitingWorkID != null)
                {
                    if (_waitingWorkDic.TryRemove(waitingWorkID, out work))
                    {
                        Interlocked.Decrement(ref _waitingWorkCount);
                    }
                }

                if (work == null)
                {
                    continue;
                }

                if (_killTimer != null)
                {
                    _killTimer.Stop();
                }

                Interlocked.Decrement(ref _powerPool._waitingWorkCount);

                SetWorkToRun(work);

                _runSignal.Set();
                break;
            }
        }

        private void StealWorksFromOtherWorker(ref string waitingWorkID, ref WorkBase work)
        {
            Worker worker = null;
            int max = 0;
            foreach (Worker runningWorker in _powerPool._aliveWorkerList)
            {
                if (runningWorker._workerState != WorkerStates.Running || runningWorker.ID == ID)
                {
                    continue;
                }

                int waitingWorkCountTemp = runningWorker.WaitingWorkCount;
                if (waitingWorkCountTemp >= 2 && waitingWorkCountTemp > max)
                {
                    if (Interlocked.CompareExchange(ref runningWorker._stealingLock, WorkerStealingFlags.Locked, WorkerStealingFlags.Unlocked) == WorkerStealingFlags.Locked)
                    {
                        continue;
                    }
                    if (worker != null)
                    {
                        Interlocked.Exchange(ref worker._stealingLock, WorkerStealingFlags.Unlocked);
                    }
                    max = waitingWorkCountTemp;
                    worker = runningWorker;
                }
            }
            if (worker != null)
            {
                int count = max / 2;
                List<WorkBase> stolenWorkList = null;
                if (count > 0)
                {
                    stolenWorkList = worker.Steal(count);
                }
                Interlocked.Exchange(ref worker._stealingLock, WorkerStealingFlags.Unlocked);
                if (stolenWorkList != null)
                {
                    foreach (WorkBase stolenWork in stolenWorkList)
                    {
                        if (waitingWorkID == null)
                        {
                            waitingWorkID = stolenWork.ID;
                            work = stolenWork;
                            work.Worker = this;
                        }
                        else
                        {
                            SetWork(stolenWork, true);
                        }
                    }
                }
            }
        }

        private bool TurnToIdle(ref string waitingWorkID, ref WorkBase work)
        {
            SpinWait.SpinUntil(() =>
            {
                int gettedLockOrig = Interlocked.CompareExchange(ref _gettedLock, WorkerGettedFlags.ToBeDisabled, WorkerGettedFlags.Unlocked);
                return (gettedLockOrig == WorkerGettedFlags.Unlocked);
            });

            if (!_waitingWorkDic.IsEmpty)
            {
                waitingWorkID = _waitingWorkIDPriorityCollection.Get();
                if (waitingWorkID != null)
                {
                    if (_waitingWorkDic.TryRemove(waitingWorkID, out work))
                    {
                        Interlocked.Decrement(ref _waitingWorkCount);
                    }
                }

                Interlocked.CompareExchange(ref _gettedLock, WorkerGettedFlags.Unlocked, WorkerGettedFlags.ToBeDisabled);

                return false;
            }
            else
            {
                _runSignal.Reset();

                PowerPoolOption powerPoolOption = _powerPool.PowerPoolOption;
                if (powerPoolOption.DestroyThreadOption != null && _powerPool.IdleWorkerCount > powerPoolOption.DestroyThreadOption.MinThreads)
                {
                    _killTimer.Interval = _powerPool.PowerPoolOption.DestroyThreadOption.KeepAliveTime;
                    _killTimer.Start();
                }

                Interlocked.Decrement(ref _powerPool._runningWorkerCount);

                Interlocked.Exchange(ref _workerState, WorkerStates.Idle);
                Interlocked.CompareExchange(ref _gettedLock, WorkerGettedFlags.Unlocked, WorkerGettedFlags.ToBeDisabled);

                if (_powerPool._idleWorkerDic.TryAdd(ID, this))
                {
                    Interlocked.Increment(ref _powerPool._idleWorkerCount);
                    _powerPool._idleWorkerQueue.Enqueue(ID);
                }

                _powerPool.CheckPoolIdle();

                return true;
            }
        }

        private void SetWorkToRun(WorkBase work)
        {
            TimeoutOption workTimeoutOption = work.WorkTimeoutOption;
            if (workTimeoutOption != null)
            {
                System.Timers.Timer timer = new System.Timers.Timer(workTimeoutOption.Duration);
                timer.AutoReset = false;
                timer.Elapsed += (s, e) =>
                {
                    _powerPool.OnWorkTimedOut(_powerPool, new WorkTimedOutEventArgs() { ID = _workID });
                    _powerPool.Stop(_workID, workTimeoutOption.ForceStop);
                };
                timer.Start();

                _timeoutTimer = timer;
            }

            _work = work;
            _workID = work.ID;
            _longRunning = work.LongRunning;

            _thread.Priority = work.ThreadPriority;
            _thread.IsBackground = work.IsBackground;
        }

        private void OnKillTimerElapsed(object s, ElapsedEventArgs e)
        {
            if (_powerPool.IdleWorkerCount > _powerPool.PowerPoolOption.DestroyThreadOption.MinThreads)
            {
                SpinWait.SpinUntil(() =>
                {
                    int gettedStatus = Interlocked.CompareExchange(ref _gettedLock, WorkerGettedFlags.Disabled, WorkerGettedFlags.Unlocked);
                    return (gettedStatus == WorkerGettedFlags.Unlocked || gettedStatus == WorkerGettedFlags.Disabled);
                });

                if (Interlocked.CompareExchange(ref _workerState, WorkerStates.ToBeDisposed, WorkerStates.Idle) == WorkerStates.Idle)
                {
                    RemoveSelf();
                }
                else
                {
                    Interlocked.CompareExchange(ref _gettedLock, WorkerGettedFlags.Unlocked, WorkerGettedFlags.Disabled);
                }
            }

            _killTimer.Stop();
        }

        private void RemoveSelf()
        {
            if (_powerPool._idleWorkerDic.TryRemove(ID, out _))
            {
                Interlocked.Decrement(ref _powerPool._idleWorkerCount);
            }
            if (_powerPool._aliveWorkerDic.TryRemove(ID, out _))
            {
                Interlocked.Decrement(ref _powerPool._aliveWorkerCount);
                _powerPool._aliveWorkerList = _powerPool._aliveWorkerDic.Values;
            }
            Kill();
        }

        internal void Kill()
        {
            _killFlag = true;
            _runSignal.Set();
        }

        internal void PauseTimer()
        {
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Stop();
            }
        }

        internal void ResumeTimer()
        {
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Start();
            }
        }

        internal void Cancel()
        {
            IEnumerable<string> waitingWorkIDList = _waitingWorkDic.Keys;
            foreach (string id in waitingWorkIDList)
            {
                Cancel(id);
            }
        }

        internal bool Cancel(string id)
        {
            if (_waitingWorkDic.TryRemove(id, out _))
            {
                ExecuteResultBase executeResult = _work.SetExecuteResult(null, null, Status.Canceled);
                executeResult.ID = id;

                _powerPool.InvokeWorkCanceledEvent(executeResult);
                _work.InvokeCallback(_powerPool, executeResult, _powerPool.PowerPoolOption);

                Interlocked.Decrement(ref _waitingWorkCount);
                Interlocked.Decrement(ref _powerPool._waitingWorkCount);
                return true;
            }
            return false;
        }

        internal bool IsCancellationRequested()
        {
            return _work.ShouldStop;
        }

        internal bool IsPausing()
        {
            return _work.IsPausing;
        }

        /// <summary>
        /// Dispose the instance. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the instance
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    RemoveSelf();

                    _runSignal.Dispose();
                }

                _disposed = true;
            }
        }

        ~Worker()
        {
            Dispose(false);
        }
    }
}
