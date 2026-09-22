using System;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext : SynchronizationContext
    {
        private class ContinuationState
        {
            internal SendOrPostCallback _callback;
            internal object _state;
        }

        private readonly PowerPool _powerPool;
        private readonly WorkBase _workBase;
        private volatile Task _originalTask;
        private int _done = 0;

        private readonly ContinuationState _continuationState = new ContinuationState();
        private readonly Action _cachedContinuation;
        private readonly Func<bool> _cachedBeforeStop;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkBase workBase)
        {
            _powerPool = powerPool;
            _workBase = workBase;
            _cachedContinuation = RunContinuation;
            _cachedBeforeStop = BeforeStop;
        }

        internal void SetTask(Task originalTask)
        {
            _originalTask = originalTask;
        }

        private bool BeforeStop()
        {
            _workBase.AllowEventsAndCallback = true;
            return true;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (_workBase.ExecuteResultBase != null)
            {
                return;
            }
            _workBase._canCancel.TrySet(Constants.CanCancel.Allowed, Constants.CanCancel.NotAllowed);
            _workBase.IsCurrentDone = false;
            ContinuationState continuationState = _continuationState;
            continuationState._callback = d;
            continuationState._state = state;
            _workBase.SetAction(_cachedContinuation, false);
            _powerPool.SetWork(_workBase);
        }

        private void RunContinuation()
        {
            ContinuationState continuationState = _continuationState;
            SendOrPostCallback d = continuationState._callback;
            object state = continuationState._state;

            SetSynchronizationContext(this);
            if (_workBase.AutoCheckStopOnAsyncTask)
            {
                _powerPool.StopIfRequested(_cachedBeforeStop);
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
                    (originalTask = _originalTask) != null, true);
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
        }
    }
}
