using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext : SynchronizationContext
    {
        private readonly PowerPool _powerPool;
        private readonly WorkBase _workBase;
        private CancellationTokenSource _cts;
        private Task _originalTask;
        private int _done = 0;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkBase workBase, CancellationTokenSource cts)
        {
            _powerPool = powerPool;
            _workBase = workBase;
            _cts = cts;
        }

        internal void SetTask(Task originalTask)
        {
            _originalTask = originalTask;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _workBase.SetAction(() =>
            {
                SetSynchronizationContext(this);
                if (_workBase.AutoCheckStopOnAsyncTask)
                {
                    _powerPool.StopIfRequested(() =>
                    {
                        _workBase.AllowEventsAndCallback = true;
                    });
                }
                d(state);
                if (_originalTask.IsFaulted)
                {
                    throw _originalTask.Exception.InnerException;
                }
                if (_originalTask.IsCompleted &&
                Interlocked.Exchange(ref _done, 1) == 0)
                {
                    _workBase.AllowEventsAndCallback = true;
                }
            });
            _powerPool.SetWork(_workBase);
        }
    }
}
