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
        private readonly AsyncWorkInfo _asyncWorkInfo;
        private Task _originalTask;
        private int _done = 0;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkOption workOption, AsyncWorkInfo asyncWorkInfo)
        {
            _powerPool = powerPool;
            _workOption = workOption;
            _asyncWorkInfo = asyncWorkInfo;
        }

        internal void SetTask(Task originalTask)
        {
            _originalTask = originalTask;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (_powerPool._asyncWorkIDDict.TryGetValue(_asyncWorkInfo.BaseAsyncWorkID, out ConcurrentSet<WorkID> idSet))
            {
                _asyncWorkInfo.AsyncWorkID = _powerPool.CreateID<object>();
                idSet.Add(_asyncWorkInfo.AsyncWorkID);

                _powerPool.QueueWorkItemInnerAsync(() =>
                {
                    SetSynchronizationContext(this);
                    _powerPool.StopIfRequested(() =>
                    {
                        _asyncWorkInfo.AllowEventsAndCallback = true;
                    });
                    d(state);
                    if (_originalTask.IsCompleted &&
                    Interlocked.Exchange(ref _done, 1) == 0)
                    {
                        _asyncWorkInfo.AllowEventsAndCallback = true;
                    }
                    return default;
                }, _workOption, _asyncWorkInfo);
            }
        }
    }
}
