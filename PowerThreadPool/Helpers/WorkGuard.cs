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

                SpinWait.SpinUntil(() => _worker.StealingFlag.TrySet(WorkerStealingFlags.Reject, WorkerStealingFlags.Allow));

                if (_isHoldWork)
                {
                    SpinWait.SpinUntil(() => _worker.WorkHeld.TrySet(WorkHeldFlags.Held, WorkHeldFlags.NotHeld));
                }
            }
            while (_work.Worker == null || (_work.Worker != null && _work.Worker.ID != _worker.ID));
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
