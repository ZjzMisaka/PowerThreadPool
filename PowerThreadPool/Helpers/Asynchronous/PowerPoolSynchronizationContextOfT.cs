using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext<TResult> : SynchronizationContext
    {
        private readonly PowerPool _powerPool;
        private readonly WorkFunc<TResult> _work;
        private CancellationTokenSource _cts;
        private Task<TResult> _originalTask;
        private int _done = 0;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkFunc<TResult> work, CancellationTokenSource cts)
        {
            _powerPool = powerPool;
            _work = work;
            _cts = cts;
        }

        internal void SetTask(Task<TResult> originalTask)
        {
            _originalTask = originalTask;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (_work.WorkHandle.ExecuteResultBase != null)
            {
                return;
            }
            WorkBase workBase = _work as WorkBase;
            workBase.WorkHandle._canCancel.TrySet(Constants.CanCancel.Allowed, Constants.CanCancel.NotAllowed);
            workBase.IsCurrentDone = false;
            _work.SetFunction(() =>
            {
                SetSynchronizationContext(this);
                if (_work.AutoCheckStopOnAsyncTask)
                {
                    _powerPool.StopIfRequested(() =>
                    {
                        _work.AllowEventsAndCallback = true;
                    });
                }
                d(state);
                if (_originalTask.IsFaulted)
                {
                    throw _originalTask.Exception.InnerException;
                }
                TResult res = default;
                if (_originalTask.IsCompleted && Interlocked.Exchange(ref _done, 1) == 0)
                {
                    _work.AllowEventsAndCallback = true;
                    res = _originalTask.Result;
                }
                return res;
            }, false);
            Interlocked.Increment(ref _powerPool._waitingWorkCount);
            _powerPool.SetWork(workBase.WorkHandle);
        }
    }
}
