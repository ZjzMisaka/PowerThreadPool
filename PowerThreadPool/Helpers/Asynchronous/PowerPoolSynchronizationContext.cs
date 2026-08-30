using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext : SynchronizationContext
    {
        private readonly PowerPool _powerPool;
        private readonly WorkBase _workBase;
        private volatile Task _originalTask;
        private int _done = 0;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkBase workBase)
        {
            _powerPool = powerPool;
            _workBase = workBase;
        }

        internal void SetTask(Task originalTask)
        {
            _originalTask = originalTask;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (_workBase.ExecuteResultBase != null)
            {
                return;
            }
            _workBase._canCancel.TrySet(Constants.CanCancel.Allowed, Constants.CanCancel.NotAllowed);
            _workBase.IsCurrentDone = false;
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
                Task originalTask = _originalTask;
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
                if (originalTask.IsCompleted &&
                Interlocked.Exchange(ref _done, 1) == 0)
                {
                    _workBase.AllowEventsAndCallback = true;
                }
            }, false);
            Interlocked.Increment(ref _powerPool._waitingWorkCount);
            _powerPool.SetWork(_workBase);
        }
    }
}
