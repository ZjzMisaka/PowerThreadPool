using System;

namespace PowerThreadPool.Option
{
    public class DestroyThreadOption
    {
        /// <summary>
        /// The amount of time a thread is kept alive after it finishes execution. If a new task is received within this time, the thread is reused; otherwise, it is destroyed.
        /// </summary>
        public int KeepAliveTime { get; set; } = 10000;

        /// <summary>
        /// The minimum number of threads that the thread pool should maintain at all times.
        /// </summary>
        public int MinThreads { get; set; } = Environment.ProcessorCount;
    }
}
