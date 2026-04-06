using System;
using System.Threading;
using PowerThreadPool.Options;

namespace PowerThreadPool.Works
{
    internal class WorkAction<TUseless> : Work<TUseless>
    {
        private Action _action;

        internal WorkAction(PowerPool powerPool, WorkID id, Action action, WorkOption option, AsyncWorkInfo asyncWorkInfo, CancellationTokenSource cts) : base(powerPool, id, option, asyncWorkInfo, cts)
        {
            _action = action;
        }

        internal override object Execute()
        {
            ++_executeCount;
            _action();
            return null;
        }
    }
}
