using System;
using System.Threading;
using PowerThreadPool.Options;

namespace PowerThreadPool.Works
{
    internal class WorkFunc<TResult> : Work<TResult>
    {
        private Func<TResult> _baseFunction;
        private Func<TResult> _function;

        internal WorkFunc()
        {
        }

        internal WorkFunc(PowerPool powerPool, WorkID id, Func<TResult> function, WorkOption option, CancellationTokenSource cts) : base(powerPool, id, option, cts)
        {
            _baseFunction = function;
            _function = function;
        }

        internal override object Execute()
        {
            ++_executeCount;
            return _function();
        }
    }
}
