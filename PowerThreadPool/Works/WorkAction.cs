using System;
using System.Threading;
using PowerThreadPool.Options;

namespace PowerThreadPool.Works
{
    internal class WorkAction<TUseless> : Work<TUseless>
    {
        private Action _baseAction;
        private Action _action;

        internal WorkAction(PowerPool powerPool, WorkID id, Action action, WorkOption option, CancellationTokenSource cts) : base(powerPool, id, option, cts)
        {
            _baseAction = action;
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
