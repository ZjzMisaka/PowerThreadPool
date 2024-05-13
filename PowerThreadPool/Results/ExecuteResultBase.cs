using System;
using PowerThreadPool.Options;

namespace PowerThreadPool.Results
{
    public enum Status { Succeed, Failed, Canceled, Stopped, ForceStopped }

    public abstract class ExecuteResultBase
    {
        private string _id;
        /// <summary>
        /// Work id.
        /// </summary>
        public string ID { get => _id; internal set => _id = value; }

        private Status _status;
        /// <summary>
        /// Status of the work.
        /// </summary>
        public Status Status { get => _status; internal set => _status = value; }

        private Exception _exception;
        /// <summary>
        /// If failed, Exception will be setted here.
        /// </summary>
        public Exception Exception { get => _exception; internal set => _exception = value; }

        private DateTime _queueDateTime;
        /// <summary>
        /// Queue datetime.
        /// </summary>
        public DateTime QueueDateTime { get => _queueDateTime; internal set => _queueDateTime = value; }

        private DateTime _startDateTime;
        /// <summary>
        /// Start datetime.
        /// </summary>
        public DateTime StartDateTime { get => _startDateTime; internal set => _startDateTime = value; }

        private DateTime _endDateTime;
        /// <summary>
        /// End datetime.
        /// </summary>
        public DateTime EndDateTime { get => _endDateTime; internal set => _endDateTime = value; }

        private RetryInfo _retryInfo;
        /// <summary>
        /// Retry information.
        /// </summary>
        public RetryInfo RetryInfo { get => _retryInfo; internal set => _retryInfo = value; }

        internal abstract void SetExecuteResult(object result, Exception exception, Status status, DateTime queueDateTime, RetryOption retryOption, int executeCount);
        internal abstract object GetResult();
        internal abstract ExecuteResult<object> ToObjResult();
    }
}
