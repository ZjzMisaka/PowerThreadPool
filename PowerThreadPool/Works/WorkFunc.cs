using System;
using System.Threading;
using PowerThreadPool.Options;
using static System.Collections.Specialized.BitVector32;

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

        internal override bool IsFirstAsyncWork => _baseFunction == _function;

        internal override object Execute()
        {
            ++_executeCount;
            return _function();
        }

        internal override void ResetBase()
        {
            _function = _baseFunction;
        }

        internal override void SetAction(Action action, bool isFirst) => throw new NotImplementedException();

        internal override void SetFunction<TRes>(Func<TRes> function, bool isFirst)
        {
            Func<TResult> func = function as Func<TResult>;
            if (isFirst)
            {
                _baseFunction = func;
            }
            _function = func;
        }
    }
}
