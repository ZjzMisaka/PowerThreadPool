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

        /// <summary>
        /// Queue datetime.
        /// </summary>
        public DateTime QueueDateTime { get; internal set; }

        /// <summary>
        /// Start datetime.
        /// </summary>
        public DateTime StartDateTime { get; internal set; }

        /// <summary>
        /// End datetime.
        /// </summary>
        public DateTime EndDateTime { get; internal set; }

        /// <summary>
        /// Retry information.
        /// </summary>
        public RetryInfo RetryInfo { get; internal set; }

        internal abstract void SetExecuteResult(object result, Exception exception, Status status, DateTime queueDateTime, RetryOption retryOption, int executeCount);
        internal abstract object GetResult();
        internal abstract ExecuteResult<object> ToObjResult();
    }
}
