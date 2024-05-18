using System;
using PowerThreadPool.Options;

namespace PowerThreadPool.Results
{
    public enum Status { Succeed, Failed, Canceled, Stopped, ForceStopped }

    public abstract class ExecuteResultBase
    {
        /// <summary>
        /// Work id.
        /// </summary>
        public string ID { get; internal set; }

        /// <summary>
        /// Status of the work.
        /// </summary>
        public Status Status { get; internal set; }

        /// <summary>
        /// If failed, Exception will be setted here.
        /// </summary>
        public Exception Exception { get; internal set; }

        private DateTime queueDateTime;
        /// <summary>
        /// Queue datetime.
        /// </summary>
        public DateTime QueueDateTime
        {
            get => queueDateTime.ToLocalTime();
            internal set => queueDateTime = value;
        }

        private DateTime startDateTime;
        /// <summary>
        /// Start datetime.
        /// </summary>
        public DateTime StartDateTime
        {
            get => startDateTime.ToLocalTime();
            internal set => startDateTime = value;
        }

        private DateTime endDateTime;
        /// <summary>
        /// End datetime.
        /// </summary>
        public DateTime EndDateTime
        {
            get => endDateTime.ToLocalTime();
            internal set => endDateTime = value;
        }

        /// <summary>
        /// Retry information.
        /// </summary>
        public RetryInfo RetryInfo { get; internal set; }

        internal abstract void SetExecuteResult(object result, Exception exception, Status status, DateTime queueDateTime, RetryOption retryOption, int executeCount);
        internal abstract object GetResult();
        internal abstract ExecuteResult<object> ToObjResult();
    }
}
