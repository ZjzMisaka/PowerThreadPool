namespace PowerThreadPool.EventArguments
{
    public class WorkStoppedEventArgs : WorkEventArgsBase
    {
        public WorkStoppedEventArgs() { }

        /// <summary>
        /// Indicating whether the work was stopped forcefully.
        /// </summary>
        public bool ForceStop { get; internal set; }
    }
}
