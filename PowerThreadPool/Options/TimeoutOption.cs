namespace PowerThreadPool.Options
{
    public class TimeoutOption
    {
        /// <summary>
        /// The maximum amount of time (ms).
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// If forceStop is true, Thread.Interrupt() will be called.
        /// </summary>
        public bool ForceStop { get; set; } = false;
    }
}
