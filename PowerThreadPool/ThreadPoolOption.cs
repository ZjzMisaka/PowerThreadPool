using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool
{
    public class ThreadPoolOption
    {
        public ThreadPoolOption(int maxThreads = 10)
        {
            MaxThreads = maxThreads;
        }

        public int MaxThreads { get; set; } = 10;
    }
}
