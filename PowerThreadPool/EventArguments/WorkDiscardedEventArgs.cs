using PowerThreadPool.Options;

namespace PowerThreadPool.EventArguments
{
    public class WorkDiscardedEventArgs : WorkEventArgsBase
    {
        public WorkDiscardedEventArgs(RejectType rejectType)
        {
            RejectType = rejectType;
        }

        public RejectType RejectType { get; }
    }
}
