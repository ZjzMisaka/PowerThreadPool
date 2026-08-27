using System;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.Asynchronous;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Helpers.Timers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;

namespace PowerThreadPool.Works
{
    internal abstract class WorkHandle : WorkItemBase
    {
        internal WorkHandle(WorkBase shell, PowerPool powerPool)
        {
            Shell = shell;
            PowerPool = powerPool;
        }
        internal ManualResetEvent WaitSignal { get; set; }

        internal abstract ExecuteResultBase ExecuteResultBase { get; }

        internal volatile bool _isDone;
        internal bool IsDone
        {
            get => _isDone;
            set => _isDone = value;
        }
        internal InterlockedFlag<CanCancel> _canCancel = CanCancel.Allowed;
        internal WorkBase Shell { get; set; }
        internal PowerPool PowerPool { get; }

        internal abstract ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status);
        internal abstract void ClearExecuteResult();

        internal bool Wait(CancellationToken cancellationToken, bool helpWhileWaiting = false)
        {
            Shell?.HelpWhileWaiting(cancellationToken, helpWhileWaiting);

            EnsureWaitSignalExists();

            if (!IsDone)
            {
                if (cancellationToken == default)
                    WaitSignal.WaitOne();
                else if (WaitHandle.WaitAny(new WaitHandle[] { WaitSignal, cancellationToken.WaitHandle }) == 1)
                    cancellationToken.ThrowIfCancellationRequested();
            }

            return true;
        }

        internal Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            Task<bool> task = null;
            if (CheckWorkAlreadyDoneWhenAsyncWait(null, out task))
            {
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

        private void EnsureWaitSignalExists()
        {
            if (WaitSignal == null)
            {
                WaitSignal = new ManualResetEvent(false);
            }
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

        internal ExecuteResult<T> Fetch<T>(CancellationToken cancellationToken, bool helpWhileWaiting = false)
        {
            Wait(cancellationToken, helpWhileWaiting);

            ExecuteResult<T> res = FetchCore<T>();

            return res;
        }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        internal async Task<ExecuteResult<T>> FetchAsync<T>(CancellationToken cancellationToken)
        {
            await WaitAsync(cancellationToken);

            ExecuteResult<T> res = FetchCore<T>();

            return res;
        }
#else
        internal Task<ExecuteResult<T>> FetchAsync<T>(CancellationToken cancellationToken)
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
            if (PowerPool._aliveWorkDic.TryGetValue(ID, out WorkHandle work))
            {
                Spinner.Start(() => work.ExecuteResultBase != null, true);
                return work.ExecuteResultBase.ToTypedResult<T>();
            }
            else
            {
                return ExecuteResultBase.ToTypedResult<T>();
            }
        }

        internal void OnWorkDone()
        {
            // If the result needs to be stored, there is a possibility of fetching the result through Group.
            // Therefore, Work should not be removed from _aliveWorkDic and _workGroupDic for the time being
            if (Shell.Group == null || !Shell.ShouldStoreResult)
            {
                bool res = PowerPool._aliveWorkDic.TryRemove(ID, out _);
                if (WaitSignal != null)
                {
                    WaitSignal.Set();
                }
                if (res)
                {
                    PowerPool._workManager.Set(Shell);
                }
            }
        }
    }

    internal sealed class WorkHandleT<TResult> : WorkHandle
    {
        internal WorkHandleT(WorkBase shell, PowerPool powerPool) : base(shell, powerPool)
        {
        }

        internal override ExecuteResultBase ExecuteResultBase => ExecuteResult;
        internal ExecuteResult<TResult> _executeResult;

        internal ExecuteResult<TResult> ExecuteResult
        {
            get => _executeResult;
            set => _executeResult = value;
        }

        internal override ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status)
        {
            Shell.Status = status;
            ExecuteResult<TResult> executeResult = new ExecuteResult<TResult>();
            executeResult.SetExecuteResult(result, exception, status, Shell.QueueDateTime, Shell.RetryOption, Shell._retryCount);
            ExecuteResult = executeResult;
            if (Shell.ShouldStoreResult)
            {
                PowerPool._resultDic[ID] = ExecuteResult;
            }
            return executeResult;
        }

        internal override void ClearExecuteResult()
        {
            ExecuteResult = null;
        }
    }

    internal abstract class WorkBase : IDisposable
    {
        internal WorkHandle WorkHandle { get; set; }
        internal Worker Worker { get; set; }
        internal PowerPool PowerPool { get; set; }
        internal CancellationTokenSource CancellationTokenSource { get; set; }
        internal InterlockedFlag<CanSetTaskCompletionSource> _canSetTaskCompletionSource = CanSetTaskCompletionSource.Allowed;
        internal InterlockedFlag<CanFinalizeWork> _canFinalizeWork = CanFinalizeWork.Allowed;
        internal ITaskCompletionSource TaskCompletionSource { get; set; }
        internal bool IsAlive { get; set; } = false;
        
        internal volatile int _retryCount;
        internal volatile int _executeCount;
        internal int ExecuteCount
        {
            get
            {
                int count = _executeCount;
                if (PowerPool._aliveWorkDic.TryGetValue(WorkHandle.ID, out WorkHandle asyncBaseWork))
                {
                    count = asyncBaseWork.Shell._executeCount;
                }
                return count;
            }
            set => _executeCount = value;
        }
        internal volatile bool _isCurrentDone;
        internal bool IsCurrentDone
        {
            get => _isCurrentDone;
            set => _isCurrentDone = value;
        }
        internal volatile bool _isPausing;
        internal bool IsPausing
        {
            get => _isPausing;
            set => _isPausing = value;
        }
        internal InterlockedFlag<DependencyStatus> _dependencyStatus = DependencyStatus.Normal;
        internal Status Status { get; set; }
        internal bool ShouldStop { get; set; }
        internal ManualResetEvent PauseSignal { get; set; }
        internal AsyncManualResetEvent PauseAsyncSignal { get; set; }
        internal DeferredActionTimer TimeoutTimer { get; set; }
        /// <summary>
        /// Queue datetime (UTC).
        /// </summary>
        internal DateTime QueueDateTime { get; set; }
        /// <summary>
        /// Start datetime (UTC).
        /// </summary>
        internal DateTime StartDateTime { get; set; }
        internal long Duration { get; set; }
        internal long DeadTickCount { get; set; }
        internal abstract object Execute();
        internal abstract void ResetBase();
        internal abstract void SetFunction<TResult>(Func<TResult> function, bool isFirst);
        internal abstract void SetAction(Action action, bool isFirst);
        internal abstract WorkBase Init(PowerPool powerPool, WorkID id, WorkOption option, CancellationTokenSource cancellationTokenSource);
        internal abstract bool Refresh();
        internal abstract bool Stop(bool forceStop);
        internal abstract bool Cancel(bool needFreeze);
        internal abstract void HelpWhileWaiting(CancellationToken cancellationToken, bool helpWhileWaiting);
        internal abstract bool Pause();
        internal abstract bool Resume();
        internal abstract void InvokeCallback(ExecuteResultBase executeResult, PowerPoolOption powerPoolOption);
        internal abstract bool ShouldRetry(ExecuteResultBase executeResult);
        internal abstract bool ShouldImmediateRetry(ExecuteResultBase executeResult);
        internal abstract bool ShouldRequeue(ExecuteResultBase executeResult);
        public abstract void Dispose();
        internal abstract string Group { get; set; }
        internal abstract ThreadPriority ThreadPriority { get; }
        internal abstract bool IsBackground { get; }
        internal abstract int WorkPriority { get; }
        internal abstract TimeoutOption WorkTimeoutOption { get; }
        internal abstract RetryOption RetryOption { get; }
        internal abstract bool LongRunning { get; }
        internal abstract bool ShouldStoreResult { get; }
        internal abstract bool AutoCheckStopOnAsyncTask { get; }
        internal abstract WorkPlacementPolicy WorkPlacementPolicy { get; }
        internal abstract ConcurrentSet<WorkID> Dependents { get; }
        internal abstract bool AllowEventsAndCallback { get; set; }
        internal abstract Type ResultType { get; }
        internal abstract bool IsFirstAsyncWork { get; }
        internal abstract bool IsFunc { get; }
    }
}
