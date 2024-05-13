namespace PowerThreadPool.EventArguments
{
    public class WorkStoppedEventArgs : WorkEventArgsBase
    {
        public WorkStoppedEventArgs() { }

        private bool _forceStop;
        public bool ForceStop { get => _forceStop; internal set => _forceStop = value; }
    }
}
