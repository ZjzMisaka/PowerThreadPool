using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;

namespace PowerThreadPool.Helpers.Asynchronous
{
    internal class PowerPoolSynchronizationContext<TResult> : SynchronizationContext
    {
        private readonly PowerPool _powerPool;
        private readonly WorkOption<TResult> _workOption;
        private Task<TResult> _originalTask;
        private int _done = 0;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkOption<TResult> workOption)
        {
            _powerPool = powerPool;
            _workOption = workOption;
        }

        internal void SetTask(Task<TResult> originalTask)
        {
            _originalTask = originalTask;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (_powerPool._asyncWorkIDDict.TryGetValue(_workOption.BaseAsyncWorkID, out ConcurrentSet<string> idSet))
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
                    TResult res = default;
                    if (_originalTask.IsCompleted && Interlocked.Exchange(ref _done, 1) == 0)
                    {
                        _workOption.AllowEventsAndCallback = true;
                        res = _originalTask.Result;
                    }
                    return res;
                }, _workOption);
            }
        }
    }
}
