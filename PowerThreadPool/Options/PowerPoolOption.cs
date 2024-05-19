using System;
using PowerThreadPool.Results;

namespace PowerThreadPool.Options
{
    public enum QueueType { FIFO, LIFO }
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
        public TimeoutOption TimeoutOption { get; set; } = null;

        /// <summary>
        /// The default maximum amount of time a work in the pool is allowed to run before it is terminated.
        /// </summary>
        public TimeoutOption DefaultWorkTimeoutOption { get; set; } = null;

        /// <summary>
        /// The default callback function that is called when a work finishes execution.
        /// </summary>
        public Action<ExecuteResult<object>> DefaultCallback { get; set; } = null;

        /// <summary>
        /// Indicates whether the pool should begin in a suspended state.
        /// </summary>
        public bool StartSuspended { get; set; } = false;

        /// <summary>
        /// FIFO or LIFO.
        /// </summary>
        public QueueType QueueType { get; set; } = QueueType.FIFO;

        /// <summary>
        /// Determines whether to clear the result storage when the pool starts.
        /// </summary>
        public bool ClearResultStorageWhenPoolStart { get; set; } = true;

        /// <summary>
        /// Determines whether to clear the records of failed work when the pool starts.
        /// </summary>
        public bool ClearFailedWorkRecordWhenPoolStart { get; set; } = true;
    }
}
