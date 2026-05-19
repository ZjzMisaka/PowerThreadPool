namespace PowerThreadPool.Options
{
    public class TimeoutOption
    {
        /// <summary>
        /// The maximum amount of time (ms).
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// If ShouldStop is true, the work will be stopped when the timeout is reached.
        /// </summary>
        public bool ShouldStop { get; set; } = true;

        /// <summary>
        /// If ForceStop is true, Thread.Interrupt() will be called when the timeout is reached.
        /// </summary>
        public bool ForceStop { get; set; } = false;
    }
}
