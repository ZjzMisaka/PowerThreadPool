using System;

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
        /// Succeed or failed.
        /// </summary>
        public Status Status { get => status; internal set => status = value; }

        private Exception exception;
        /// <summary>
        /// If failed, Exception will be setted here.
        /// </summary>
        public Exception Exception { get => exception; internal set => exception = value; }

        private DateTime queueDateTime;
        /// <summary>
        /// queue datetime.
        /// </summary>
        public DateTime QueueDateTime { get => queueDateTime; internal set => queueDateTime = value; }

        private DateTime executeDateTime;
        /// <summary>
        /// execute datetime.
        /// </summary>
        public DateTime ExecuteDateTime { get => executeDateTime; internal set => executeDateTime = value; }

        private DateTime endDateTime;
        /// <summary>
        /// callback datetime.
        /// </summary>
        public DateTime EndDateTime { get => endDateTime; internal set => endDateTime = value; }

        internal abstract void SetExecuteResult(object result, Exception exception, Status status, DateTime queueDateTime);
        internal abstract object GetResult();
        internal abstract ExecuteResult<object> ToObjResult();
    }
}
