using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
        internal InterlockedFlag<CanDispose> CanDispose { get; } = Constants.CanDispose.Allowed;
        internal InterlockedFlag<CanForceStop> CanForceStop { get; } = Constants.CanForceStop.Allowed;

        internal Thread _thread;

        internal int ID { get; set; }

        internal InterlockedFlag<WorkerStates> WorkerState { get; } = WorkerStates.Idle;
        internal InterlockedFlag<CanGetWork> CanGetWork { get; } = Constants.CanGetWork.Allowed;
        internal InterlockedFlag<WorkHeldStates> WorkHeldState { get; } = WorkHeldStates.NotHeld;
        internal InterlockedFlag<WorkStealability> WorkStealability { get; } = Constants.WorkStealability.Allowed;

        private IStealablePriorityCollection<string> _waitingWorkIDPriorityCollection;
        private ConcurrentDictionary<string, WorkBase> _waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();

        private DeferredActionTimer _timeoutTimer;
        private DeferredActionTimer _killTimer;

        private ManualResetEvent _runSignal = new ManualResetEvent(false);

        internal string WorkID => Work.ID;

        internal WorkBase Work { get; set; }

        private bool _killFlag = false;

        private PowerPool _powerPool;

        internal bool LongRunning { get; set; } = true;

        private int _waitingWorkCount = 0;

        internal int WaitingWorkCount => _waitingWorkCount;

        internal Worker(PowerPool powerPool)
        {
            _powerPool = powerPool;

            _killTimer = new DeferredActionTimer(() => { TryDisposeSelf(true); });
            _timeoutTimer = new DeferredActionTimer();

            _waitingWorkIDPriorityCollection = QueueFactory();

            _thread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        SetKillTimer();

                        _runSignal.WaitOne();

                        if (_killFlag)
                        {
                            return;
                        }

                        ExecuteWork();

                        if (Work.LongRunning)
                        {
                            Interlocked.Decrement(ref _powerPool._longRunningWorkerCount);
                            LongRunning = false;
                        }

                        AssignWork();
                        // May be disposed at WorkerCountOutOfRange().
                        if (CanDispose == Constants.CanDispose.NotAllowed)
                        {
                            return;
                        }
                    }
                }
                catch (ThreadInterruptedException ex)
                {
                    ThreadInterrupted(ex);
                }
            });
            ID = _thread.ManagedThreadId;
            _thread.Start();
        }

        private IStealablePriorityCollection<string> QueueFactory()
        {
            if (_powerPool.PowerPoolOption.CustomQueueFactory != null)
            {
                return _powerPool.PowerPoolOption.CustomQueueFactory();
            }
            else if (_powerPool.PowerPoolOption.QueueType == QueueType.FIFO)
            {
                return new ConcurrentStealablePriorityQueue<string>();
            }
            else
            {
                return new ConcurrentStealablePriorityStack<string>();
            }
        }

        private void ExecuteWork()
        {
            _powerPool.OnWorkStarted(Work.ID);

            ExecuteResultBase executeResult;
            do
            {
                executeResult = ExecuteMain();

                if (executeResult.Status == Status.Stopped)
                {
                    _powerPool.InvokeWorkStoppedEvent(executeResult);
                }
                else
                {
                    _powerPool.InvokeWorkEndedEvent(executeResult);
                }
                Work.InvokeCallback(_powerPool, executeResult, _powerPool.PowerPoolOption);
            } while (Work.ShouldImmediateRetry(executeResult));

            if (Work.ShouldRequeue(executeResult))
            {
                Interlocked.Increment(ref _powerPool._waitingWorkCount);
                _powerPool.SetWork(Work);
            }
            else
            {
                _powerPool.WorkCallbackEnd(Work, executeResult.Status);

                Work.IsDone = true;

                if (Work.WaitSignal != null)
                {
                    Work.WaitSignal.Set();
                }
            }
        }

        private void ThreadInterrupted(ThreadInterruptedException ex)
        {
            CanGetWork.InterlockedValue = Constants.CanGetWork.Disabled;

            WorkerStates origWorkState = WorkerState.InterlockedValue;
            WorkerState.InterlockedValue = WorkerStates.ToBeDisposed;

            if (Work.LongRunning)
            {
                Interlocked.Decrement(ref _powerPool._longRunningWorkerCount);
                LongRunning = false;
            }

            if (origWorkState == WorkerStates.Running)
            {
                Interlocked.Decrement(ref _powerPool._runningWorkerCount);
                _powerPool.InvokeRunningWorkerCountChangedEvent(false);
            }

            if (_powerPool._aliveWorkerDic.TryRemove(ID, out _))
            {
                Interlocked.Decrement(ref _powerPool._aliveWorkerCount);
                _powerPool._aliveWorkerList = _powerPool._aliveWorkerDic.Values;
            }
            if (_powerPool._idleWorkerDic.TryRemove(ID, out _))
            {
                Interlocked.Decrement(ref _powerPool._idleWorkerCount);
            }

            ExecuteResultBase executeResult = Work.SetExecuteResult(_powerPool, null, ex, Status.ForceStopped);
            executeResult.ID = Work.ID;
            _powerPool.InvokeWorkStoppedEvent(executeResult);

            if (!ex.Data.Contains("ThrowedWhenExecuting"))
            {
                ex.Data.Add("ThrowedWhenExecuting", false);
            }
            Work.InvokeCallback(_powerPool, executeResult, _powerPool.PowerPoolOption);

            _powerPool.WorkCallbackEnd(Work, Status.Failed);

            bool hasWaitingWork = RequeueAllWaitingWork();
            Work.IsDone = true;

            if (Work.WaitSignal != null)
            {
                Work.WaitSignal.Set();
            }

            if (!hasWaitingWork)
            {
                _powerPool.CheckPoolIdle();
            }

            Dispose();
        }

        private void WorkerCountOutOfRange()
        {
            if (_powerPool._canDeleteRedundantWorker.TrySet(CanDeleteRedundantWorker.NotAllowed, CanDeleteRedundantWorker.Allowed))
            {
                CanGetWork.InterlockedValue = Constants.CanGetWork.Disabled;

                WorkerState.InterlockedValue = WorkerStates.ToBeDisposed;

                if (_powerPool._aliveWorkerDic.TryRemove(ID, out _))
                {
                    Interlocked.Decrement(ref _powerPool._aliveWorkerCount);
                    _powerPool._aliveWorkerList = _powerPool._aliveWorkerDic.Values;
                }

                bool hasWaitingWork = RequeueAllWaitingWork();

                Interlocked.Decrement(ref _powerPool._runningWorkerCount);
                _powerPool.InvokeRunningWorkerCountChangedEvent(false);

                if (!hasWaitingWork)
                {
                    _powerPool.CheckPoolIdle();
                }

                Dispose();

                _powerPool._canDeleteRedundantWorker.InterlockedValue = CanDeleteRedundantWorker.Allowed;
            }
        }

        private bool RequeueAllWaitingWork()
        {
            bool hasWaitingWork = false;
            string workID;
            while ((workID = _waitingWorkIDPriorityCollection.Get()) != null)
            {
                if (_waitingWorkDic.TryRemove(workID, out WorkBase work))
                {
                    _powerPool.SetWork(work);
                    hasWaitingWork = true;
                }
            }
            return hasWaitingWork;
        }

        private void SetKillTimer()
        {
            if (_powerPool.PowerPoolOption.DestroyThreadOption != null && _powerPool.PowerPoolOption.DestroyThreadOption.KeepAliveTime != 0)
            {
                _killTimer.Set(_powerPool.PowerPoolOption.DestroyThreadOption.KeepAliveTime);
            }
            else if (_powerPool.PowerPoolOption.DestroyThreadOption == null)
            {
                _killTimer.Cancel();
            }
        }

        private ExecuteResultBase ExecuteMain()
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
                _powerPool.OnWorkErrorOccurred(ex, ErrorFrom.WorkLogic, executeResult);
            }
            // During the WorkGuard.Freeze logic, the WorkHeldState will be set to WorkHeldStates.Held
            // to temporarily prevent the executing work from allowing the worker to switch to the next work 
            // when the current work is completed. The WorkGuard.Freeze logic is non-blocking and executes quickly,
            // so spinning will not consume a lot of CPU resources. 
            SpinWait.SpinUntil(() => WorkHeldState == WorkHeldStates.NotHeld);
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
            else
            {
                CanForceStop.InterlockedValue = Constants.CanForceStop.Allowed;
            }
        }

        internal void WaitForResume()
        {
            Work.PauseSignal.WaitOne();
        }

        internal void Resume()
        {
            IEnumerable<WorkBase> waitingWorkList = _waitingWorkDic.Values;
            foreach (WorkBase workToResume in waitingWorkList)
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

        internal void SetWork(WorkBase work, bool reset)
        {
            _waitingWorkDic[work.ID] = work;
            _powerPool.SetWorkOwner(work);
            _waitingWorkIDPriorityCollection.Set(work.ID, work.WorkPriority);
            work.Worker = this;
            Interlocked.Increment(ref _waitingWorkCount);
            WorkerState.TrySet(WorkerStates.Running, WorkerStates.Idle, out WorkerStates originalWorkerState);

            _killTimer.Cancel();

            if (!reset)
            {
                CanGetWork.InterlockedValue = Constants.CanGetWork.Allowed;
            }

            if (originalWorkerState == WorkerStates.Idle)
            {
                Interlocked.Increment(ref _powerPool._runningWorkerCount);
                _powerPool.InvokeRunningWorkerCountChangedEvent(true);
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
                stolenWorkID = _waitingWorkIDPriorityCollection.Steal();

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

                if (_powerPool.AliveWorkerCount - _powerPool.LongRunningWorkerCount > _powerPool.PowerPoolOption.MaxThreads)
                {
                    WorkerCountOutOfRange();

                    return;
                }

                string waitingWorkID = _waitingWorkIDPriorityCollection.Get();
                if (waitingWorkID == null && _powerPool.AliveWorkerCount <= _powerPool.PowerPoolOption.MaxThreads)
                {
                    List<WorkBase> stolenWorkList = StealWorksFromOtherWorker();
                    SetStolenWorkList(ref waitingWorkID, ref work, stolenWorkList, false);
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

                _killTimer.Cancel();

                Interlocked.Decrement(ref _powerPool._waitingWorkCount);

                SetWorkToRun(work);

                _runSignal.Set();
                break;
            }
        }

        internal bool TryAssignWorkForNewWorker()
        {
            string waitingWorkID = null;
            WorkBase work = null;

            List<WorkBase> stolenWorkList = StealWorksFromOtherWorker();
            return SetStolenWorkList(ref waitingWorkID, ref work, stolenWorkList, true);
        }

        private List<WorkBase> StealWorksFromOtherWorker()
        {
            Worker worker = null;
            int max = 0;
            IEnumerable<Worker> workers = _powerPool._aliveWorkerList;
            foreach (Worker runningWorker in workers)
            {
                if (runningWorker.WorkerState != WorkerStates.Running || runningWorker.ID == ID)
                {
                    continue;
                }

                int waitingWorkCountTemp = runningWorker.WaitingWorkCount;
                if (waitingWorkCountTemp >= 1 && waitingWorkCountTemp > max)
                {
                    if (!runningWorker.WorkStealability.TrySet(Constants.WorkStealability.NotAllowed, Constants.WorkStealability.Allowed))
                    {
                        continue;
                    }
                    if (worker != null)
                    {
                        worker.WorkStealability.InterlockedValue = Constants.WorkStealability.Allowed;
                    }
                    max = waitingWorkCountTemp;
                    worker = runningWorker;
                }
            }
            if (worker != null)
            {
                int count = max == 1 ? 1 : max / 2;
                List<WorkBase> stolenWorkList = null;
                if (count > 0)
                {
                    stolenWorkList = worker.Steal(count);
                }
                worker.WorkStealability.InterlockedValue = Constants.WorkStealability.Allowed;
                return stolenWorkList;
            }
            return null;
        }

        private bool SetStolenWorkList(ref string waitingWorkID, ref WorkBase work, List<WorkBase> stolenWorkList, bool newWorker)
        {
            bool res = false;
            if (stolenWorkList != null)
            {
                foreach (WorkBase stolenWork in stolenWorkList)
                {
                    res = true;
                    if (!newWorker && waitingWorkID == null)
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
            return res;
        }

        private bool TurnToIdle(ref string waitingWorkID, ref WorkBase work)
        {
            // The time that CanGetWork is in other states is very short; these logics are non-blocking and execute quickly,
            // so spinning will not consume a lot of CPU resources.
            SpinWait.SpinUntil(() => CanGetWork.TrySet(Constants.CanGetWork.ToBeDisabled, Constants.CanGetWork.Allowed));

            waitingWorkID = _waitingWorkIDPriorityCollection.Get();
            if (waitingWorkID != null)
            {
                if (_waitingWorkDic.TryRemove(waitingWorkID, out work))
                {
                    Interlocked.Decrement(ref _waitingWorkCount);
                }

                CanGetWork.TrySet(Constants.CanGetWork.Allowed, Constants.CanGetWork.ToBeDisabled);

                return false;
            }
            else
            {
                _runSignal.Reset();

                PowerPoolOption powerPoolOption = _powerPool.PowerPoolOption;

                Interlocked.Decrement(ref _powerPool._runningWorkerCount);
                _powerPool.InvokeRunningWorkerCountChangedEvent(false);

                if (powerPoolOption.DestroyThreadOption != null && _powerPool.PowerPoolOption.DestroyThreadOption.KeepAliveTime == 0 && _powerPool.IdleWorkerCount >= powerPoolOption.DestroyThreadOption.MinThreads)
                {
                    CanGetWork.TrySet(Constants.CanGetWork.Disabled, Constants.CanGetWork.ToBeDisabled);
                    TryDisposeSelf(false);
                }
                else
                {
                    if (powerPoolOption.DestroyThreadOption != null && _powerPool.IdleWorkerCount >= powerPoolOption.DestroyThreadOption.MinThreads)
                    {
                        _killTimer.Set(_powerPool.PowerPoolOption.DestroyThreadOption.KeepAliveTime);
                    }

                    WorkerState.InterlockedValue = WorkerStates.Idle;

                    CanGetWork.TrySet(Constants.CanGetWork.Allowed, Constants.CanGetWork.ToBeDisabled);

                    if (_powerPool._idleWorkerDic.TryAdd(ID, this))
                    {
                        Interlocked.Increment(ref _powerPool._idleWorkerCount);
                        _powerPool._idleWorkerQueue.Enqueue(ID);
                    }
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
                _timeoutTimer.Set(workTimeoutOption.Duration, () =>
                {
                    _powerPool.OnWorkTimedOut(_powerPool, new WorkTimedOutEventArgs() { ID = WorkID });
                    _powerPool.Stop(WorkID, workTimeoutOption.ForceStop);
                });
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

        internal void TryDisposeSelf(bool isIdle)
        {
            if (isIdle ? _powerPool.IdleWorkerCount > _powerPool.PowerPoolOption.DestroyThreadOption.MinThreads : _powerPool.IdleWorkerCount >= _powerPool.PowerPoolOption.DestroyThreadOption.MinThreads)
            {
                // ① There is a possibility that a worker may still obtain and execute work between the 
                // time the _killTimer triggers OnKillTimerElapsed and when CanGetWork is set to Disabled. 
                SpinWait.SpinUntil(() =>
                {
                    CanGetWork.TrySet(Constants.CanGetWork.Disabled, Constants.CanGetWork.Allowed, out CanGetWork origValue);
                    // If situation ① occurs and _killTimer.Stop() has not yet been executed, the current state 
                    // of CanGetWork will be Disabled, although this is an extremely rare case.
                    // Therefore, SpinUntil will exit either when CanGetWork is successfully set from Allowed to Disabled, 
                    // or if the current state of CanGetWork is already Disabled.
                    return origValue == Constants.CanGetWork.Allowed || origValue == Constants.CanGetWork.Disabled;
                });

                if (!isIdle || WorkerState.TrySet(WorkerStates.ToBeDisposed, WorkerStates.Idle))
                {
                    Dispose();
                    // Although reaching this point means that WorkerState has been set from Idle to ToBeDisposed, 
                    // indicating that no work is currently running, there is still a possibility that situation ① has occurred, 
                    // and the work may have finished executing before WorkerState.TrySet was called.
                    // It is also an extremely rare case, but since this case is harmless, just ignore it.
                    return;
                }

                // Reaching this point means that WorkerState was not set from Idle to ToBeDisposed, 
                // indicating that situation ① has occurred and that work is currently running. 
                // Therefore, reset the CanGetWork. This is also an extremely rare case. 
                CanGetWork.TrySet(Constants.CanGetWork.Allowed, Constants.CanGetWork.Disabled);
            }
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
            _timeoutTimer.Pause();
        }

        internal void ResumeTimer()
        {
            _timeoutTimer.Resume();
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
            if (_waitingWorkDic.TryRemove(id, out WorkBase work))
            {
                ExecuteResultBase executeResult = work.SetExecuteResult(_powerPool, null, null, Status.Canceled);
                executeResult.ID = id;

                _powerPool.InvokeWorkCanceledEvent(executeResult);
                work.InvokeCallback(_powerPool, executeResult, _powerPool.PowerPoolOption);
                _powerPool.WorkCallbackEnd(work, Status.Canceled);

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
        public void DisposeWithJoin()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the instance. 
        /// </summary>
        public void Dispose()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the instance
        /// </summary>
        /// <param name="join"></param>
        protected virtual void Dispose(bool join)
        {
            if (CanDispose.TrySet(Constants.CanDispose.NotAllowed, Constants.CanDispose.Allowed))
            {
                RemoveSelf();

                if (join)
                {
                    Kill();
                    _thread.Join();
                }

                _runSignal.Dispose();
                _timeoutTimer.Dispose();
                _killTimer.Dispose();
            }
        }
    }
}
