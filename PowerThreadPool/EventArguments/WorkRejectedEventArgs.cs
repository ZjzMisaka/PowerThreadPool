using PowerThreadPool.Options;

namespace PowerThreadPool.EventArguments
{
    public class WorkRejectedEventArgs : WorkEventArgsBase
    {
        public WorkRejectedEventArgs(RejectType rejectType)
        {
            RejectType = rejectType;
        }

        public RejectType RejectType { get; }
    }
}
