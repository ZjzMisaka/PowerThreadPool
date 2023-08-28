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

        public int MaxThreads { get; set; } = 10;
        public TimeoutOption Timeout { get; set; } = null;
        public TimeoutOption DefaultThreadTimeout { get; set; } = null;
        public Action<ExecuteResult<object>> DefaultCallback { get; set; } = null;
    }
}
