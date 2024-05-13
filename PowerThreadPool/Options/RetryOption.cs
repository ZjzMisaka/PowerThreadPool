namespace PowerThreadPool.Options
{
    public enum RetryBehavior { ImmediateRetry, Requeue };
    public enum RetryPolicy { Limited, Unlimited };
    public class RetryOption
    {
        /// <summary>
        /// ImmediateRetry or Requeue.
        /// </summary>
        public RetryBehavior RetryBehavior { get; set; } = RetryBehavior.ImmediateRetry;

        /// <summary>
        /// Unlimited or Limited.
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Limited;

        /// <summary>
        /// Max retry count.
        /// Enable if RetryPolicy is Limited
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
    }
}
