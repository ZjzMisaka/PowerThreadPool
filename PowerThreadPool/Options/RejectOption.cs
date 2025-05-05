namespace PowerThreadPool.Options
{
    public enum RejectType
    {
        AbortPolicy,
        CallerRunsPolicy,
        DiscardPolicy,
        DiscardOldestPolicy,
    }

    public class RejectOption
    {
        public int ThreadQueueLimit { get; set; }
        public RejectType RejectType { get; set; }
    }
}
