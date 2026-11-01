namespace PowerThreadPool.Options
{
    public enum RetryBehavior
    {
        Immediate,
        Requeue
    }

    public enum RetryPolicy
    {
        Limited,
        Unlimited
    }

    public class RetryOption
    {
        /// <summary>
        /// Immediate or Requeue.
        /// </summary>
        public RetryBehavior RetryBehavior { get; set; } = RetryBehavior.Immediate;

        /// <summary>
        /// Unlimited or Limited.
        /// </summary>
        public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Limited;

        /// <summary>
        /// Max retry count.
        /// Enable if RetryPolicy is Limited.
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
    }
}
