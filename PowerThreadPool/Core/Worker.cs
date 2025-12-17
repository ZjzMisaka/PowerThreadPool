using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Helpers;
using PowerThreadPool.Helpers.Asynchronous;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Helpers.Timers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    internal class Worker : IDisposable
    {
        private StatusPingPongChecker _statusPingPongChecker = new StatusPingPongChecker();

        internal InterlockedFlag<CanDispose> CanDispose { get; } = Constants.CanDispose.Allowed;
        internal InterlockedFlag<CanForceStop> CanForceStop { get; } = Constants.CanForceStop.Allowed;

        internal Thread _thread;

        internal int ID { get; set; }

        internal InterlockedFlag<WorkerStates> WorkerState { get; } = WorkerStates.Idle;
        internal InterlockedFlag<CanGetWork> CanGetWork { get; } = Constants.CanGetWork.Allowed;
        internal InterlockedFlag<WorkHeldStates> WorkHeldState { get; } = WorkHeldStates.NotHeld;
        internal InterlockedFlag<WorkStealability> WorkStealability { get; } = Constants.WorkStealability.Allowed;

        private ConcurrentQueue<WorkBase> _workInbox;

        private IStealablePriorityCollection<WorkItemBase> _waitingWorkPriorityCollection;

        private DeferredActionTimer _timeoutTimer;
        private DeferredActionTimer _killTimer;

        private ManualResetEvent _runSignal = new ManualResetEvent(false);

        internal WorkID WorkID => Work.ID;

        internal WorkBase Work { get; set; }

        private bool _killFlag = false;

        internal bool _isHelper = false;
        internal Worker _helpingWorker;
        internal Worker _helperWorker;
        internal Worker _baseHelpingWorker;

        private PowerPool _powerPool;

        internal bool LongRunning { get; set; } = true;

        internal int _waitingWorkCount = 0;

        internal int WaitingWorkCount => _waitingWorkCount;

#pragma warning disable CS0618
        private bool EnforceDequeOwnership => _powerPool.PowerPoolOption.EnforceDequeOwnership || _waitingWorkPriorityCollection.EnforceDequeOwnership;
#pragma warning restore CS0618

        internal Worker(PowerPool powerPool)
        {
            _powerPool = powerPool;

            _waitingWorkPriorityCollection = QueueFactory();

            if (EnforceDequeOwnership)
            {
                _workInbox = new ConcurrentQueue<WorkBase>();
            }

            _thread = new Thread(() =>
            {
                WorkerContext.s_current = this;

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

                        AssignWork(null);
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
                finally
                {
                    WorkerContext.s_current = null;
                }
            });
            ID = _thread.ManagedThreadId;
            _thread.IsBackground = true;
            _thread.Start();
        }

        internal Worker()
        {
            _isHelper = true;
        }

        internal void CheckIsPingedPong()
        {
            _statusPingPongChecker.CheckIsPingedPong();
        }

        internal void RunHelp(PowerPool powerPool, WorkBase work)
        {
            _powerPool = powerPool;
            ID = Thread.CurrentThread.ManagedThreadId;
            _helpingWorker = null;
            WorkerState.InterlockedValue = WorkerStates.Running;
            if (_powerPool.GetCurrentThreadWorker(out _helpingWorker))
            {
                if (_helpingWorker._isHelper)
                {
                    _baseHelpingWorker = _helpingWorker._baseHelpingWorker;
                }
                else
                {
                    _baseHelpingWorker = _helpingWorker;
                }
            }

            if (_baseHelpingWorker != null)
            {
                _baseHelpingWorker._helperWorker = this;
            }

            Worker workerTemp = WorkerContext.s_current;
            WorkerContext.s_current = this;
            work.Worker = this;
            Interlocked.Decrement(ref _powerPool._waitingWorkCount);
            SetWorkToRun(work);
            Work = work;
            ExecuteWork();
            WorkerContext.s_current = workerTemp;

            WorkerState.InterlockedValue = WorkerStates.Idle;

            if (_baseHelpingWorker != null)
            {
                _baseHelpingWorker._helperWorker = null;
                _baseHelpingWorker = null;
            }
            _helpingWorker = null;
        }

        private IStealablePriorityCollection<WorkItemBase> QueueFactory()
        {
            if (_powerPool.PowerPoolOption.CustomQueueFactory != null)
            {
                return _powerPool.PowerPoolOption.CustomQueueFactory();
            }
            else if (_powerPool.PowerPoolOption.QueueType == QueueType.FIFO)
            {
                return new ConcurrentStealablePriorityQueue<WorkItemBase>(false);
            }
            else if (_powerPool.PowerPoolOption.QueueType == QueueType.LIFO)
            {
                return new ConcurrentStealablePriorityStack<WorkItemBase>(false);
            }
            else
            {
                return new ConcurrentStealablePriorityDeque<WorkItemBase>(true);
            }
        }

        internal void ExecuteWork()
        {
            _powerPool.OnWorkStarted(Work.ID);

            ExecuteResultBase executeResult;
            bool isRetry = false;
            do
            {
                if (isRetry && Work.BaseAsyncWorkID != null && _powerPool._aliveWorkDic.TryGetValue(Work.BaseAsyncWorkID, out WorkBase asyncBaseWork))
                {
                    Work = asyncBaseWork;
                    Work.AllowEventsAndCallback = false;
                }
                executeResult = ExecuteMain();
                InvokeEventsAndCallback(executeResult);
                isRetry = true;
            } while (Work.ShouldImmediateRetry(executeResult));

            if (Work.ShouldRequeue(executeResult))
            {
                WorkBase requeueWork = Work;
                if (Work.BaseAsyncWorkID != null && _powerPool._aliveWorkDic.TryGetValue(Work.BaseAsyncWorkID, out WorkBase asyncBaseWork))
                {
                    requeueWork = asyncBaseWork;
                    requeueWork.AllowEventsAndCallback = false;
                }
                requeueWork._canCancel.InterlockedValue = CanCancel.Allowed;
                Interlocked.Increment(ref _powerPool._waitingWorkCount);
                _powerPool.SetWork(requeueWork);
            }
            else
            {
                CleanUpAndSetSignalAfterExecute(executeResult);
            }
        }

        private void SetTaskCompletionSourceAfterExecute(ExecuteResultBase executeResult)
        {
            if (_powerPool._tcsDict.TryRemove(Work.RealWorkID, out ITaskCompletionSource tcs))
            {
                if (executeResult.Status == Status.Stopped)
                {
                    tcs.SetCanceled();
                }
                else if (executeResult.Status == Status.Failed)
                {
                    tcs.SetException(executeResult.Exception);
                }
                else
                {
                    tcs.SetResult(executeResult);
                }
            }
        }

        private void InvokeEventsAndCallback(ExecuteResultBase executeResult)
        {
            if (executeResult == null)
            {
                // Work.AllowEventsAndCallback == false
                return;
            }
            if (executeResult.Status == Status.Stopped)
            {
                _powerPool.InvokeWorkStoppedEvent(executeResult, Work.BaseAsyncWorkID != null);
            }
            else
            {
                _powerPool.InvokeWorkEndedEvent(executeResult, Work.BaseAsyncWorkID != null);
            }
            if (Work.AllowEventsAndCallback)
            {
                if (Work.BaseAsyncWorkID != null)
                {
                    SetTaskCompletionSourceAfterExecute(executeResult);
                }
                Work.InvokeCallback(executeResult, _powerPool.PowerPoolOption);
            }
        }

        private void CleanUpAndSetSignalAfterExecute(ExecuteResultBase executeResult)
        {
            if (Work.AllowEventsAndCallback)
            {
                _powerPool.WorkCallbackEnd(Work, executeResult.Status);
                Work.AsyncDone = true;
            }

            Work.IsDone = true;

            if (Work.WaitSignal != null && Work.BaseAsyncWorkID == null)
            {
                Work.WaitSignal.Set();
            }

            if (Work.AllowEventsAndCallback && Work.BaseAsyncWorkID != null && _powerPool._aliveWorkDic.TryGetValue(Work.BaseAsyncWorkID, out WorkBase asyncBaseWork) && !asyncBaseWork.ShouldStoreResult)
            {
                if (asyncBaseWork.WaitSignal != null)
                {
                    asyncBaseWork.WaitSignal.Set();
                }
                _powerPool.TryRemoveAsyncWork(Work.BaseAsyncWorkID, true);
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
                _powerPool._aliveWorkerDicChanged = true;
            }
            if (_powerPool._idleWorkerDic.TryRemove(ID, out _))
            {
                Interlocked.Decrement(ref _powerPool._idleWorkerCount);
            }
            if (Work.BaseAsyncWorkID != null)
            {
                _powerPool.TryRemoveAsyncWork(Work.BaseAsyncWorkID, true);

                if (_powerPool._tcsDict.TryRemove(Work.RealWorkID, out ITaskCompletionSource tcs))
                {
                    tcs.SetCanceled();
                }
            }

            ExecuteResultBase executeResult = Work.SetExecuteResult(null, ex, Status.ForceStopped);
            executeResult.ID = Work.RealWorkID;
            if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
            {
                executeResult.StartDateTime = Work.StartDateTime;
            }
            _powerPool.InvokeWorkStoppedEvent(executeResult, Work.BaseAsyncWorkID != null);

            if (!ex.Data.Contains("ThrowedWhenExecuting"))
            {
                ex.Data.Add("ThrowedWhenExecuting", false);
            }
            Work.InvokeCallback(executeResult, _powerPool.PowerPoolOption);

            _powerPool.WorkCallbackEnd(Work, Status.ForceStopped);

            bool hasWaitingWork = RequeueAllWaitingWork(null);
            Work.AsyncDone = true;
            Work.IsDone = true;

            if (Work.WaitSignal != null)
            {
                Work.WaitSignal.Set();
            }

            _powerPool.FillWorkerQueue();

            if (!hasWaitingWork)
            {
                _powerPool.CheckPoolIdle();
            }

            Dispose();
        }

        private bool WorkerCountOutOfRange(WorkBase work)
        {
            bool res = false;
            if (_powerPool._canDeleteRedundantWorker.TrySet(CanDeleteRedundantWorker.NotAllowed, CanDeleteRedundantWorker.Allowed)
                && CanGetWork.TrySet(Constants.CanGetWork.Disabled, Constants.CanGetWork.Allowed))
            {
                if (_powerPool.AliveWorkerCount - _powerPool.LongRunningWorkerCount <= _powerPool.PowerPoolOption.MaxThreads)
                {
                    _powerPool._canDeleteRedundantWorker.InterlockedValue = CanDeleteRedundantWorker.Allowed;
                    CanGetWork.InterlockedValue = Constants.CanGetWork.Allowed;
                    return false;
                }

                WorkerState.InterlockedValue = WorkerStates.ToBeDisposed;

                if (_powerPool._aliveWorkerDic.TryRemove(ID, out _))
                {
                    Interlocked.Decrement(ref _powerPool._aliveWorkerCount);
                    _powerPool._aliveWorkerDicChanged = true;
                }

                _powerPool._canDeleteRedundantWorker.InterlockedValue = CanDeleteRedundantWorker.Allowed;

                Interlocked.Decrement(ref _powerPool._runningWorkerCount);
                _powerPool.InvokeRunningWorkerCountChangedEvent(false);

                _powerPool.FillWorkerQueue();

                bool hasWaitingWork = RequeueAllWaitingWork(work);
                if (!hasWaitingWork)
                {
                    _powerPool.CheckPoolIdle();
                }

                Dispose();

                res = true;
            }
            return res;
        }

        private bool RequeueAllWaitingWork(WorkBase work)
        {
            bool hasWaitingWork = false;
            if (work != null && work._canCancel.TrySet(CanCancel.Allowed, CanCancel.NotAllowed))
            {
                _powerPool.SetWork(work);
                hasWaitingWork = true;
            }
            WorkBase workBase;
            while ((workBase = GetNotCanceledWork()) != null)
            {
                _powerPool.SetWork(workBase);
                hasWaitingWork = true;
            }
            return hasWaitingWork;
        }

        private void SetKillTimer()
        {
            DestroyThreadOption destroyThreadOption = _powerPool.PowerPoolOption.DestroyThreadOption;

            if (destroyThreadOption != null && destroyThreadOption.KeepAliveTime != 0)
            {
                if (_killTimer == null)
                {
                    _killTimer = new DeferredActionTimer(() => { TryDisposeSelf(true); });
                }
                _killTimer.Set(destroyThreadOption.KeepAliveTime);
            }
            else if (destroyThreadOption == null && _killTimer != null)
            {
                _killTimer.Cancel();
            }
        }

        private ExecuteResultBase ExecuteMain()
        {
            ExecuteResultBase executeResult = null;
            DateTime runDateTime = default;
            try
            {
                if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
                {
                    runDateTime = DateTime.UtcNow;
                    Work.StartDateTime = runDateTime;
                    if (Work.BaseAsyncWorkID == null || Work.BaseAsyncWorkID == WorkID)
                    {
                        Interlocked.Increment(ref _powerPool._startCount);
                    }
                    Interlocked.Add(ref _powerPool._queueTime, (long)(runDateTime - Work.QueueDateTime).TotalMilliseconds);
                }
                object result = Work.Execute();

                WorkBase baseWork = AppendDuration(runDateTime);

                if (Work.AllowEventsAndCallback)
                {
                    executeResult = Work.SetExecuteResult(result, null, Status.Succeed);
                    SetStatisticsCollection(executeResult, runDateTime, baseWork);
                }
            }
            catch (ThreadInterruptedException ex)
            {
                if (!ex.Data.Contains("ThrowedWhenExecuting"))
                {
                    ex.Data.Add("ThrowedWhenExecuting", true);
                }
                throw;
            }
            catch (WorkStopException ex)
            {
                // If the incoming asynchronous work doesn't execute await,
                // and is stopped mid-execution,
                // then AllowEventsAndCallback may not be set to true.
                Work.AllowEventsAndCallback = true;

                executeResult = Work.SetExecuteResult(null, ex, Status.Stopped);
                WorkBase baseWork = null;
                if (Work.BaseAsyncWorkID != null)
                {
                    _powerPool._aliveWorkDic.TryGetValue(Work.BaseAsyncWorkID, out baseWork);
                }
                SetStatisticsCollection(executeResult, runDateTime, baseWork);
            }
            catch (Exception ex)
            {
                // If the incoming asynchronous work doesn't execute await,
                // and terminates by throwing an exception during execution,
                // then AllowEventsAndCallback may not be set to true
                Work.AllowEventsAndCallback = true;

                executeResult = Work.SetExecuteResult(null, ex, Status.Failed);
                executeResult.ID = Work.RealWorkID;
                _powerPool.OnWorkErrorOccurred(ex, ErrorFrom.WorkLogic, executeResult);
            }
#if DEBUG
            Spinner.Start(() => WorkHeldState == WorkHeldStates.NotHeld);
#else
            while (true)
            {
                if (WorkHeldState == WorkHeldStates.NotHeld)
                {
                    break;
                }
            }
#endif
            Work.Worker = null;
            if (Work.AllowEventsAndCallback)
            {
                executeResult.ID = Work.RealWorkID;
            }

            return executeResult;
        }

        private void SetStatisticsCollection(ExecuteResultBase executeResult, DateTime runDateTime, WorkBase baseWork)
        {
            if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
            {
                if (baseWork != null)
                {
                    runDateTime = baseWork.StartDateTime;
                    executeResult.QueueDateTime = baseWork.QueueDateTime;
                    executeResult.Duration = baseWork.Duration;
                }
                else
                {
                    executeResult.Duration = Work.Duration;
                }
                executeResult.StartDateTime = runDateTime;
            }
        }

        private WorkBase AppendDuration(DateTime runDateTime)
        {
            WorkBase baseWork = null;

            if (_powerPool.PowerPoolOption.EnableStatisticsCollection)
            {
                long duration = (long)(DateTime.UtcNow - runDateTime).TotalMilliseconds;
                if (Work.BaseAsyncWorkID != null && _powerPool._aliveWorkDic.TryGetValue(Work.BaseAsyncWorkID, out baseWork))
                {
                    baseWork.Duration += duration;
                }
                else
                {
                    Work.Duration += duration;
                }
            }

            return baseWork;
        }

        // Due to the unreliability of Thread.Interrupt(), forced stop functionality should be avoided as much as possible.
        // However, due to requirements, PowerThreadPool still provides this feature, with a warning in the documentation.
        // Although the PowerThreadPool will catch ThreadInterruptedException to ensure its own operation,
        // it cannot guarantee that business logic will not encounter unexpected issues as a result.
        internal void ForceStop()
        {
            if (WorkerState == WorkerStates.Running)
            {
                if (_thread != null)
                {
                    _thread.Interrupt();
                }
            }
            else
            {
                CanForceStop.InterlockedValue = Constants.CanForceStop.Allowed;
            }
        }

        internal void SetWork(WorkBase work, bool shouldSetCanGetWork)
        {
            _powerPool.SetWorkOwner(work);

            work.Worker = this;
            Interlocked.Increment(ref _waitingWorkCount);
            WorkerState.TrySet(WorkerStates.Running, WorkerStates.Idle, out WorkerStates originalWorkerState);

            if (_killTimer != null)
            {
                _killTimer.Cancel();
            }

            if (!shouldSetCanGetWork)
            {
                CanGetWork.InterlockedValue = Constants.CanGetWork.Allowed;
            }

            if (originalWorkerState == WorkerStates.Idle)
            {
                Interlocked.Increment(ref _powerPool._runningWorkerCount);
                _powerPool.InvokeRunningWorkerCountChangedEvent(true);

                if (!work._canCancel.TrySet(CanCancel.NotAllowed, CanCancel.Allowed))
                {
                    work = null;
                }

                AssignWork(work);
            }
            else
            {
                if (EnforceDequeOwnership && Thread.CurrentThread.ManagedThreadId != _thread.ManagedThreadId)
                {
                    _workInbox.Enqueue(work);
                }
                else
                {
                    _waitingWorkPriorityCollection.Set(work, work.WorkPriority);
                }
            }
        }

        internal List<WorkBase> Steal(int count)
        {
            List<WorkBase> stolenList = null;

            bool isContinue = true;
            while ((stolenList == null || stolenList.Count < count) && isContinue)
            {
                isContinue = false;

                WorkBase stolenWork = Steal();

                if (stolenWork != null)
                {
                    Interlocked.Decrement(ref _waitingWorkCount);
                    stolenWork.Worker = null;
                    if (stolenList == null)
                    {
                        stolenList = new List<WorkBase>();
                    }
                    stolenList.Add(stolenWork);

                    isContinue = true;
                }
            }

            return stolenList;
        }

        private void AssignWork(WorkBase work)
        {
            // In most cases, the loop will not iterate more than once.
            while (true)
            {
                if (_powerPool.AliveWorkerCount - _powerPool.LongRunningWorkerCount > _powerPool.PowerPoolOption.MaxThreads)
                {
                    if (WorkerCountOutOfRange(work))
                    {
                        if (work != null)
                        {
                            Interlocked.Decrement(ref _waitingWorkCount);
                        }
                        return;
                    }
                }

                if (work == null)
                {
                    work = Get();
                }
                if (work != null)
                {
                    Interlocked.Decrement(ref _waitingWorkCount);
                }

                if (work == null && _powerPool.AliveWorkerCount <= _powerPool.PowerPoolOption.MaxThreads)
                {
                    List<WorkBase> stolenWorkList = StealWorksFromOtherWorker();
                    SetStolenWorkList(ref work, stolenWorkList, false);
                }

                if (work == null)
                {
                    if (TurnToIdle(ref work))
                    {
                        return;
                    }
                }

                if (work == null)
                {
                    continue;
                }

                if (_killTimer != null)
                {
                    _killTimer.Cancel();
                }

                Interlocked.Decrement(ref _powerPool._waitingWorkCount);

                SetWorkToRun(work);

                _runSignal.Set();
                break;
            }
        }

        internal bool TryAssignWorkForNewWorker()
        {
            WorkBase work = null;

            List<WorkBase> stolenWorkList = StealWorksFromOtherWorker();
            return SetStolenWorkList(ref work, stolenWorkList, true);
        }

        private List<WorkBase> StealWorksFromOtherWorker()
        {
            Worker worker = null;
            int max = 0;

            _powerPool.UpdateAliveWorkerList();
            Worker[] workerList = _powerPool._aliveWorkerList;
            int step = 0;
            int startIndex = _powerPool._aliveWorkerListLoopIndex;
            int loopIndex = _powerPool._aliveWorkerListLoopIndex;

            // In most cases, the loop will not iterate more than once.
            while (true)
            {
                // WorkStealingLoopMaxStep is automatically calculated from MaxThreads using a logarithmic formula to optimize loop performance for different thread pool sizes.
                // It limits the minimum number of steps for each loop iteration.
                // The number of loop steps will not exceed the length of _aliveWorkerList.
                // _aliveWorkerListLoopIndex is used to ensure that the starting point of each loop iteration varies as much as possible.
                if ((step >= _powerPool.PowerPoolOption.WorkLoopMaxStep && worker != null) || step >= workerList.Length)
                {
                    break;
                }
                ++step;
                if (loopIndex >= workerList.Length)
                {
                    loopIndex = 0;
                }

                Worker runningWorker = workerList[loopIndex];

                if (runningWorker.WorkerState != WorkerStates.Running || runningWorker.ID == ID)
                {
                    ++loopIndex;
                    continue;
                }

                int waitingWorkCountTemp = runningWorker.WaitingWorkCount;
                if (waitingWorkCountTemp >= 1 && waitingWorkCountTemp > max)
                {
                    if (!runningWorker.WorkStealability.TrySet(Constants.WorkStealability.NotAllowed, Constants.WorkStealability.Allowed))
                    {
                        ++loopIndex;
                        continue;
                    }
                    if (worker != null)
                    {
                        worker.WorkStealability.InterlockedValue = Constants.WorkStealability.Allowed;
                    }
                    max = waitingWorkCountTemp;
                    worker = runningWorker;
                }
                ++loopIndex;
            }
            _powerPool._aliveWorkerListLoopIndex = loopIndex;
            return StealFromWorker(worker, max);
        }

        private List<WorkBase> StealFromWorker(Worker worker, int max)
        {
            if (worker != null)
            {
                int count = _powerPool.PowerPoolOption.StealOneWorkOnly ? 1 : (max == 1 ? 1 : max / 2);
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

        private bool SetStolenWorkList(ref WorkBase work, List<WorkBase> stolenWorkList, bool newWorker)
        {
            bool res = false;
            if (stolenWorkList != null)
            {
                foreach (WorkBase stolenWork in stolenWorkList)
                {
                    res = true;
                    if (!newWorker && work == null && stolenWork._canCancel.TrySet(CanCancel.NotAllowed, CanCancel.Allowed))
                    {
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

        private bool TurnToIdle(ref WorkBase work)
        {
            if (_statusPingPongChecker.HasPingedPong)
            {
                work = TryGetWorkAgainOnPingedPong();

                if (work != null)
                {
                    Interlocked.Decrement(ref _waitingWorkCount);
                    return false;
                }
            }

            if (CanGetWork.TrySet(Constants.CanGetWork.ToBeDisabled, Constants.CanGetWork.Allowed))
            {
                work = Get();

                if (work != null)
                {
                    Interlocked.Decrement(ref _waitingWorkCount);

                    CanGetWork.TrySet(Constants.CanGetWork.Allowed, Constants.CanGetWork.ToBeDisabled);

                    return false;
                }
                else
                {
                    _runSignal.Reset();

                    PowerPoolOption powerPoolOption = _powerPool.PowerPoolOption;

                    Interlocked.Decrement(ref _powerPool._runningWorkerCount);
                    _powerPool.InvokeRunningWorkerCountChangedEvent(false);

                    DestroyThreadOption destroyThreadOption = powerPoolOption.DestroyThreadOption;

                    if (destroyThreadOption != null && destroyThreadOption.KeepAliveTime == 0 && _powerPool.IdleWorkerCount >= destroyThreadOption.MinThreads)
                    {
                        CanGetWork.TrySet(Constants.CanGetWork.Disabled, Constants.CanGetWork.ToBeDisabled);
                        TryDisposeSelf(false);
                    }
                    else
                    {
                        _statusPingPongChecker.StartNewCheck();
                        if (destroyThreadOption != null && _powerPool.IdleWorkerCount >= destroyThreadOption.MinThreads)
                        {
                            SetKillTimer();
                        }

                        WorkerState.InterlockedValue = WorkerStates.Idle;

                        List<WorkBase> waitingWorkList = ResetAllWaitingWorkWhenIdle();

                        if (waitingWorkList != null)
                        {
                            foreach (WorkBase workBase in waitingWorkList)
                            {
                                SetWork(workBase, false);
                            }

                            CanGetWork.TrySet(Constants.CanGetWork.Allowed, Constants.CanGetWork.ToBeDisabled);
                        }
                        else
                        {
                            CanGetWork.TrySet(Constants.CanGetWork.Allowed, Constants.CanGetWork.ToBeDisabled);

                            if (_powerPool._idleWorkerDic.TryAdd(ID, this))
                            {
                                Interlocked.Increment(ref _powerPool._idleWorkerCount);
                                _powerPool._idleWorkerQueue.Enqueue(ID);
                            }

                            _thread.IsBackground = true;
                        }
                    }

                    _powerPool.CheckPoolIdle();

                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// After a Worker enters the Idle state, before allowing it to acquire a new Work,
        /// a "draining process" is performed.
        /// 
        /// There used to be a race here: a work item could be enqueued after the worker transitioned to Idle.
        /// The "Deque update" expanded the window in which this race can happen compared to before.
        /// Timing before the update:
        /// Path 2: Failed to get a Work; preparing to enter Idle state
        /// Path 2: Before entering, tries to get again; still fails
        /// Path 1: New Work is enqueued
        /// Path 1: Increments the waiting count
        /// Path 1: Changes the Work state (before: Running, after: remains Running)
        /// Path 2: Worker transitions to Idle state
        /// Timing after the update:
        /// Path 1: Increments the waiting count
        /// Path 2: Failed to get a Work; preparing to enter Idle state
        /// Path 2: Before entering, tries to get again; still fails
        /// Path 1: Changes the Work state (before: Running, after: remains Running)
        /// Path 2: Worker transitions to Idle state
        /// Path 1: New Work is enqueued
        /// Therefore, after transitioning to Idle, we must check the waiting count once more.
        /// An ABA is acceptable: even if the count changes to 1 and back to 0 during this period,
        /// it only means the Worker successfully processed that task.
        /// However, if the current count is not 0, we need to re-enqueue the unprocessed Work
        /// until the count returns to 0.
        /// </summary>
        private List<WorkBase> ResetAllWaitingWorkWhenIdle()
        {
            List<WorkBase> workList = null;
            WorkBase work = null;

            while (WaitingWorkCount > 0)
            {
                while ((work = Get()) != null)
                {
                    if (workList == null)
                    {
                        workList = new List<WorkBase>();
                    }
                    Interlocked.Decrement(ref _waitingWorkCount);
                    work._canCancel.TrySet(CanCancel.Allowed, CanCancel.NotAllowed);
                    workList.Add(work);
                }
            }

            return workList;
        }

        private WorkBase TryGetWorkAgainOnPingedPong()
        {
            // When a Worker experiences a state "ping-pong"
            // (i.e., the time interval since it last entered the Idle state and was then awakened is less than the threshold),
            // perform a limited number of spins to fetch Work before transitioning to Idle.
            WorkBase work = null;
            _statusPingPongChecker.StartSpin();
            for (int i = 0; _statusPingPongChecker.CanSpin && work == null; ++i)
            {
                if (i > 100)
                {
                    Thread.Yield();
                }
                work = Get();
            }

            _statusPingPongChecker.HandleSpinRes(work != null);

            return work;
        }

        private void SetWorkToRun(WorkBase work)
        {
            TimeoutOption workTimeoutOption = work.WorkTimeoutOption;
            if (workTimeoutOption == null)
            {
                workTimeoutOption = _powerPool.PowerPoolOption.DefaultWorkTimeoutOption;
            }

            if (workTimeoutOption != null)
            {
                if (_timeoutTimer == null)
                {
                    _timeoutTimer = new DeferredActionTimer();
                }
                _timeoutTimer.Set(workTimeoutOption.Duration, () =>
                {
                    _powerPool.OnWorkTimedOut(_powerPool, new WorkTimedOutEventArgs() { ID = WorkID });
                    _powerPool.Stop(WorkID, workTimeoutOption.ForceStop);
                });
            }

            Work = work;
            LongRunning = work.LongRunning;

            if (_thread != null)
            {
                if (_thread.Priority != work.ThreadPriority)
                {
                    _thread.Priority = work.ThreadPriority;
                }
                if (_thread.IsBackground != work.IsBackground)
                {
                    _thread.IsBackground = work.IsBackground;
                }
            }
        }

        internal void TryDisposeSelf(bool isIdle)
        {
            DestroyThreadOption destroyThreadOption = _powerPool.PowerPoolOption.DestroyThreadOption;
            if (destroyThreadOption == null)
            {
                return;
            }
            if (isIdle ? _powerPool.IdleWorkerCount > destroyThreadOption.MinThreads : _powerPool.IdleWorkerCount >= destroyThreadOption.MinThreads)
            {
                // ① There is a possibility that a worker may still obtain and execute work between the 
                // time the _killTimer triggers OnKillTimerElapsed and when CanGetWork is set to Disabled. 
                Spinner.Start(() =>
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
                _powerPool._aliveWorkerDicChanged = true;
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
                _timeoutTimer.Pause();
            }
        }

        internal void ResumeTimer()
        {
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Resume();
            }
        }

        internal bool DiscardOneWork(out WorkBase discardWork)
        {
            discardWork = null;
            bool res = false;
            WorkBase work = Discard();
            if (work != null)
            {
                Interlocked.Decrement(ref _waitingWorkCount);
                if (work.BaseAsyncWorkID != work.AsyncWorkID)
                {
                    _powerPool.SetWork(work);
                    res = false;
                }
                else
                {
                    res = true;
                    discardWork = work;
                }
            }
            return res;
        }

        private WorkBase Get()
        {
            if (EnforceDequeOwnership && Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId)
            {
                while (_workInbox.TryDequeue(out WorkBase work))
                {
                    _waitingWorkPriorityCollection.Set(work, work.WorkPriority);
                }
            }

            WorkBase waitingWork = _waitingWorkPriorityCollection.Get() as WorkBase;
            while (waitingWork != null && !waitingWork._canCancel.TrySet(CanCancel.NotAllowed, CanCancel.Allowed))
            {
                waitingWork = _waitingWorkPriorityCollection.Get() as WorkBase;
            }
            return waitingWork;
        }

        private WorkBase GetNotCanceledWork()
        {
            WorkBase waitingWork;
            do
            {
                waitingWork = _waitingWorkPriorityCollection.Get() as WorkBase;
            }
            while (WorkCancelNotAllowed(waitingWork));
            if (EnforceDequeOwnership && waitingWork == null)
            {
                do
                {
                    _workInbox.TryDequeue(out waitingWork);
                }
                while (WorkCancelNotAllowed(waitingWork));
            }
            return waitingWork;
        }

        private WorkBase Steal()
        {
            WorkBase waitingWork;
            do
            {
                waitingWork = _waitingWorkPriorityCollection.Steal() as WorkBase;
            }
            while (WorkCancelNotAllowed(waitingWork));
            if (EnforceDequeOwnership && waitingWork == null)
            {
                while (!_workInbox.TryDequeue(out waitingWork))
                {
                    break;
                }
            }
            return waitingWork;
        }

        private WorkBase Discard()
        {
            WorkBase waitingWork;
            do
            {
                waitingWork = _waitingWorkPriorityCollection.Discard() as WorkBase;
            }
            while (WorkCancelNotAllowed(waitingWork));
            return waitingWork;
        }

        private bool WorkCancelNotAllowed(WorkBase waitingWork)
        {
            return waitingWork != null && waitingWork._canCancel.InterlockedValue == CanCancel.NotAllowed;
        }

        internal bool IsCancellationRequested()
        {
            return Work.ShouldStop;
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
                    if (_thread != null)
                    {
                        _thread.Join();
                    }
                }

                if (_timeoutTimer != null)
                {
                    _timeoutTimer.Dispose();
                }
                if (_killTimer != null)
                {
                    _killTimer.Dispose();
                }
            }
        }
    }
}
