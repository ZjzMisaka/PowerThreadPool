namespace PowerThreadPool.EventArguments
{
    public class WorkStoppedEventArgs : WorkEventArgsBase
    {
        public WorkStoppedEventArgs() { }

        private bool forceStop;
        public bool ForceStop { get => forceStop; internal set => forceStop = value; }
    }
}
