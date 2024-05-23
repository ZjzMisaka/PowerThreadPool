using System;
using PowerThreadPool.Results;

namespace PowerThreadPool.EventArguments
{
    public class WorkEndedEventArgs : WorkEventArgsBase
    {
        public WorkEndedEventArgs() { }

        /// <summary>
        /// The result of the work.
        /// </summary>
        public object Result { get; internal set; }

        /// <summary>
        /// Indicates whether the work was successful.
        /// </summary>
        public bool Succeed { get; internal set; }

        /// <summary>
        /// The exception that occurred if the work failed due to an uncaught exception.
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// The retry information of the work.
        /// </summary>
        public RetryInfo RetryInfo { get; internal set; }
    }
}
