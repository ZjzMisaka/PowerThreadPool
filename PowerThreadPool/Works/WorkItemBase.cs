namespace PowerThreadPool.Works
{
    public abstract class WorkItemBase
    {
        internal WorkID ID { get; set; }
        internal string Group { get; set; }
    }
}
