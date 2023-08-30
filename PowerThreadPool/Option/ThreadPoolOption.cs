using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Option
{

    public class ThreadPoolOption
    {
        public ThreadPoolOption()
        {
        }

        public int MaxThreads { get; set; } = Environment.ProcessorCount * 2;
        public DestroyThreadOption DestroyThreadOption { get; set; } = null;
        public TimeoutOption Timeout { get; set; } = null;
        public TimeoutOption DefaultThreadTimeout { get; set; } = null;
        public Action<ExecuteResultBase> DefaultCallback { get; set; } = null;
    }
}
