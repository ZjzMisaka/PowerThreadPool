using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext : SynchronizationContext
    {
        private readonly PowerPool _powerPool;
        private readonly WorkHandle _workBase;
        private CancellationTokenSource _cts;
        private Task _originalTask;
        private int _done = 0;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkHandle workBase, CancellationTokenSource cts)
        {
            _powerPool = powerPool;
            _workBase = workBase;
            _cts = cts;
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
            _workBase.Shell.IsCurrentDone = false;
            _workBase.Shell.SetAction(() =>
            {
                SetSynchronizationContext(this);
                if (_workBase.Shell.AutoCheckStopOnAsyncTask)
                {
                    _powerPool.StopIfRequested(() =>
                    {
                        _workBase.Shell.AllowEventsAndCallback = true;
                    });
                }
                d(state);
                if (_originalTask.IsFaulted)
                {
                    throw _originalTask.Exception.InnerException;
                }
                if (_originalTask.IsCompleted &&
                Interlocked.Exchange(ref _done, 1) == 0)
                {
                    _workBase.Shell.AllowEventsAndCallback = true;
                }
            }, false);
            Interlocked.Increment(ref _powerPool._waitingWorkCount);
            _powerPool.SetWork(_workBase);
        }
    }
}
