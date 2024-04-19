using PowerThreadPool.Options;

namespace PowerThreadPool.Results
{
    public class RetryInfo
    {
        /// <summary>
        /// Current retry count.
        /// </summary>
        public int CurrentRetryCount { get; internal set; }

        /// <summary>
        /// Max retry count.
        /// Same as RetryOption.MaxRetryCount.
        /// </summary>
        public int MaxRetryCount { get; internal set; }

        /// <summary>
        /// Unlimited or Limited.
        /// Same as RetryOption.RetryPolicy.
        /// </summary>
        public RetryPolicy RetryPolicy { get; internal set; }

        private bool stopRetry = false;
        /// <summary>
        /// If set to true, even if the retry count is not full or unlimited, subsequent retries will be aborted.
        /// </summary>
        public bool StopRetry 
        { 
            get
            {
                return stopRetry;
            }
            set
            { 
                if (value == true)
                {
                    stopRetry = value;
                }
            }
        }
    }
}
