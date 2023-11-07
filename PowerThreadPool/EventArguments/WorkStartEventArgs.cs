using System;

namespace PowerThreadPool.EventArguments
{
    public class WorkStartEventArgs : EventArgs
    {
        public WorkStartEventArgs() { }

        private string id;
        public string ID { get => id; internal set => id = value; }
    }
}
