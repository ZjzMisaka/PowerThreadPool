﻿using System;
using System.Threading;
using PowerThreadPool.Constants;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers
{
    /// <summary>
    /// Used to ensure that the wrong Work will not be controlled when operating the Worker.
    /// </summary>
    internal class WorkGuard : IDisposable
    {
        private readonly WorkBase _work;
        private Worker _worker = null;
        private readonly bool _isHoldWork;
        private bool _disposed;

        /// <summary>
        /// Used to ensure that the wrong Work will not be controlled when operating the Worker.
        /// </summary>
        /// <param name="work"></param>
        /// <param name="isHoldWork">Ensure that the executing Work is not switched</param>
        /// <param name="needFreeze">Ensure that the target Work is not stolen</param>
        public WorkGuard(WorkBase work,
                            bool isHoldWork,
                            bool needFreeze)
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

                SpinWait.SpinUntil(() => (_worker = _work.Worker) != null || _work.IsDone);

                if (!_work.IsDone)
                {
                    // Prevent the target work from being stolen by other workers using the work-stealing algorithm when it is stopped or canceled
                    SpinWait.SpinUntil(() => _worker.WorkStealability.TrySet(WorkStealability.NotAllowed, WorkStealability.Allowed));

                    // Temporarily prevent the executing work from allowing the worker to switch to the next work when the current work is completed
                    if (_isHoldWork)
                    {
                        SpinWait.SpinUntil(() => _worker.WorkHeldState.TrySet(WorkHeldStates.Held, WorkHeldStates.NotHeld));
                    }
                }
            }
            while (_work.Worker?.ID != _worker?.ID);
            // There is a possibility of a failure in prevention, in which case _work.Worker might be null or _work.Worker might not be the same instance as the previously saved _worker.
            // In such a case, a retry is necessary. This is an extremely rare case. 
        }

        private void UnFreeze()
        {
            if (_worker != null)
            {
                _worker.WorkStealability.InterlockedValue = WorkStealability.Allowed;

                if (_isHoldWork)
                {
                    _worker.WorkHeldState.InterlockedValue = WorkHeldStates.NotHeld;
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
