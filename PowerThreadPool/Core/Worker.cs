using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Helpers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    internal class Worker : IDisposable
    {
        internal bool _disposed = false;
        internal bool _workerLoopEnded = false;

        internal Thread _thread;

        internal Guid ID { get; set; }

        internal InterlockedFlag<WorkerStates> WorkerState { get; set; } = WorkerStates.Idle;

        internal InterlockedFlag<WorkerGettedFlags> GettedLock { get; set; } = WorkerGettedFlags.Unlocked;

        internal InterlockedFlag<WorkHeldFlags> WorkHeld { get; set; } = WorkHeldFlags.NotHeld;

        private IConcurrentPriorityCollection<string> _waitingWorkIDPriorityCollection;
        private ConcurrentDictionary<string, WorkBase> _waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();

        private System.Timers.Timer _timeoutTimer;
        private System.Timers.Timer _killTimer;

        private ManualResetEvent _runSignal = new ManualResetEvent(false);

        internal string WorkID => Work.ID;

        internal WorkBase Work { get; set; }

        private bool _killFlag = false;

        internal InterlockedFlag<WorkerStealingFlags> StealingLock { get; set; } = WorkerStealingFlags.Unlocked;

        private PowerPool _powerPool;

        internal bool LongRunning { get; set; } = true;

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
            ID = Guid.NewGuid();

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
                            _workerLoopEnded = true;
                            return;
                        }

                        powerPool.OnWorkStarted(Work.ID);

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
                            Work.InvokeCallback(powerPool, executeResult, powerPool.PowerPoolOption);
                        } while (Work.ShouldImmediateRetry(executeResult));

                        if (Work.ShouldRequeue(executeResult))
                        {
                            Interlocked.Increment(ref powerPool._waitingWorkCount);
                            powerPool.SetWork(Work);
                        }
                        else
                        {
                            powerPool.WorkCallbackEnd(Work, executeResult.Status);

                            if (Work.WaitSignal != null)
                            {
                                Work.WaitSignal.Set();
                            }
                        }

                        if (Work.LongRunning)
                        {
                            Interlocked.Decrement(ref powerPool._longRunningWorkerCount);
                            LongRunning = false;
                        }

                        AssignWork();
                    }
                }
                catch (ThreadInterruptedException ex)
                {
                    GettedLock.InterlockedValue = WorkerGettedFlags.Disabled;

                    WorkerStates origWorkState = WorkerState.InterlockedValue;
                    WorkerState.InterlockedValue = WorkerStates.ToBeDisposed;

                    if (Work.LongRunning)
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

                    ExecuteResultBase executeResult = Work.SetExecuteResult(powerPool, null, ex, Status.ForceStopped);
                    executeResult.ID = Work.ID;
                    powerPool.InvokeWorkStoppedEvent(executeResult);

                    if (!ex.Data.Contains("ThrowedWhenExecuting"))
                    {
                        ex.Data.Add("ThrowedWhenExecuting", false);
                    }
                    Work.InvokeCallback(powerPool, executeResult, powerPool.PowerPoolOption);

                    powerPool.WorkCallbackEnd(Work, Status.Failed);

                    bool hasWaitingWork = false;
                    IEnumerable<WorkBase> waitingWorkList = _waitingWorkDic.Values;
                    foreach (WorkBase work in waitingWorkList)
                    {
                        powerPool.SetWork(work);
                        hasWaitingWork = true;
                    }

                    if (Work.WaitSignal != null)
                    {
                        Work.WaitSignal.Set();
                    }

                    if (!hasWaitingWork)
                    {
                        powerPool.CheckPoolIdle();
                    }

                    _workerLoopEnded = true;
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
            DateTime runDateTime = DateTime.UtcNow;
            try
            {
                Interlocked.Increment(ref _powerPool._startCount);
                Interlocked.Add(ref _powerPool._queueTime, (long)(runDateTime - Work.QueueDateTime).TotalMilliseconds);
                object result = Work.Execute();
                executeResult = Work.SetExecuteResult(_powerPool, result, null, Status.Succeed);
                executeResult.StartDateTime = runDateTime;
            }
            catch (ThreadInterruptedException ex)
            {
                ex.Data.Add("ThrowedWhenExecuting", true);
                throw;
            }
            catch (WorkStopException ex)
            {
                executeResult = Work.SetExecuteResult(_powerPool, null, ex, Status.Stopped);
            }
            catch (Exception ex)
            {
                executeResult = Work.SetExecuteResult(_powerPool, null, ex, Status.Failed);
                _powerPool.OnWorkErrorOccurred(ex, EventArguments.ErrorFrom.WorkLogic, executeResult);
            }
            SpinWait.SpinUntil(() => WorkHeld == WorkHeldFlags.NotHeld);
            Work.Worker = null;
            executeResult.ID = Work.ID;

            return executeResult;
        }

        internal void ForceStop(bool cancelOtherWorks)
        {
            if (WorkerState == WorkerStates.Running)
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
            Work.PauseSignal.WaitOne();
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
            if (Work.IsPausing)
            {
                Work.IsPausing = false;
                Work.PauseSignal.Set();
            }
        }

        internal void SetWork(WorkBase work, bool resetted)
        {
            _waitingWorkDic[work.ID] = work;
            _powerPool.SetWorkOwner(work);
            _waitingWorkIDPriorityCollection.Set(work.ID, work.WorkPriority);
            work.Worker = this;
            Interlocked.Increment(ref _waitingWorkCount);
            WorkerState.TrySet(WorkerStates.Running, WorkerStates.Idle, out WorkerStates originalWorkerState);

            if (_killTimer != null)
            {
                _killTimer.Stop();
            }

            if (!resetted)
            {
                GettedLock.InterlockedValue = WorkerGettedFlags.Unlocked;
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
                if (runningWorker.WorkerState != WorkerStates.Running || runningWorker.ID == ID)
                {
                    continue;
                }

                int waitingWorkCountTemp = runningWorker.WaitingWorkCount;
                if (waitingWorkCountTemp >= 2 && waitingWorkCountTemp > max)
                {
                    if (!runningWorker.StealingLock.TrySet(WorkerStealingFlags.Locked, WorkerStealingFlags.Unlocked))
                    {
                        continue;
                    }
                    if (worker != null)
                    {
                        worker.StealingLock.InterlockedValue = WorkerStealingFlags.Unlocked;
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
                worker.StealingLock.InterlockedValue = WorkerStealingFlags.Unlocked;
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
            SpinWait.SpinUntil(() => GettedLock.TrySet(WorkerGettedFlags.ToBeDisabled, WorkerGettedFlags.Unlocked));

            waitingWorkID = _waitingWorkIDPriorityCollection.Get();
            if (waitingWorkID != null)
            {
                if (_waitingWorkDic.TryRemove(waitingWorkID, out work))
                {
                    Interlocked.Decrement(ref _waitingWorkCount);
                }

                GettedLock.TrySet(WorkerGettedFlags.Unlocked, WorkerGettedFlags.ToBeDisabled);

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

                WorkerState.InterlockedValue = WorkerStates.Idle;

                GettedLock.TrySet(WorkerGettedFlags.Unlocked, WorkerGettedFlags.ToBeDisabled);

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
                    _powerPool.OnWorkTimedOut(_powerPool, new WorkTimedOutEventArgs() { ID = WorkID });
                    _powerPool.Stop(WorkID, workTimeoutOption.ForceStop);
                };
                timer.Start();

                _timeoutTimer = timer;
            }

            Work = work;
            LongRunning = work.LongRunning;

            if (_thread.Priority != work.ThreadPriority)
            {
                _thread.Priority = work.ThreadPriority;
            }
            if (_thread.IsBackground != work.IsBackground)
            {
                _thread.IsBackground = work.IsBackground;
            }
        }

        private void OnKillTimerElapsed(object s, ElapsedEventArgs e)
        {
            if (_powerPool.IdleWorkerCount > _powerPool.PowerPoolOption.DestroyThreadOption.MinThreads)
            {
                SpinWait.SpinUntil(() =>
                {
                    GettedLock.TrySet(WorkerGettedFlags.Disabled, WorkerGettedFlags.Unlocked, out WorkerGettedFlags origValue);
                    return origValue == WorkerGettedFlags.Unlocked || origValue == WorkerGettedFlags.Disabled;
                });

                if (WorkerState.TrySet(WorkerStates.ToBeDisposed, WorkerStates.Idle))
                {
                    RemoveSelf();
                }
                else
                {
                    GettedLock.TrySet(WorkerGettedFlags.Unlocked, WorkerGettedFlags.Disabled);
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
                ExecuteResultBase executeResult = Work.SetExecuteResult(_powerPool, null, null, Status.Canceled);
                executeResult.ID = id;

                _powerPool.InvokeWorkCanceledEvent(executeResult);
                Work.InvokeCallback(_powerPool, executeResult, _powerPool.PowerPoolOption);

                Interlocked.Decrement(ref _waitingWorkCount);
                Interlocked.Decrement(ref _powerPool._waitingWorkCount);
                return true;
            }
            return false;
        }

        internal bool IsCancellationRequested()
        {
            return Work.ShouldStop;
        }

        internal bool IsPausing()
        {
            return Work.IsPausing;
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

                    SpinWait.SpinUntil(() => _workerLoopEnded);
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
