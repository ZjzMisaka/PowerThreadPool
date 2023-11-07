
using System;

namespace PowerThreadPool.EventArguments
{
    public class ForceStopEventArgs : EventArgs
    {
        public ForceStopEventArgs() { }

        private string id;
        public string ID { get => id; internal set => id = value; }
    }
}
