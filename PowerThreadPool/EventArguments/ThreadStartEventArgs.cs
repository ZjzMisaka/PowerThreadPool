using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.EventArguments
{
    public class ThreadStartEventArgs : EventArgs
    {
        public ThreadStartEventArgs() { }

        private string id;
        public string ID { get => id; internal set => id = value; }
    }
}
