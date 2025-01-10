using System;

namespace PowerThreadPool.EventArguments
{
    public class WorkEventArgsBase : EventArgs
    {
        public WorkEventArgsBase() { }

        /// <summary>
        /// work id
        /// </summary>
        public string ID { get; internal set; }

        /// <summary>
        /// work parameter
        /// </summary>
        public object[] Parameter { get; internal set; }

        /// <summary>
        /// queue datetime.
        /// </summary>
        public DateTime QueueDateTime { get; internal set; }

        /// <summary>
        /// start datetime.
        /// </summary>
        public DateTime StartDateTime { get; internal set; }

        /// <summary>
        /// callback datetime.
        /// </summary>
        public DateTime EndDateTime { get; internal set; }
    }
}
