namespace PowerThreadPool.EventArguments
{
    public class WorkStoppedEventArgs : WorkEventArgsBase
    {
        public WorkStoppedEventArgs() { }

        public bool ForceStop { get; internal set; }
    }
}
