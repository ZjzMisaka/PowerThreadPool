using System;

namespace PowerThreadPool.EventArguments
{
    public class RunningTimerElapsedEventArgs
    {
        public RunningTimerElapsedEventArgs() { }

        /// <summary>
        /// The date/time when the System.Timers.Timer.Elapsed event was raised.
        /// </summary>
        public DateTime SignalTime { get; internal set; }

        /// <summary>
        /// Pool runtime duration.
        /// </summary>
        public TimeSpan RuntimeDuration { get; internal set; }
    }
}
