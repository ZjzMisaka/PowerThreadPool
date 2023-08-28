using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Option
{
    public class TimeoutOption
    {
        public TimeoutOption()
        {
        }

        public int Duration { get; set; }
        public bool ForceStop { get; set; } = false;
    }
}
