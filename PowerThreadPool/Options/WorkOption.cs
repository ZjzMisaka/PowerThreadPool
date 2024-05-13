using System;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Results;

namespace PowerThreadPool.Options
{
    public class WorkOption<TResult>
    {
        public WorkOption()
        {
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
        public Action<ExecuteResult<TResult>> Callback { get; set; } = null;

        /// <summary>
        /// The priority level of the work. Higher priority works are executed before lower priority works.
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
        /// A set of works that this work depends on. This work will not start until all dependent works have completed execution.
        /// </summary>
        public ConcurrentSet<string> Dependents { get; set; } = null;

        /// <summary>
        /// Is long running work.
        /// </summary>
        public bool LongRunning { get; set; } = false;

        /// <summary>
        /// Retry the work if execute failed.
        /// </summary>
        public RetryOption RetryOption { get; set; } = null;
    }

    public class WorkOption : WorkOption<object>
    {
        public WorkOption()
        {
        }
    }
}
