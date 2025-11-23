using System.Threading;
using System.Threading.Tasks;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal sealed class AsyncManualResetEvent
    {
        internal volatile TaskCompletionSource<bool> _tcs;

        public AsyncManualResetEvent(bool initialState = false)
        {
            _tcs = NewTcs();
            if (initialState)
            {
                _tcs.TrySetResult(true);
            }
        }

        internal static TaskCompletionSource<bool> NewTcs()
        {
#if (NET46_OR_GREATER || NET5_0_OR_GREATER)
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#else
            return new TaskCompletionSource<bool>();
#endif
        }

        public bool IsSet => _tcs.Task.IsCompleted;

        public Task WaitAsync()
        {
            return _tcs.Task;
        }

        public void Set()
        {
            _tcs.TrySetResult(true);
        }

        public void Reset()
        {
            while (true)
            {
                var tcs = _tcs;
                if (!tcs.Task.IsCompleted)
                {
                    return;
                }

                var newTcs = NewTcs();
                if (Interlocked.CompareExchange(ref _tcs, newTcs, tcs) == tcs)
                {
                    return;
                }
            }
        }
    }
}
