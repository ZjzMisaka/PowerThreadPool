using System;
namespace PowerThreadPool.EventArguments
{
    public class WorkEventArgsBase : EventArgs
    {
        public WorkEventArgsBase() { }

        /// <summary>
        /// work id
        /// </summary>
        private string id;
        public string ID { get => id; internal set => id = value; }

        private DateTime queueDateTime;
        /// <summary>
        /// queue datetime.
        /// </summary>
        public DateTime QueueDateTime { get => queueDateTime; internal set => queueDateTime = value; }

        private DateTime startDateTime;
        /// <summary>
        /// start datetime.
        /// </summary>
        public DateTime StartDateTime { get => startDateTime; internal set => startDateTime = value; }

        private DateTime endDateTime;
        /// <summary>
        /// callback datetime.
        /// </summary>
        public DateTime EndDateTime { get => endDateTime; internal set => endDateTime = value; }
    }
}
