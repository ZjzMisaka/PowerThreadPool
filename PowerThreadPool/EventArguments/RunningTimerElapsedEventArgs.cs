using System;

namespace PowerThreadPool.EventArguments
{
    public class RunningTimerElapsedEventArgs
    {
        public RunningTimerElapsedEventArgs() { }

        private DateTime _signalTime;
        /// <summary>
        /// The date/time when the System.Timers.Timer.Elapsed event was raised.
        /// </summary>
        public DateTime SignalTime
        {
            get => _signalTime.ToLocalTime();
            internal set => _signalTime = value;
        }

        /// <summary>
        /// Pool runtime duration.
        /// </summary>
        public TimeSpan RuntimeDuration { get; internal set; }
    }
}
