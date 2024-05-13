using System;
using PowerThreadPool.Results;

namespace PowerThreadPool.EventArguments
{
    public class WorkEndedEventArgs : WorkEventArgsBase
    {
        public WorkEndedEventArgs() { }

        public object Result { get; internal set; }

        public bool Succeed { get; internal set; }

        public Exception Exception { get; internal set; }

        public RetryInfo RetryInfo { get; internal set; }
    }
}
