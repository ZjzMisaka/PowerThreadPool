﻿using System;
using System.Timers;

namespace PowerThreadPool.Options
{
    public class RunningTimerOption
    {
        /// <summary>
        /// The time, in milliseconds, between events.
        /// </summary>
        public double Interval { get; set; }

        /// <summary>
        /// Occurs when the interval elapses, but only if the thread pool is in the Running state.
        /// </summary>
        public Action<ElapsedEventArgs> Elapsed { get; set; }
    }
}