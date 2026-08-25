using System;
using System.Threading;
using System.Threading.Tasks;
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
        private WorkOption _workOption;
        private WorkOption<TResult> _workOptionResult;
        private WorkOption WorkOption
        {
            get => _workOptionResult ?? _workOption;
            set => _workOption = value;
        }

        internal ExecuteResult<TResult> _executeResult;
        internal ExecuteResult<TResult> ExecuteResult
        {
            get => _executeResult;
            set => _executeResult = value;
        }

        internal override string Group
        {
            get => WorkOption.Group;
            set
            {
                if (WorkOption.IsDefaultInstance)
                {
                    WorkOption = new WorkOption();
                }
                WorkOption.Group = value;
            }
        }
        internal override ThreadPriority ThreadPriority => WorkOption.ThreadPriority;
        internal override bool IsBackground => WorkOption.IsBackground;
        internal override int WorkPriority => WorkOption.WorkPriority;
        internal override TimeoutOption WorkTimeoutOption => WorkOption.TimeoutOption;
        internal override RetryOption RetryOption => WorkOption.RetryOption;
        internal override bool LongRunning => WorkOption.LongRunning;
        internal override bool ShouldStoreResult => WorkOption.ShouldStoreResult;
        internal override ExecuteResultBase ExecuteResultBase => ExecuteResult;
        internal override bool AutoCheckStopOnAsyncTask => WorkOption.AutoCheckStopOnAsyncTask;
        internal override WorkPlacementPolicy WorkPlacementPolicy => WorkOption.WorkPlacementPolicy;
        internal override ConcurrentSet<WorkID> Dependents => WorkOption.Dependents;
        internal bool _allowEventsAndCallback;
        internal override bool AllowEventsAndCallback
        {
            get => TaskCompletionSource == null ? true : _allowEventsAndCallback;
            set => _allowEventsAndCallback = value;
        }
        internal override Type ResultType => typeof(TResult);

        internal Work()
        {
        }

        internal Work(PowerPool powerPool, WorkID id, WorkOption option, CancellationTokenSource cancellationTokenSource)
        {
            Init(powerPool, id, option, cancellationTokenSource);
        }

        internal override WorkBase Init(PowerPool powerPool, WorkID id, WorkOption option, CancellationTokenSource cancellationTokenSource)
        {
            if (option is WorkOption<TResult> wor)
            {
                _workOptionResult = wor;
            }
            else
            {
                _workOption = option;
            }
            PowerPool = powerPool;
            ID = id;
            ExecuteCount = 0;
            ShouldStop = false;
            IsPausing = false;
            CancellationTokenSource = cancellationTokenSource;
            return this;
        }

        internal override bool Refresh()
        {
            if (_canRefresh != 0)
            {
                return false;
            }

            IsAlive = false;
            IsCurrentDone = false;
            IsDone = false;
            IsPausing = false;
            ShouldStop = false;
            Status = default;

            _retryCount = 0;
            _executeCount = 0;
            Duration = 0;
            QueueDateTime = default;
            StartDateTime = default;

            _canSetTaskCompletionSource = CanSetTaskCompletionSource.Allowed;
            _canFinalizeWork = CanFinalizeWork.Allowed;
            _canCancel = CanCancel.Allowed;
            _dependencyStatus = DependencyStatus.Normal;

            Worker = null;
            TaskCompletionSource = null;

            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Dispose();
                CancellationTokenSource = null;
            }

            if (WaitSignal != null)
            {
                WaitSignal.Dispose();
                WaitSignal = null;
            }

            if (PauseSignal != null)
            {
                PauseSignal.Dispose();
                PauseSignal = null;
            }

            PauseAsyncSignal = null;

            if (TimeoutTimer != null)
            {
                TimeoutTimer.Dispose();
                TimeoutTimer = null;
            }

            _workOption = null;
            _workOptionResult = null;
            ExecuteResult = null;
            _allowEventsAndCallback = false;

            return true;
        }


        private void EnsureWaitSignalExists()
        {
            if (WaitSignal == null)
            {
                WaitSignal = new ManualResetEvent(false);
            }
        }

        internal override bool Stop(bool forceStop)
        {
            bool res = false;

            CancellationTokenSource?.Cancel();

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

            bool res = false;

            using (new WorkGuard(this, needFreeze))
            {
                res = _canCancel.TrySet(CanCancel.NotAllowed, CanCancel.Allowed);
                if (res)
                {
                    if (TaskCompletionSource != null)
                    {
                        Interlocked.Decrement(ref PowerPool._asyncWorkCount);
                        TaskCompletionSource.SetCanceled();
                    }

                    ExecuteResultBase executeResult = SetExecuteResult(null, null, Status.Canceled);
                    executeResult.ID = ID;
                    executeResult.StartDateTime = StartDateTime;

                    PowerPool.InvokeWorkCanceledEvent(executeResult);
                    InvokeCallback(executeResult, PowerPool.PowerPoolOption);
                    PowerPool.WorkCallbackEnd(this, Status.Canceled);

                    // Run help
                    if (Worker != null)
                    {
                        Interlocked.Decrement(ref Worker._waitingWorkCount);
                    }
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

        internal override bool Wait(CancellationToken cancellationToken, bool helpWhileWaiting = false)
        {
            Interlocked.Increment(ref _canRefresh);

            HelpWhileWaiting(cancellationToken, helpWhileWaiting);

            EnsureWaitSignalExists();

            if (!IsDone)
            {
                if (cancellationToken == default)
                    WaitSignal.WaitOne();
                else if (WaitHandle.WaitAny(new WaitHandle[] { WaitSignal, cancellationToken.WaitHandle }) == 1)
                    cancellationToken.ThrowIfCancellationRequested();
            }

            Interlocked.Decrement(ref _canRefresh);

            return true;
        }

        private void HelpWhileWaiting(CancellationToken cancellationToken, bool helpWhileWaiting)
        {
            SpinWait spinner = new SpinWait();
            while (!IsDone && helpWhileWaiting)
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                if (!PowerPool.HelpWhileWaiting())
                {
                    spinner.SpinOnce();
                }
                else
                {
                    spinner.Reset();
                }
            }
        }

        internal override Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _canRefresh);

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            Task<bool> task = null;
            if (CheckWorkAlreadyDoneWhenAsyncWait(null, out task))
            {
                Interlocked.Decrement(ref _canRefresh);
                return task;
            }

            TaskCompletionSource<bool> tcs = PowerPool.NewTcs<bool>();
            EnsureWaitSignalExists();
            ManualResetEvent ev = WaitSignal;

            RegisteredWaitHandle rwh = null;
            WaitOrTimerCallback cb = (state, timedOut) =>
            {
                SetTcsResult(tcs);
            };
            rwh = ThreadPool.RegisterWaitForSingleObject(ev, cb, null, Timeout.Infinite, true);

            PowerPool._waitRegDict[tcs.Task] = rwh;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
#if (NET46_OR_GREATER || NET5_0_OR_GREATER)
                    if (tcs.TrySetCanceled(cancellationToken))
                    {
                        Interlocked.Decrement(ref _canRefresh);
                        SetTcsResult(tcs);
                    }
#else
                    if (tcs.TrySetCanceled())
                    {
                        Interlocked.Decrement(ref _canRefresh);
                        SetTcsResult(tcs);
                    }
#endif
                });
            }

            if (CheckWorkAlreadyDoneWhenAsyncWait(tcs, out task))
            {
                Interlocked.Decrement(ref _canRefresh);
                return task;
            }

            return tcs.Task;
#else
            return Task.Factory.StartNew(() =>
            {
                Interlocked.Decrement(ref _canRefresh);
                return Wait(cancellationToken, false);
            });
#endif
        }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        private bool CheckWorkAlreadyDoneWhenAsyncWait(TaskCompletionSource<bool> tcs, out Task<bool> task)
        {
            bool res = false;
            task = default;

            if (IsDone)
            {
                res = true;

                SetTcsResult(tcs);

                task = Task.FromResult(true);
            }

            return res;
        }

        private void SetTcsResult(TaskCompletionSource<bool> tcs)
        {
            if (tcs != null)
            {
                tcs.TrySetResult(true);
                if (PowerPool._waitRegDict.TryRemove(tcs.Task, out RegisteredWaitHandle h))
                {
                    h.Unregister(null);
                }
            }
        }
#endif

        internal override ExecuteResult<T> Fetch<T>(CancellationToken cancellationToken, bool helpWhileWaiting = false)
        {
            Interlocked.Increment(ref _canRefresh);
            Wait(cancellationToken, helpWhileWaiting);

            ExecuteResult<T> res = FetchCore<T>();
            Interlocked.Decrement(ref _canRefresh);

            return res;
        }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        internal override async Task<ExecuteResult<T>> FetchAsync<T>(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _canRefresh);
            await WaitAsync(cancellationToken);

            ExecuteResult<T> res = FetchCore<T>();
            Interlocked.Decrement(ref _canRefresh);

            return res;
        }
#else
        internal override Task<ExecuteResult<T>> FetchAsync<T>(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                WaitAsync(cancellationToken).Wait();

                return FetchCore<T>();
            });
        }
#endif

        private ExecuteResult<T> FetchCore<T>()
        {
            if (PowerPool._aliveWorkDic.TryGetValue(ID, out WorkBase work))
            {
                Work<T> workT = work as Work<T>;
                Spinner.Start(() => workT.ExecuteResult != null, true);
                return workT.ExecuteResult.ToTypedResult<T>();
            }
            else
            {
                return ExecuteResult.ToTypedResult<T>();
            }
        }

        internal override bool Pause()
        {
            if (TaskCompletionSource == null && PauseSignal == null)
            {
                PauseSignal = new ManualResetEvent(true);
            }
            if (TaskCompletionSource != null && PauseAsyncSignal == null)
            {
                PauseAsyncSignal = new AsyncManualResetEvent(true);
            }

            IsPausing = true;
            PauseSignal?.Reset();
            PauseAsyncSignal?.Reset();
            return true;
        }

        internal override bool Resume()
        {
            bool res = false;
            if (IsPausing)
            {
                IsPausing = false;
                PauseSignal?.Set();
                PauseAsyncSignal?.Set();
                res = true;
            }
            return res;
        }

        internal override void InvokeCallback(ExecuteResultBase executeResult, PowerPoolOption powerPoolOption)
        {
            if (WorkOption.Callback != null)
            {
                PowerPool.SafeCallback<TResult>(WorkOption.Callback, EventArguments.ErrorFrom.Callback, executeResult);
            }
            else if (WorkOption is WorkOption<TResult> wor && wor.Callback != null)
            {
                PowerPool.SafeCallback<TResult>(wor.Callback, EventArguments.ErrorFrom.Callback, executeResult);
            }
            else if (powerPoolOption.DefaultCallback != null)
            {
                PowerPool.SafeCallback(powerPoolOption.DefaultCallback, EventArguments.ErrorFrom.DefaultCallback, executeResult);
            }
        }

        internal override ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status)
        {
            Status = status;
            ExecuteResult<TResult> executeResult = new ExecuteResult<TResult>();
            executeResult.SetExecuteResult(result, exception, status, QueueDateTime, RetryOption, _retryCount);
            ExecuteResult = executeResult;
            if (WorkOption.ShouldStoreResult)
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
            else if (WorkOption.RetryOption != null && Status == Status.Failed && ((WorkOption.RetryOption.RetryPolicy == RetryPolicy.Limited && _retryCount < WorkOption.RetryOption.MaxRetryCount) || WorkOption.RetryOption.RetryPolicy == RetryPolicy.Unlimited))
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
            bool res = ShouldRetry(executeResult) && WorkOption.RetryOption.RetryBehavior == RetryBehavior.ImmediateRetry;
            if (res)
            {
                ExecuteResult = null;
            }
            return res;
        }

        internal override bool ShouldRequeue(ExecuteResultBase executeResult)
        {
            bool res = ShouldRetry(executeResult) && WorkOption.RetryOption.RetryBehavior == RetryBehavior.Requeue;
            if (res)
            {
                ExecuteResult = null;
            }
            return res;
        }

        public override void Dispose()
        {
            IsAlive = false;
            if (PauseSignal != null)
            {
                PauseSignal.Dispose();
            }
            if (TimeoutTimer != null)
            {
                TimeoutTimer.Dispose();
            }
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Dispose();
            }
        }
    }
}
