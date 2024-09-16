using System;

namespace PowerThreadPool.EventArguments
{
    public class RunningWorkerCountChangedEventArgs : EventArgs
    {
        public RunningWorkerCountChangedEventArgs() { }

        private int _previousCount = 0;
        /// <summary>
        /// previous count.
        /// </summary>
        public int PreviousCount
        {
            get => _previousCount;
            internal set => _previousCount = value;
        }

        private int _nowCount = 0;
        /// <summary>
        /// now count.
        /// </summary>
        public int NowCount
        {
            get => _nowCount;
            internal set
            {
                _nowCount = value;
            }
        }
    }
}
