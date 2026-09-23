using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext<TResult> : SynchronizationContext
    {
        private class ContinuationState
        {
            internal SendOrPostCallback _callback;
            internal object _state;
        }

        private readonly PowerPool _powerPool;
        private readonly WorkFunc<TResult> _work;
        private volatile Task<TResult> _originalTask;
        private int _done = 0;

        private readonly Func<TResult> _cachedContinuation;
        private readonly Func<bool> _cachedBeforeStop;

        private ContinuationState _slot;
        private ConcurrentQueue<ContinuationState> _overflow;
        private int _drainScheduled;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkFunc<TResult> work)
        {
            _powerPool = powerPool;
            _work = work;
            _cachedContinuation = RunContinuation;
            _cachedBeforeStop = BeforeStop;
        }

        internal void SetTask(Task<TResult> originalTask)
        {
            _originalTask = originalTask;
        }

        private bool BeforeStop()
        {
            _work.AllowEventsAndCallback = true;
            return true;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (_work.ExecuteResultBase != null)
            {
                return;
            }
            _work._canCancel.TrySet(Constants.CanCancel.Allowed, Constants.CanCancel.NotAllowed);
            _work.IsCurrentDone = false;
            _work.SetFunction(_cachedContinuation, false);

            EnqueuePost(d, state);

            if (Interlocked.CompareExchange(ref _drainScheduled, 1, 0) == 0)
            {
                _powerPool.SetWork(_work);
            }
        }

        private void EnqueuePost(SendOrPostCallback d, object state)
        {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            ContinuationState slot = Volatile.Read(ref _slot);
#else
            ContinuationState slot = Interlocked.CompareExchange(ref _slot, null, null);
#endif
            bool shouldEnqueueOverflow = false;
            bool slotIsNull = slot == null;
            if (slotIsNull)
            {
                slot = new ContinuationState
                {
                    _callback = d,
                    _state = state,
                };
                shouldEnqueueOverflow = Interlocked.CompareExchange(ref _slot, slot, null) != null;
            }
            if (!slotIsNull || shouldEnqueueOverflow)
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

        private TResult RunContinuation()
        {
            TResult res = default;
            do
            {
                ContinuationState item;
                while (TryDequeuePost(out item))
                {
                    res = InvokeOne(item._callback, item._state);
                }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
                Volatile.Write(ref _drainScheduled, 0);
#else
                Interlocked.Exchange(ref _drainScheduled, 0);
#endif
            }
            while (HasPendingPosts() && Interlocked.CompareExchange(ref _drainScheduled, 1, 0) == 0);
            return res;
        }

        private TResult InvokeOne(SendOrPostCallback d, object state)
        {
            SetSynchronizationContext(this);
            if (_work.AutoCheckStopOnAsyncTask)
            {
                _powerPool.StopIfRequested(_cachedBeforeStop);
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
                    (originalTask = _originalTask) != null, true);
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
        }
    }
}
