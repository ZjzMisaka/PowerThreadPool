using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext<TResult> : SynchronizationContext
    {
        private readonly PowerPool _powerPool;
        private readonly WorkFunc<TResult> _work;
        private CancellationTokenSource _cts;
        private Task<TResult> _originalTask;
        private int _done = 0;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkFunc<TResult> work, CancellationTokenSource cts)
        {
            _powerPool = powerPool;
            _work = work;
            _cts = cts;
        }

        internal void SetTask(Task<TResult> originalTask)
        {
            _originalTask = originalTask;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _work.SetFunction(() =>
            {
                SetSynchronizationContext(this);
                if (_work.AutoCheckStopOnAsyncTask)
                {
                    _powerPool.StopIfRequested(() =>
                    {
                        _work.AllowEventsAndCallback = true;
                    });
                }
                d(state);
                if (_originalTask.IsFaulted)
                {
                    throw _originalTask.Exception.InnerException;
                }
                TResult res = default;
                if (_originalTask.IsCompleted && Interlocked.Exchange(ref _done, 1) == 0)
                {
                    _work.AllowEventsAndCallback = true;
                    res = _originalTask.Result;
                }
                return res;
            });
            _powerPool.SetWork(_work);
        }
    }
}
