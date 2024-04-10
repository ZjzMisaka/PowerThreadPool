using System;

namespace PowerThreadPool.EventArguments
{
    public class TimeoutEventArgs : EventArgs
    {
        public TimeoutEventArgs() { }

        private string id;
        public string ID { get => id; internal set => id = value; }
    }
}
