namespace PowerThreadPool.EventArguments
{
    public class WorkStoppedEventArgs : EventArgsBase
    {
        public WorkStoppedEventArgs() { }

        private bool forceStop;
        public bool ForceStop { get => forceStop; internal set => forceStop = value; }
    }
}
