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
        // Holds either a WorkOption or a WorkOption<TResult>; the distinction is recovered
        // by "is WorkOption<TResult>" at the few call sites that need the typed callback.
        private WorkOption _workOption;
        private WorkOption WorkOption
        {
            get => _workOption;
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
        internal override bool EnableWorkTracking => WorkOption.EnableWorkTracking;
        internal override WorkPlacementPolicy WorkPlacementPolicy => WorkOption.WorkPlacementPolicy;
        internal override ConcurrentSet<WorkID> Dependents => WorkOption.Dependents;
        internal bool _allowEventsAndCallback;
        internal override bool AllowEventsAndCallback
        {
            get => TaskCompletionSource == null ? true : _allowEventsAndCallback;
            set => _allowEventsAndCallback = value;
        }

        internal Work()
        {
        }

        internal Work(PowerPool powerPool, WorkID id, WorkOption option, CancellationTokenSource cancellationTokenSource)
        {
            Init(powerPool, id, option, cancellationTokenSource);
        }

        internal override WorkBase Init(PowerPool powerPool, WorkID id, WorkOption option, CancellationTokenSource cancellationTokenSource)
        {
            _workOption = option ?? WorkOption.DefaultInstance;
            PowerPool = powerPool;
            ID = id;
            ExecuteCount = 0;
            ShouldStop = false;
            IsPausing = false;
            if (cancellationTokenSource != null)
            {
                Extras._cancellationTokenSource = cancellationTokenSource;
            }
            NeedsExecuteResult = TaskCompletionSource != null
                || WorkOption.ShouldStoreResult || powerPool.PowerPoolOption.ShouldStoreResult
                || WorkOption.Callback != null
                || (WorkOption is WorkOption<TResult> wor2 && wor2.Callback != null)
                || powerPool.PowerPoolOption.DefaultCallback != null
                || powerPool.HasWorkEndEventSubscriber
                || powerPool.PowerPoolOption.EnableStatisticsCollection;
            return this;
        }

        internal override bool Stop(bool forceStop)
        {
            bool res = false;

            EnsureCancellationTokenSourceCanceled();

            if (forceStop)
            {
                // Ensure that the executing Work is not switched and the target Work is not stolen during the operation of the Worker
                using (new WorkGuard(this, true))
                {
                    if (Worker != null)
                    {
                        if (Worker.WorkID == ID)
                        {
                            if (Worker._canForceStop.TrySet(CanForceStop.NotAllowed, CanForceStop.Allowed))
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

        /// <summary>
        /// Cancels the work's cancellation token source, creating it first if the work was
        /// queued without one. Creation and cancel are two separate steps, so a Stop racing
        /// with Init's publication is resolved by cancelling after publishing: whoever
        /// publishes the CTS re-checks ShouldStop, guaranteeing the token ends up canceled.
        /// </summary>
        private void EnsureCancellationTokenSourceCanceled()
        {
            WorkExtras e = _extras;
            if (e != null)
            {
                e._cancellationTokenSource?.Cancel();
                if (e._cancellationTokenSource != null)
                {
                    return;
                }
            }

            CancellationTokenSource created = new CancellationTokenSource();
            if (Interlocked.CompareExchange(ref Extras._cancellationTokenSource, created, null) == null && !ShouldStop)
            {
                created.Cancel();
            }
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
                        SetTaskCompletionSource(Status.Canceled, null);
                    }

                    ExecuteResultBase executeResult = SetExecuteResult(null, null, Status.Canceled);
                    executeResult.ID = ID;
                    executeResult.StartDateTime = StartDateTime;

                    PowerPool.InvokeWorkCanceledEvent(executeResult);
                    InvokeCallback(executeResult, PowerPool.PowerPoolOption);
                    PowerPool.WorkCallbackEnd(this, Status.Canceled);

                    int waitingWorkCount = Interlocked.Decrement(ref Worker._waitingWorkCount);

                    if (waitingWorkCount == 0)
                    {
                        // The Cancel function decreases the count of _waitingWorkCount before execution. 
                        // Although in most cases, an Idle check will be performed after the currently running work completes, 
                        // if the Worker has already completed its Idle check when the count is decreased, 
                        // it may cause the thread pool to remain in a running state indefinitely. 
                        // Therefore, an additional check is required here to ensure that an Idle check is performed 
                        // after reducing the count of _waitingWorkCount.
                        PowerPool.CheckPoolIdle();
                    }
                }
            }

            return res;
        }

        internal override void SetTaskCompletionSource(Status status, ExecuteResultBase executeResult)
        {
            // InterlockedFlag is a struct: TrySet must run against the field inside WorkExtras,
            // never against a property-returned copy, or the atomic transition would be lost.
            if (TaskCompletionSource == null
                || (status == Status.Succeed && executeResult == null)
                || !Extras._canSetTaskCompletionSource.TrySet(CanSetTaskCompletionSource.NotAllowed, CanSetTaskCompletionSource.Allowed))
            {
                return;
            }
            if (status == Status.Stopped || status == Status.ForceStopped || status == Status.Canceled)
            {
                TaskCompletionSource.SetCanceled();
            }
            else if (status == Status.Failed)
            {
                TaskCompletionSource.SetException(executeResult.Exception);
            }
            else
            {
                TaskCompletionSource.SetResult(executeResult);
            }
        }

        internal override bool Wait(CancellationToken cancellationToken, bool helpWhileWaiting = false)
        {
            HelpWhileWaiting(cancellationToken, helpWhileWaiting);

            // The CAS inside EnsureWaitSignal is the acquire side of the IsDone/WaitSignal
            // handshake; IsDone must be checked after it, not before.
            ManualResetEventSlim waitSignal = EnsureWaitSignal();

            if (!IsDone)
            {
                if (cancellationToken == default)
                    waitSignal.Wait();
                else if (WaitHandle.WaitAny(new WaitHandle[] { waitSignal.WaitHandle, cancellationToken.WaitHandle }) == 1)
                    cancellationToken.ThrowIfCancellationRequested();
            }

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
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            Task<bool> task = null;
            if (CheckWorkAlreadyDoneWhenAsyncWait(null, out task))
            {
                return task;
            }

            TaskCompletionSource<bool> tcs = PowerPool.NewTcs<bool>();
            ManualResetEventSlim ev = EnsureWaitSignal();

            RegisteredWaitHandle rwh = null;
            WaitOrTimerCallback cb = (state, timedOut) =>
            {
                SetTcsResult(tcs);
            };
            rwh = ThreadPool.RegisterWaitForSingleObject(ev.WaitHandle, cb, null, Timeout.Infinite, true);

            PowerPool._waitRegDict[tcs.Task] = rwh;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
#if (NET46_OR_GREATER || NET5_0_OR_GREATER)
                    if (tcs.TrySetCanceled(cancellationToken))
                    {
                        SetTcsResult(tcs);
                    }
#else
                    if (tcs.TrySetCanceled())
                    {
                        SetTcsResult(tcs);
                    }
#endif
                });
            }

            if (CheckWorkAlreadyDoneWhenAsyncWait(tcs, out task))
            {
                return task;
            }

            return tcs.Task;
#else
            return Task.Factory.StartNew(() =>
            {
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
            _resultRequested = true;
            Wait(cancellationToken, helpWhileWaiting);

            return FetchCore<T>();
        }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        internal override async Task<ExecuteResult<T>> FetchAsync<T>(CancellationToken cancellationToken)
        {
            await WaitAsync(cancellationToken);

            return FetchCore<T>();
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
                if (ExecuteResult == null && IsDone)
                {
                    SetExecuteResult(_lastResult, null, Status.Succeed);
                }
                return ExecuteResult.ToTypedResult<T>();
            }
        }

        internal override bool Pause()
        {
            // Publish the signals first (CAS), then IsPausing, then Reset:
            // the Worker that observes IsPausing is guaranteed to see a fully published signal.
            if (TaskCompletionSource == null)
            {
                EnsurePauseSignal()?.Reset();
            }
            if (TaskCompletionSource != null)
            {
                EnsurePauseAsyncSignal()?.Reset();
            }

            IsPausing = true;
            return true;
        }

        private ManualResetEvent EnsurePauseSignal()
        {
            WorkExtras e = Extras;
            ManualResetEvent s = e._pauseSignal;
            if (s != null)
            {
                return s;
            }

            ManualResetEvent created = new ManualResetEvent(true);
            s = Interlocked.CompareExchange(ref e._pauseSignal, created, null);
            if (s == null)
            {
                return created;
            }
            created.Dispose(); // Lost the race: drop ours, use the winner's.
            return s;
        }

        private AsyncManualResetEvent EnsurePauseAsyncSignal()
        {
            WorkExtras e = Extras;
            AsyncManualResetEvent s = e._pauseAsyncSignal;
            if (s != null)
            {
                return s;
            }

            AsyncManualResetEvent created = new AsyncManualResetEvent(true);
            return Interlocked.CompareExchange(ref e._pauseAsyncSignal, created, null) ?? created;
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
            ExecuteResult<TResult> executeResult = ExecuteResult;
            if (executeResult == null)
            {
                executeResult = new ExecuteResult<TResult>();
                ExecuteResult = executeResult;
            }
            executeResult.SetExecuteResult(result, exception, status, QueueDateTime, RetryOption, _retryCount);
            if (WorkOption.ShouldStoreResult || PowerPool.PowerPoolOption.ShouldStoreResult)
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
            WorkExtras e = _extras;
            if (e == null)
            {
                // Default path: a single null check finishes the cleanup.
                return;
            }
            e._pauseSignal?.Dispose();
            e._timeoutTimer?.Dispose();
            e._cancellationTokenSource?.Dispose();
        }
    }
}
