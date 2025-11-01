using System;
using PowerThreadPool.Works;

namespace PowerThreadPool.EventArguments
{
    public class WorkEventArgsBase : EventArgs
    {
        public WorkEventArgsBase() { }

        /// <summary>
        /// work id
        /// </summary>
        public WorkID ID { get; internal set; }

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
        /// Measures the total wall-clock time that the work’s code runs on workers,
        /// excluding time spent awaiting external I/O or being suspended off-thread.
        /// </summary>
        public long Duration { get; internal set; }
    }
}
