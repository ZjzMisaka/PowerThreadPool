using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool
{
    public class ThreadStartEventArgs : EventArgs
    {
        public ThreadStartEventArgs() { }
        public string ThreadId { get; set; }
    }
}
