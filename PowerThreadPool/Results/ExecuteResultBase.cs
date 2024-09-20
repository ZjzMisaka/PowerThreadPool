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
        /// If failed, the exception will be stored here.
        /// </summary>
        public Exception Exception { get; internal set; }

        private DateTime _queueDateTime;
        /// <summary>
        /// Queue datetime.
        /// </summary>
        public DateTime QueueDateTime
        {
            get => _queueDateTime.ToLocalTime();
            internal set => _queueDateTime = value;
        }

        private DateTime _startDateTime;
        /// <summary>
        /// Start datetime.
        /// </summary>
        public DateTime StartDateTime
        {
            get => _startDateTime.ToLocalTime();
            internal set => _startDateTime = value;
        }

        private DateTime _endDateTime;
        /// <summary>
        /// End datetime.
        /// </summary>
        public DateTime EndDateTime
        {
            get => _endDateTime.ToLocalTime();
            internal set => _endDateTime = value;
        }

        /// <summary>
        /// Retry information.
        /// </summary>
        public RetryInfo RetryInfo { get; internal set; }

        internal abstract void SetExecuteResult(object result, Exception exception, Status status, DateTime queueDateTime, RetryOption retryOption, int executeCount);
        internal abstract object GetResult();
        internal abstract ExecuteResult<object> ToObjResult();
        internal abstract ExecuteResult<TResult> ToTypedResult<TResult>();
    }
}
