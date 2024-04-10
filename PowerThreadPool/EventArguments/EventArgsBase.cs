using System;
namespace PowerThreadPool.EventArguments
{
    public class EventArgsBase : EventArgs
    {
        public EventArgsBase() { }

        private string id;
        public string ID { get => id; internal set => id = value; }
    }
}
