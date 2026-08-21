using System;
#if NET5_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
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
#if NET5_0_OR_GREATER
            Func<TResult> func = Unsafe.As<Func<TRes>, Func<TResult>>(ref function);
#else
            Func<TResult> func = function as Func<TResult>;
#endif
            if (isFirst)
            {
                _baseFunction = func;
            }
            _function = func;
        }
    }
}
