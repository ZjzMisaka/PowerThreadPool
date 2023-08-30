using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Option
{
    public class DestroyThreadOption
    {
        public int KeepAliveTime { get; set; } = 10000;
        public int MinThreads { get; set; } = Environment.ProcessorCount;
    }
}
