using System;
using PowerThreadPool.Constants;
using System.Threading;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers
{
    internal class WorkerLocker : IDisposable
    {
        private readonly WorkBase _work;
        private Worker _worker = null;
        private readonly bool _isHoldWork;
        private bool _disposed;

        public WorkerLocker(WorkBase work,
                            bool isHoldWork = true,
                            bool isLock = true)
        {
            _work = work;
            _isHoldWork = isHoldWork;

            if (isLock)
                Lock();
        }

        private void Lock()
        {
            do
            {
                Unlock();

                SpinWait.SpinUntil(() => (_worker = _work.Worker) != null);

                SpinWait.SpinUntil(() => _worker.StealingLock.TrySet(WorkerStealingFlags.Locked, WorkerStealingFlags.Unlocked));

                if (_isHoldWork)
                {
                    SpinWait.SpinUntil(() => _worker.WorkHeld.TrySet(WorkHeldFlags.Held, WorkHeldFlags.NotHeld));
                }
            }
            while (_work.Worker == null || _work.Worker != null && _work.Worker.ID != _worker.ID);
        }

        private void Unlock()
        {
            if (_worker != null)
            {
                _worker.StealingLock.InterlockedValue = WorkerStealingFlags.Unlocked;

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
                    Unlock();
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
