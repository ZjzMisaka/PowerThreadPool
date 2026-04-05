using System;
using System.Threading;
using PowerThreadPool.Options;

namespace PowerThreadPool.Works
{
    internal class WorkFunc<TResult> : Work<TResult>
    {
        private Func<TResult> _function;

        internal WorkFunc(PowerPool powerPool, WorkID id, Func<TResult> function, WorkOption option, AsyncWorkInfo asyncWorkInfo, CancellationTokenSource cts = null) : base(powerPool, id, option, asyncWorkInfo, cts)
        {
            _function = function;
        }

        internal override object Execute()
        {
            ++_executeCount;
            return _function();
        }
    }
}
