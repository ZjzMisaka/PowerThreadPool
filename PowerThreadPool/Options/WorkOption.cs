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
        internal static WorkOption DefaultInstance { get; set; } = new WorkOption
        {
            IsDefaultInstance = true
        };

        internal bool IsDefaultInstance { get; set; } = false;

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
        /// This work will not start until all dependent works have completed execution.
        /// </summary>
        public ConcurrentSet<WorkID> Dependents { get; set; } = null;

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
    }

    public class WorkOption<T> : WorkOption
    {
        public new Action<ExecuteResult<T>> Callback { get; set; } = null;
    }

    public static class WorkOptionExtensions
    {
        public static void SetCallback<TResult>(
            this WorkOption option,
            Action<ExecuteResult<TResult>> callback)
        {
            option.Callback = baseResult =>
            {
                if (baseResult is ExecuteResult<TResult> typedResult)
                {
                    callback(typedResult);
                }
                else
                {
                    throw new InvalidCastException(
                        $"Callback expects ExecuteResult<{typeof(TResult).Name}>, " +
                        $"but got {baseResult?.GetType().Name ?? "null"}.");
                }
            };
        }
    }
}
