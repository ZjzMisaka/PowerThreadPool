using System;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool.Options
{
    public enum WorkPlacementPolicy
    {
        PreferLocalWorker,
        PreferIdleThenLocal,
        PreferIdleThenLeastLoaded,
    }

    public class WorkOption
    {
        // An option flagged IsDefaultInstance is shared by multiple works
        // (the library-wide WorkOption.DefaultInstance, per-batch Parallel options).
        // Queue paths hand it out as-is to avoid per-work allocations; therefore any
        // internal code that writes to a work's option must first clone it via Clone()
        // (which resets the flag), so mutations cannot leak into sibling works.
        internal static WorkOption DefaultInstance { get; set; } = new WorkOption
        {
            IsDefaultInstance = true
        };

        internal bool IsDefaultInstance { get; set; } = false;

        /// <summary>
        /// Creates a shallow copy of this instance, preserving the runtime type.
        /// The copy is never a default instance, so mutating it cannot affect
        /// the shared DefaultInstance.
        /// </summary>
        internal WorkOption Clone()
        {
            WorkOption copy = (WorkOption)MemberwiseClone();
            copy.IsDefaultInstance = false;
            return copy;
        }

        /// <summary>
        /// The custom work ID. If set to null, the thread pool will use a Guid as the work ID.
        /// </summary>
        public string CustomWorkID { get; set; } = null;

        /// <summary>
        /// The group name of the work.
        /// </summary>
        public string Group { get; set; } = null;

        /// <summary>
        /// The maximum amount of time the work is allowed to run before it is terminated.
        /// </summary>
        public TimeoutOption TimeoutOption { get; set; } = null;

        /// <summary>
        /// The callback function that is called when the work finishes execution.
        /// </summary>
        public Action<ExecuteResultBase> Callback { get; set; } = null;

        /// <summary>
        /// The priority level of the work. Works with higher priority (represented by higher numerical values) are executed first.
        /// </summary>
        public int WorkPriority { get; set; } = 0;

        /// <summary>
        /// Specifies the scheduling priority of a System.Threading.Thread.
        /// </summary>
        public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;

        /// <summary>
        ///  Get/Set backgroundness of thread in thread pool.
        /// </summary>
        public bool IsBackground { get; set; } = true;

        /// <summary>
        /// A set of works that this work depends on.
        /// This work will not start until all preceding works have completed execution.
        /// </summary>
        public ConcurrentSet<WorkID> Dependencies { get; set; } = null;

        /// <summary>
        /// Is long running work.
        /// </summary>
        public bool LongRunning { get; set; } = false;

        /// <summary>
        /// Retry the work if execute failed.
        /// </summary>
        public RetryOption RetryOption { get; set; } = null;

        /// <summary>
        /// Should store the work result.
        /// </summary>
        public bool ShouldStoreResult { get; set; } = false;

        /// <summary>
        /// Indicates whether the work should be placed in the local worker's queue if possible.
        /// </summary>
        public WorkPlacementPolicy WorkPlacementPolicy { get; set; } = WorkPlacementPolicy.PreferIdleThenLeastLoaded;

        /// <summary>
        /// Indicates whether to automatically check for task stop when posting an async continuation.
        /// </summary>
        public bool AutoCheckStopOnAsyncTask { get; set; } = true;

        /// <summary>
        /// Enable work tracking.
        /// If true, Work can be controlled via WorkID.
        /// </summary>
        public bool EnableWorkTracking { get; set; } = true;
    }
}
