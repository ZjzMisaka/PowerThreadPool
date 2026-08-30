using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext<TResult> : SynchronizationContext
    {
        private readonly PowerPool _powerPool;
        private readonly WorkFunc<TResult> _work;
        private CancellationTokenSource _cts;
        private volatile Task<TResult> _originalTask;
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
            if (_work.ExecuteResultBase != null)
            {
                return;
            }
            _work._canCancel.TrySet(Constants.CanCancel.Allowed, Constants.CanCancel.NotAllowed);
            _work.IsCurrentDone = false;
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
                Task<TResult> originalTask = _originalTask;
                if (originalTask == null)
                {
                    // The continuation may start before SetTask publishes the task instance. 
                    // This race is more likely to surface with awaits that force the continuation to be
                    // queued/posted asynchronously (e.g., Task.Yield()), since a plain await often just
                    // runs the continuation inline (synchronously) on the same thread.
                    Spinner.Start(() =>
                        (originalTask = _originalTask) != null);
                }
                if (originalTask.IsFaulted)
                {
                    throw originalTask.Exception.InnerException;
                }
                TResult res = default;
                if (originalTask.IsCompleted && Interlocked.Exchange(ref _done, 1) == 0)
                {
                    _work.AllowEventsAndCallback = true;
                    res = originalTask.Result;
                }
                return res;
            }, false);
            Interlocked.Increment(ref _powerPool._waitingWorkCount);
            _powerPool.SetWork(_work);
        }
    }
}
