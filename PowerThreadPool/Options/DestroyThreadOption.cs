using System;

namespace PowerThreadPool.Options
{
    public class DestroyThreadOption
    {
        internal PowerPoolOption _powerPoolOption;
        internal PowerPoolOption PowerPoolOption
        {
            get => _powerPoolOption;
            set
            {
                CheckThreadCount(MinThreads, value.MaxThreads);
                _powerPoolOption = value;
            }
        }

        /// <summary>
        /// The amount of time a thread is kept alive after it finishes execution (ms).
        /// If a new work is received within this time, the thread is reused; otherwise, it is destroyed.
        /// </summary>
        public int KeepAliveTime { get; set; } = 10000;

        private int _minThreads = Environment.ProcessorCount;
        /// <summary>
        /// The minimum number of threads that the thread pool should maintain at all times.
        /// </summary>
        public int MinThreads
        {
            get => _minThreads;
            set
            {
                if (PowerPoolOption != null)
                {
                    CheckThreadCount(value, PowerPoolOption.MaxThreads);
                }
                _minThreads = value;
            }
        }

        internal void CheckThreadCount(int min, int max)
        {
            if (min > max)
            {
                throw new ArgumentException("The minimum number of threads cannot be greater than the maximum number of threads.");
            }
        }
    }
}
