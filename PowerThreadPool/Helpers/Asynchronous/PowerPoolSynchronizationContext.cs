using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using PowerThreadPool.Works;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext : SynchronizationContext
    {
        private readonly PowerPool _powerPool;
        private readonly WorkOption _workOption;
        private Task _originalTask;
        private int _done = 0;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkOption workOption)
        {
            _powerPool = powerPool;
            _workOption = workOption;
        }

        internal void SetTask(Task originalTask)
        {
            _originalTask = originalTask;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (_powerPool._asyncWorkIDDict.TryGetValue(_workOption.BaseAsyncWorkID, out ConcurrentSet<WorkID> idSet))
            {
                _workOption.AsyncWorkID = _powerPool.CreateID<object>();
                idSet.Add(_workOption.AsyncWorkID);

                _powerPool.QueueWorkItem(() =>
                {
                    SetSynchronizationContext(this);
                    _powerPool.StopIfRequested(() =>
                    {
                        _workOption.AllowEventsAndCallback = true;
                    });
                    d(state);
                    if (_originalTask.IsCompleted &&
                    Interlocked.Exchange(ref _done, 1) == 0)
                    {
                        _workOption.AllowEventsAndCallback = true;
                    }
                    return default;
                }, _workOption);
            }
        }
    }
}
