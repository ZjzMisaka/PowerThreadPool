using System;
using PowerThreadPool.Results;

namespace PowerThreadPool.Options
{
    public class PowerPoolOption
    {
        public PowerPoolOption()
        {
        }

        /// <summary>
        /// The maximum number of threads that the thread pool can support.
        /// </summary>
        public int MaxThreads { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// The option for destroying threads in the thread pool.
        /// </summary>
        public DestroyThreadOption DestroyThreadOption { get; set; } = null;

        /// <summary>
        /// The total maximum amount of time that all works in the thread pool are permitted to run collectively before they are terminated.
        /// </summary>
        public TimeoutOption Timeout { get; set; } = null;

        /// <summary>
        /// The default maximum amount of time a work in the pool is allowed to run before it is terminated.
        /// </summary>
        public TimeoutOption DefaultWorkTimeout { get; set; } = null;

        /// <summary>
        /// The default callback function that is called when a work finishes execution.
        /// </summary>
        public Action<ExecuteResult<object>> DefaultCallback { get; set; } = null;

        /// <summary>
        /// Indicates whether the pool should begin in a suspended state.
        /// </summary>
        public bool StartSuspended { get; set; } = false;
    }
}
