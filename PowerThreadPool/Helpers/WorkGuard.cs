using System;
using System.Threading;
using PowerThreadPool.Constants;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers
{
    internal class WorkGuard : IDisposable
    {
        private readonly WorkBase _work;
        private Worker _worker = null;
        private readonly bool _isHoldWork;
        private bool _disposed;

        public WorkGuard(WorkBase work,
                            bool isHoldWork = true,
                            bool needFreeze = true)
        {
            _work = work;
            _isHoldWork = isHoldWork;

            if (needFreeze)
                Freeze();
        }

        private void Freeze()
        {
            do
            {
                UnFreeze();

                SpinWait.SpinUntil(() => (_worker = _work.Worker) != null);

                // Prevent the target work from being stolen by other workers using the work-stealing algorithm when it is stopped or canceled
                SpinWait.SpinUntil(() => _worker.StealingFlag.TrySet(WorkerStealingFlags.Reject, WorkerStealingFlags.Allow));

                // Temporarily prevent the executing work from allowing the worker to switch to the next work when the current work is completed
                if (_isHoldWork)
                {
                    SpinWait.SpinUntil(() => _worker.WorkHeld.TrySet(WorkHeldFlags.Held, WorkHeldFlags.NotHeld));
                }
            }
            while (_work.Worker?.ID != _worker.ID);
            // There is a possibility of a failure in prevention, in which case _work.Worker might be null or _work.Worker might not be the same instance as the previously saved _worker.
            // In such a case, a retry is necessary. This is an extremely rare case. 
        }

        private void UnFreeze()
        {
            if (_worker != null)
            {
                _worker.StealingFlag.InterlockedValue = WorkerStealingFlags.Allow;

                if (_isHoldWork)
                {
                    _worker.WorkHeld.InterlockedValue = WorkHeldFlags.NotHeld;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnFreeze();
                }

                _disposed = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
