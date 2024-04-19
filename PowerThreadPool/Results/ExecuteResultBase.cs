using System;
using PowerThreadPool.Options;

namespace PowerThreadPool.Results
{
    public enum Status { Succeed, Failed, Canceled, Stopped, ForceStopped }

    public abstract class ExecuteResultBase
    {
        private string id;
        /// <summary>
        /// Work id.
        /// </summary>
        public string ID { get => id; internal set => id = value; }

        private Status status;
        /// <summary>
        /// Status of the work.
        /// </summary>
        public Status Status { get => status; internal set => status = value; }

        private Exception exception;
        /// <summary>
        /// If failed, Exception will be setted here.
        /// </summary>
        public Exception Exception { get => exception; internal set => exception = value; }

        private DateTime queueDateTime;
        /// <summary>
        /// Queue datetime.
        /// </summary>
        public DateTime QueueDateTime { get => queueDateTime; internal set => queueDateTime = value; }

        private DateTime startDateTime;
        /// <summary>
        /// Start datetime.
        /// </summary>
        public DateTime StartDateTime { get => startDateTime; internal set => startDateTime = value; }

        private DateTime endDateTime;
        /// <summary>
        /// End datetime.
        /// </summary>
        public DateTime EndDateTime { get => endDateTime; internal set => endDateTime = value; }

        private RetryInfo retryInfo;
        /// <summary>
        /// Retry information.
        /// </summary>
        public RetryInfo RetryInfo { get => retryInfo; internal set => retryInfo = value; }

        internal abstract void SetExecuteResult(object result, Exception exception, Status status, DateTime queueDateTime, RetryOption retryOption, int executeCount);
        internal abstract object GetResult();
        internal abstract ExecuteResult<object> ToObjResult();
    }
}
