using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using PowerThreadPool.Options;

namespace PowerThreadPool.Works
{
    internal class WorkAction<TUseless> : Work<TUseless>
    {
        private Action _baseAction;
        private Action _action;

        internal WorkAction()
        {
        }

        internal override bool IsFirstAsyncWork => _baseAction == _action;

        internal override bool IsFunc => false;

        internal override object Execute()
        {
            ++_executeCount;
            _action();
            return null;
        }

        internal override void ResetBase()
        {
            _action = _baseAction;
        }

        internal override void SetAction(Action action, bool isFirst)
        {
            if (isFirst)
            {
                _baseAction = action;
            }
            _action = action;
        }

        [ExcludeFromCodeCoverage]
        internal override void SetFunction<TResult>(Func<TResult> function, bool isFirst)
            => throw new NotImplementedException();
    }
}
