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
    }
}
