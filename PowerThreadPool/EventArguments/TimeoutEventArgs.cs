
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.EventArguments
{
    public class TimeoutEventArgs : EventArgs
    {
        public TimeoutEventArgs() { }

        private string id;
        public string ID { get => id; internal set => id = value; }
    }
}
