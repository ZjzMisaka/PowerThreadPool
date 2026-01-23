using System;
using PowerThreadPool.Results;

namespace PowerThreadPool.Options
{
    public class WorkOption<T> : WorkOption
    {
        public new Action<ExecuteResult<T>> Callback { get; set; } = null;
    }
}
