using System;

namespace PowerThreadPool.EventArguments
{
    public class PoolIdledEventArgs : EventArgs
    {
        public PoolIdledEventArgs() { }

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
        /// Runtime duration of the thread pool.
        /// </summary>
        public TimeSpan RuntimeDuration => EndDateTime - StartDateTime;
    }
}
