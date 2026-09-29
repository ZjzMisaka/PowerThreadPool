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
    /// <summary>
    /// An abstract class representing a task submitted by the user. Each submitted task generates one instance of this class.
    /// The continuation of an asynchronous task does not generate a new instance, but reuses the current instance.
    ///
    /// Optimization already attempted: managing WorkBase using an object pool.
    /// Since the user may call blocking APIs such as Wait, there is a race condition where the Work object is still being referenced when it is returned to the pool.
    /// Therefore, an attempt was made to split Work into WorkBase and WorkHandle,
    /// WorkBase: regardless of whether ShouldStoreResult is true or a Group exists, it is immediately returned to the pool once the task completes.
    /// WorkHandle: holds the ID, Group, WaitSignal, ExecuteResultBase, IsDone, _canCancel.
    ///     When the user calls APIs such as Wait, the PTP can continue to hold a reference to it.
    /// However, the creation/pooling and unpooling of two objects, along with a large number of additional checks, resulted in:
    /// 1. GC benefits exist but are minor.
    /// 2. A slight decrease in performance.
    /// 3. Possible introduction of other bugs.
    /// Therefore, using an object pool to manage Work is not being considered for the time being.
    ///
    /// Size optimization: rarely-used members (signals, timeout timer, cancellation token
    /// source, statistics timestamps and the async-only completion flag) live in a lazily
    /// allocated WorkExtras side object instead of as fields here, so a plain synchronous
    /// work pays only for one extra reference. Forwarding properties below keep the original
    /// member names; their getters must never allocate - only Ensure*/ setter paths create
    /// the extras object. TaskCompletionSource stays a field because null-checks against
    /// it are used as the sync/async branch switch throughout the pool.
    /// </summary>
    internal abstract class WorkBase : WorkItemBase, IDisposable
    {
        internal volatile WorkExtras _extras;

        /// <summary>
        /// Write path: creates the extras object on demand. The CAS is a full barrier,
        /// which publishes the fully constructed object to other threads.
        /// Readers must use the forwarding properties below or _extras directly
        /// so that pure-read paths never allocate.
        /// </summary>
        internal WorkExtras Extras
        {
            get
            {
                WorkExtras e = _extras;
                if (e != null)
                {
                    return e;
                }
                WorkExtras created = new WorkExtras();
                return Interlocked.CompareExchange(ref _extras, created, null) ?? created;
            }
        }

        internal Worker Worker { get; set; }
        internal PowerPool PowerPool { get; set; }
        internal ITaskCompletionSource TaskCompletionSource { get; set; }
        internal bool IsAlive { get; set; } = false;
        internal volatile int _retryCount;
        internal volatile int _executeCount;
        internal int ExecuteCount
        {
            get
            {
                int count = _executeCount;
                if (PowerPool._aliveWorkDic.TryGetValue(ID, out WorkBase asyncBaseWork))
                {
                    count = asyncBaseWork._executeCount;
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
        internal volatile bool _isDone;
        internal bool IsDone
        {
            get => _isDone;
            set => _isDone = value;
        }
        internal volatile bool _isPausing;
        internal bool IsPausing
        {
            get => _isPausing;
            set => _isPausing = value;
        }
        internal InterlockedFlag<DependencyStatus> _dependencyStatus = DependencyStatus.Normal;
        internal Status Status { get; set; }
        internal volatile bool _resultRequested;
        internal volatile object _lastResult;
        internal bool NeedsExecuteResult { get; set; }
        internal bool ShouldStop { get; set; }
        internal InterlockedFlag<CanCancel> _canCancel = CanCancel.Allowed;
        internal InterlockedFlag<CanFinalizeWork> _canFinalizeWork = CanFinalizeWork.Allowed;

        internal ManualResetEventSlim WaitSignal => _extras?._waitSignal;
        internal ManualResetEvent PauseSignal => _extras?._pauseSignal;
        internal AsyncManualResetEvent PauseAsyncSignal => _extras?._pauseAsyncSignal;
        internal DeferredActionTimer TimeoutTimer => _extras?._timeoutTimer;
        internal CancellationTokenSource CancellationTokenSource => _extras?._cancellationTokenSource;

        /// <summary>
        /// Queue datetime (UTC). Reading never allocates the extras object.
        /// </summary>
        internal DateTime QueueDateTime
        {
            get
            {
                WorkExtras e = _extras;
                return e != null ? e._queueDateTime : default;
            }
            set => Extras._queueDateTime = value;
        }
        /// <summary>
        /// Start datetime (UTC). Reading never allocates the extras object.
        /// </summary>
        internal DateTime StartDateTime
        {
            get
            {
                WorkExtras e = _extras;
                return e != null ? e._startDateTime : default;
            }
            set => Extras._startDateTime = value;
        }
        internal long Duration
        {
            get
            {
                WorkExtras e = _extras;
                return e != null ? e._duration : 0;
            }
            set => Extras._duration = value;
        }

        /// <summary>
        /// Lazily creates and atomically publishes the wait signal. Two concurrent waiters
        /// race here; the loser disposes its own signal and uses the winner's, so both
        /// always observe the same instance. The CAS also acts as the acquire side of the
        /// IsDone/WaitSignal Dekker-style handshake: callers must re-check IsDone after
        /// this returns (the completers order IsDone = true; Thread.MemoryBarrier() before
        /// publishing/setting the signal).
        /// </summary>
        internal ManualResetEventSlim EnsureWaitSignal()
        {
            WorkExtras e = Extras;
            ManualResetEventSlim s = e._waitSignal;
            if (s != null)
            {
                return s;
            }

            ManualResetEventSlim created = new ManualResetEventSlim(false);
            s = Interlocked.CompareExchange(ref e._waitSignal, created, null);
            if (s == null)
            {
                return created;
            }
            created.Dispose(); // Lost the race: drop ours, use the winner's.
            return s;
        }

        internal abstract object Execute();
        internal abstract void ResetBase();
        internal abstract void SetFunction<TResult>(Func<TResult> function, bool isFirst);
        internal abstract void SetAction(Action action, bool isFirst);
        internal abstract WorkBase Init(PowerPool powerPool, WorkID id, WorkOption option, CancellationTokenSource cancellationTokenSource);
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
        internal abstract void SetTaskCompletionSource(Status status, ExecuteResultBase executeResult);
        public abstract void Dispose();
        internal abstract string Group { get; set; }
        internal abstract ThreadPriority ThreadPriority { get; }
        internal abstract bool IsBackground { get; }
        internal abstract int WorkPriority { get; }
        internal abstract TimeoutOption WorkTimeoutOption { get; }
        internal abstract RetryOption RetryOption { get; }
        internal abstract bool LongRunning { get; }
        internal abstract bool ShouldStoreResult { get; }
        internal abstract ExecuteResultBase ExecuteResultBase { get; }
        internal abstract bool AutoCheckStopOnAsyncTask { get; }
        internal abstract bool EnableWorkTracking { get; }
        internal abstract WorkPlacementPolicy WorkPlacementPolicy { get; }
        internal abstract ConcurrentSet<WorkID> Dependents { get; }
        internal abstract bool AllowEventsAndCallback { get; set; }

        internal abstract bool IsFirstAsyncWork { get; }
    }
}
