using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Option
{

    public class ThreadOption<TResult>
    {
        public ThreadOption()
        {
        }

        public TimeoutOption Timeout { get; set; } = null;

        public Action<ExecuteResult<TResult>> Callback { get; set; } = null;

        public int Priority { get; set; } = 0;
    }

    public class ThreadOption : ThreadOption<object>
    {
        public ThreadOption()
        {
        }
    }
}
