using System;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Options;
using PowerThreadPool.Results;

namespace PowerThreadPool.Works
{
    internal abstract class WorkBase : WorkItemBase, IDisposable
    {
        internal Worker Worker { get; set; }
        internal PowerPool PowerPool { get; set; }
        internal volatile int _executeCount;
        internal int ExecuteCount
        {
            get
            {
                int count = _executeCount;
                if (BaseAsyncWorkID != null && BaseAsyncWorkID != AsyncWorkID && PowerPool._aliveWorkDic.TryGetValue(BaseAsyncWorkID, out WorkBase asyncBaseWork))
                {
                    count = asyncBaseWork._executeCount;
                }
                return count;
            }
            set => _executeCount = value;
        }
        internal volatile bool _isDone;
        internal bool IsDone
        {
            get => _isDone;
            set => _isDone = value;
        }
        internal volatile bool _asyncDone;
        internal bool AsyncDone
        {
            get => _asyncDone;
            set => _asyncDone = value;
        }
        internal volatile bool _isPausing;
        internal bool SyncOrAsyncWorkDone
            => IsDone && (BaseAsyncWorkID == null || AsyncDone);
        internal bool IsPausing
        {
            get => _isPausing;
            set => _isPausing = value;
        }
        internal InterlockedFlag<DependencyStatus> _dependencyStatus = DependencyStatus.Normal;
        internal Status Status { get; set; }
        internal ManualResetEvent WaitSignal { get; set; }
        internal bool ShouldStop { get; set; }
        internal InterlockedFlag<CanCancel> _canCancel = CanCancel.Allowed;
        internal ManualResetEvent PauseSignal { get; set; }
        /// <summary>
        /// Queue datetime (UTC).
        /// </summary>
        internal DateTime QueueDateTime { get; set; }
        internal abstract object Execute();
        internal abstract bool Stop(bool forceStop);
        internal abstract bool Cancel(bool needFreeze);
        internal abstract bool Wait(CancellationToken cancellationToken, bool helpWhileWaiting = false);
        internal abstract Task<bool> WaitAsync(CancellationToken cancellationToken);
        internal abstract ExecuteResult<T> Fetch<T>(CancellationToken cancellationToken, bool helpWhileWaiting = false);
        internal abstract Task<ExecuteResult<T>> FetchAsync<T>(CancellationToken cancellationToken);
        internal abstract bool Pause();
        internal abstract bool Resume();
        internal abstract void InvokeCallback(ExecuteResultBase executeResult, PowerPoolOption powerPoolOption);
        internal abstract ExecuteResultBase SetExecuteResult(object result, Exception exception, Status status);
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
        internal abstract WorkPlacementPolicy WorkPlacementPolicy { get; }
        internal abstract ConcurrentSet<WorkID> Dependents { get; }
        internal abstract bool AllowEventsAndCallback { get; set; }
        internal abstract WorkID AsyncWorkID { get; }
        internal abstract WorkID BaseAsyncWorkID { get; }
        internal abstract WorkID RealWorkID { get; }
    }
}
