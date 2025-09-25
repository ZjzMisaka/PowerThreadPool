using System;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.Asynchronous;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Options;
using PowerThreadPool.Results;

namespace PowerThreadPool.Works
{
    internal abstract class Work<TResult> : WorkBase
    {
        private WorkOption<TResult> _workOption;

        internal ExecuteResult<TResult> _executeResult;
        internal ExecuteResult<TResult> ExecuteResult
        {
            get => _executeResult;
            set => _executeResult = value;
        }

        internal override string Group
        {
            get => _workOption.Group;
            set
            {
                if (_workOption.IsDefaultInstance)
                {
                    _workOption = new WorkOption<TResult>();
                }
                _workOption.Group = value;
            }
        }
        internal override ThreadPriority ThreadPriority => _workOption.ThreadPriority;
        internal override bool IsBackground => _workOption.IsBackground;
        internal override int WorkPriority => _workOption.WorkPriority;
        internal override TimeoutOption WorkTimeoutOption => _workOption.TimeoutOption;
        internal override RetryOption RetryOption => _workOption.RetryOption;
        internal override bool LongRunning => _workOption.LongRunning;
        internal override bool ShouldStoreResult => _workOption.ShouldStoreResult;
        internal override WorkPlacementPolicy WorkPlacementPolicy => _workOption.WorkPlacementPolicy;
        internal override ConcurrentSet<WorkID> Dependents => _workOption.Dependents;
        internal override bool AllowEventsAndCallback
        {
            get => _workOption.AllowEventsAndCallback;
            set => _workOption.AllowEventsAndCallback = value;
        }
        internal override WorkID AsyncWorkID => _workOption.AsyncWorkID;
        internal override WorkID BaseAsyncWorkID => _workOption.BaseAsyncWorkID;
        internal override WorkID RealWorkID => _workOption.BaseAsyncWorkID == null ? ID : _workOption.BaseAsyncWorkID;

        internal Work(PowerPool powerPool, WorkID id, WorkOption<TResult> option)
        {
            PowerPool = powerPool;
            ID = id;
            ExecuteCount = 0;
            _workOption = option;
            ShouldStop = false;
            IsPausing = false;
        }

        internal override bool Stop(bool forceStop)
        {
            bool res = false;

            if (forceStop)
            {
                // Ensure that the executing Work is not switched and the target Work is not stolen during the operation of the Worker
                using (new WorkGuard(this, true))
                {
                    if (Worker != null)
                    {
                        if (Worker.WorkID == ID)
                        {
                            if (Worker.CanForceStop.TrySet(CanForceStop.NotAllowed, CanForceStop.Allowed))
                            {
                                Worker.ForceStop();
                            }
                            res = true;
                        }
                        else
                        {
                            res = Cancel(false);
                        }
                    }
                }
            }
            else
            {
                ShouldStop = true;
                Cancel(true);
                res = true;
            }

            return res;
        }

        internal override bool Cancel(bool needFreeze)
        {
            if (_canCancel.InterlockedValue == CanCancel.NotAllowed)
            {
                return false;
            }

            if (BaseAsyncWorkID != null && BaseAsyncWorkID != ID)
            {
                return false;
            }

            bool res = false;

            using (new WorkGuard(this, needFreeze))
            {
                res = _canCancel.TrySet(CanCancel.NotAllowed, CanCancel.Allowed);

                if (res)
                {
                    if (BaseAsyncWorkID != null)
                    {
                        PowerPool.TryRemoveAsyncWork(ID, false);

                        if (PowerPool._tcsDict.TryRemove(RealWorkID, out ITaskCompletionSource tcs))
                        {
                            tcs.SetCanceled();
                        }
                    }

                    ExecuteResultBase executeResult = SetExecuteResult(null, null, Status.Canceled);
                    executeResult.ID = ID;

                    PowerPool.InvokeWorkCanceledEvent(executeResult);
                    InvokeCallback(executeResult, PowerPool.PowerPoolOption);
                    PowerPool.WorkCallbackEnd(this, Status.Canceled);

                    Interlocked.Decrement(ref Worker._waitingWorkCount);
                    int waitingWorkCount = Interlocked.Decrement(ref PowerPool._waitingWorkCount);

                    if (waitingWorkCount == 0)
                    {
                        // The Cancel function decreases the count of _powerPool.PowerPoolOption before execution. 
                        // Although in most cases, an Idle check will be performed after the currently running work completes, 
                        // if the Worker has already completed its Idle check when the count is decreased, 
                        // it may cause the thread pool to remain in a running state indefinitely. 
                        // Therefore, an additional check is required here to ensure that an Idle check is performed 
                        // after reducing the count of _powerPool.PowerPoolOption.
                        PowerPool.CheckPoolIdle();
                    }
                }
            }

            return res;
        }

        internal override bool Wait(bool helpWhileWaiting = false)
        {
            SpinWait spinner = new SpinWait();
            while (!IsDone && helpWhileWaiting)
            {
                if (!PowerPool.HelpWhileWaiting())
                {
                    spinner.SpinOnce();
                }
                else
                {
                    spinner.Reset();
                }
            }

            if (WaitSignal == null)
            {
                WaitSignal = new AutoResetEvent(false);
            }

            if (!IsDone || (BaseAsyncWorkID != null && !AsyncDone))
            {
                WaitSignal.WaitOne();
            }

            return true;
        }

        internal override ExecuteResult<T> Fetch<T>(bool helpWhileWaiting = false)
        {
            Wait(helpWhileWaiting);

            if (BaseAsyncWorkID != null && PowerPool._asyncWorkIDDict.TryGetValue(BaseAsyncWorkID, out ConcurrentSet<WorkID> idSet) && idSet.Last != null && PowerPool._aliveWorkDic.TryGetValue(idSet.Last, out WorkBase lastWork))
            {
                Work<T> lastWorkT = lastWork as Work<T>;
                Spinner.Start(() => lastWorkT.ExecuteResult != null);
                return lastWorkT.ExecuteResult.ToTypedResult<T>();
            }
            else
            {
                return ExecuteResult.ToTypedResult<T>();
            }
        }

        internal override bool Pause()
        {
            if (PauseSignal == null)
            {
                PauseSignal = new ManualResetEvent(true);
            }

            IsPausing = true;
            PauseSignal.Reset();
            return true;
        }

        internal override bool Resume()
        {
            bool res = false;
            if (IsPausing)
            {
                IsPausing = false;
                PauseSignal.Set();
                res = true;
            }
            return res;
        }

        internal override void InvokeCallback(ExecuteResultBase executeResult, PowerPoolOption powerPoolOption)
        {
            if (_workOption.Callback != null)
            {
                PowerPool.SafeCallback(_workOption.Callback, EventArguments.ErrorFrom.Callback, executeResult);
            }
            else if (powerPoolOption.DefaultCallback != null)
            {
                PowerPool.SafeCallback(powerPoolOption.DefaultCallback, EventArguments.ErrorFrom.DefaultCallback, executeResult);
            }
        }

        internal override ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status)
        {
            // In theory, the program should ensure that ExecuteResult is set only when it is really necessary.
            // Whether it is a synchronous work or an asynchronous work, ExecuteResult should only be set once in its life cycle.
            // However, since `ThreadInterruptedException` will be thrown in various strange places (such as callback functions) when forced to stop,
            // it is specially allowed to reset ExecuteResult when the work is forced to stop.
            if (status != Status.ForceStopped && ExecuteResult != null)
            {
                return ExecuteResult;
            }
            Status = status;
            ExecuteResult<TResult> executeResult = new ExecuteResult<TResult>();
            executeResult.SetExecuteResult(result, exception, status, QueueDateTime, RetryOption, ExecuteCount);
            ExecuteResult = executeResult;
            if (_workOption.ShouldStoreResult)
            {
                PowerPool._resultDic[ID] = ExecuteResult;
            }
            return executeResult;
        }

        internal override bool ShouldRetry(ExecuteResultBase executeResult)
        {
            if (executeResult != null && executeResult.RetryInfo != null && executeResult.RetryInfo.StopRetry)
            {
                return false;
            }
            else if (_workOption.RetryOption != null && Status == Status.Failed && ((_workOption.RetryOption.RetryPolicy == RetryPolicy.Limited && ExecuteCount - 1 < _workOption.RetryOption.MaxRetryCount) || _workOption.RetryOption.RetryPolicy == RetryPolicy.Unlimited))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal override bool ShouldImmediateRetry(ExecuteResultBase executeResult)
        {
            bool res = ShouldRetry(executeResult) && _workOption.RetryOption.RetryBehavior == RetryBehavior.ImmediateRetry;
            if (res)
            {
                ExecuteResult = null;
            }
            return res;
        }

        internal override bool ShouldRequeue(ExecuteResultBase executeResult)
        {
            bool res = ShouldRetry(executeResult) && _workOption.RetryOption.RetryBehavior == RetryBehavior.Requeue;
            if (res)
            {
                ExecuteResult = null;
            }
            return res;
        }

        public override void Dispose()
        {
            if (PauseSignal != null)
            {
                PauseSignal.Dispose();
            }
            if (WaitSignal != null)
            {
                WaitSignal.Dispose();
            }
        }
    }
}
