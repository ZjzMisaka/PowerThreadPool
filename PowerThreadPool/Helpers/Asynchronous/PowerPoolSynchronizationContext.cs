using System;
using System.Collections.Concurrent;
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

        private readonly Action _cachedContinuation;
        private readonly Func<bool> _cachedBeforeStop;

        private ContinuationState _slot;
        private ConcurrentQueue<ContinuationState> _overflow;
        private int _drainScheduled;

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
            _workBase.SetAction(_cachedContinuation, false);

            EnqueuePost(d, state);

            if (Interlocked.CompareExchange(ref _drainScheduled, 1, 0) == 0)
            {
                _powerPool.SetWork(_workBase);
            }
        }

        private void EnqueuePost(SendOrPostCallback d, object state)
        {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            ContinuationState slot = Volatile.Read(ref _slot);
#else
            ContinuationState slot = Interlocked.CompareExchange(ref _slot, null, null);
#endif
            if (slot == null)
            {
                slot = new ContinuationState
                {
                    _callback = d,
                    _state = state,
                };
                if (Interlocked.CompareExchange(ref _slot, slot, null) != null)
                {
                    EnqueueOverflow(d, state);
                }
            }
            else
            {
                EnqueueOverflow(d, state);
            }
        }

        private void EnqueueOverflow(SendOrPostCallback d, object state)
        {
            ConcurrentQueue<ContinuationState> overflow = _overflow;
            if (overflow == null)
            {
                overflow = new ConcurrentQueue<ContinuationState>();
                if (Interlocked.CompareExchange(ref _overflow, overflow, null) != null)
                {
                    overflow = _overflow;
                }
            }
            overflow.Enqueue(new ContinuationState
            {
                _callback = d,
                _state = state,
            });
        }

        private bool TryDequeuePost(out ContinuationState item)
        {
            ContinuationState slot = Interlocked.Exchange(ref _slot, null);
            if (slot != null)
            {
                item = slot;
                return true;
            }
            ConcurrentQueue<ContinuationState> overflow = _overflow;
            if (overflow != null && overflow.TryDequeue(out item))
            {
                return true;
            }
            item = null;
            return false;
        }

        private bool HasPendingPosts()
        {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            if (Volatile.Read(ref _slot) != null)
#else
            if (Interlocked.CompareExchange(ref _slot, null, null) != null)
#endif
            {
                return true;
            }
            ConcurrentQueue<ContinuationState> overflow = _overflow;
            return overflow != null && !overflow.IsEmpty;
        }

        private void RunContinuation()
        {
            do
            {
                ContinuationState item;
                while (TryDequeuePost(out item))
                {
                    InvokeOne(item._callback, item._state);
                }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
                Volatile.Write(ref _drainScheduled, 0);
#else
                Interlocked.Exchange(ref _drainScheduled, 0);
#endif
            }
            while (HasPendingPosts() && Interlocked.CompareExchange(ref _drainScheduled, 1, 0) == 0);
        }

        private void InvokeOne(SendOrPostCallback d, object state)
        {
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
