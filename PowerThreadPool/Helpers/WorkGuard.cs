using System;
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
        private bool _disposed;

        /// <summary>
        /// Used to ensure that the wrong Work will not be controlled when operating the Worker.
        /// </summary>
        /// <param name="work"></param>
        /// <param name="needFreeze">Ensure that the target Work is not stolen</param>
        public WorkGuard(WorkBase work,
                            bool needFreeze)
        {
            _work = work;

            if (needFreeze)
            {
                Freeze();
            }
        }

        private void Freeze()
        {
            do
            {
                // Do not perform UnFreeze when _worker is null. Cases where _worker is null:
                // 1. When first entering the loop, _worker is null.
                // 2. Inside the Spinner.Start() function below, _work.Worker is null 
                //    (e.g., the task has already been completed, or during the work-stealing logic).
                UnFreeze();

                // First, retrieve the Worker from _work, then prevent it from performing work-stealing 
                // and ensure it does not switch to the next task after completing the current one 
                // (temporarily keeping the Worker bound to this task).
                // Perform another check to see if the current Worker in _work matches the previously retrieved Worker. 
                // If they are different, it means the Freeze operation has failed and a retry is needed.
                Spinner.Start(() => (_worker = _work.Worker) != null || _work.IsDone);

                if (!_work.IsDone)
                {
                    // Prevent the target work from being stolen by other workers using the work-stealing algorithm when it is stopped or canceled
                    Spinner.Start(() => _worker.WorkStealability.TrySet(WorkStealability.NotAllowed, WorkStealability.Allowed));

                    // Temporarily prevent the executing work from allowing the worker to switch to the next work when the current work is completed
                    Spinner.Start(() => _worker.WorkHeldState.TrySet(WorkHeldStates.Held, WorkHeldStates.NotHeld));
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

                _worker.WorkHeldState.InterlockedValue = WorkHeldStates.NotHeld;
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
