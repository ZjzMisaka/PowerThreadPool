using System;

namespace PowerThreadPool.EventArguments
{
    public class WorkEventArgsBase : EventArgs
    {
        public WorkEventArgsBase() { }

        /// <summary>
        /// work id
        /// </summary>
        private string _id;
        public string ID { get => _id; internal set => _id = value; }

        private DateTime _queueDateTime;
        /// <summary>
        /// queue datetime.
        /// </summary>
        public DateTime QueueDateTime { get => _queueDateTime; internal set => _queueDateTime = value; }

        private DateTime _startDateTime;
        /// <summary>
        /// start datetime.
        /// </summary>
        public DateTime StartDateTime { get => _startDateTime; internal set => _startDateTime = value; }

        private DateTime _endDateTime;
        /// <summary>
        /// callback datetime.
        /// </summary>
        public DateTime EndDateTime { get => _endDateTime; internal set => _endDateTime = value; }
    }
}
