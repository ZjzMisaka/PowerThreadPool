namespace PowerThreadPool.Works
{
    internal class AsyncWorkInfo
    {
        internal WorkID AsyncWorkID { get; set; }

        internal WorkID BaseAsyncWorkID { get; set; }

        internal bool AllowEventsAndCallback { get; set; } = true;
    }
}
