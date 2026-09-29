using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using PowerThreadPool.Options;

namespace PowerThreadPool.Works
{
    internal class WorkAction<TUseless> : Work<TUseless>
    {
        // Only written when an async continuation replaces the action (SetAction with
        // isFirst == false); stays null on the synchronous path.
        private Action _baseAction;
        private Action _action;

        internal WorkAction()
        {
        }

        internal override bool IsFirstAsyncWork => _baseAction == null;

        internal override object Execute()
        {
            ++_executeCount;
            _action();
            return null;
        }

        internal override void ResetBase()
        {
            if (_baseAction != null)
            {
                _action = _baseAction;
                _baseAction = null;
            }
        }

        internal override void SetAction(Action action, bool isFirst)
        {
            if (!isFirst && _baseAction == null)
            {
                _baseAction = _action;
            }
            _action = action;
        }

        [ExcludeFromCodeCoverage]
        internal override void SetFunction<TResult>(Func<TResult> function, bool isFirst)
            => throw new NotImplementedException();
    }
}
