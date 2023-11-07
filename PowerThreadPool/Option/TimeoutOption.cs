namespace PowerThreadPool.Option
{
    public class TimeoutOption
    {
        public TimeoutOption()
        {
        }

        /// <summary>
        /// The maximum amount of time (ms)
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// If forceStop is true, Thread.Interrupt() and Thread.Join() will be called.
        /// </summary>
        public bool ForceStop { get; set; } = false;
    }
}
