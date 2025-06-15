using System;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Options;

namespace PowerThreadPool.Utils
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
            _workOption.AsyncWorkID = _powerPool.CreateID<object>();
            _powerPool._asyncWorkIDDict[_workOption.BaseAsyncWorkID].Add(_workOption.AsyncWorkID);

            _powerPool.QueueWorkItem(() =>
            {
                var prevCtx = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(this);
                d(state);
                if (_originalTask.IsCompleted &&
                Interlocked.Exchange(ref _done, 1) == 0)
                {
                    _workOption.AllowEventsAndCallback = true;
                }
            }, _workOption);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            d(state);
        }
    }

    internal class PowerPoolSynchronizationContext<TResult> : SynchronizationContext
    {
        private readonly PowerPool _powerPool;
        private readonly WorkOption<TResult> _workOption;
        private Task _originalTask;
        private int _done = 0;

        internal PowerPoolSynchronizationContext(PowerPool powerPool, WorkOption<TResult> workOption)
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
            _workOption.AsyncWorkID = _powerPool.CreateID<object>();
            _powerPool._asyncWorkIDDict[_workOption.BaseAsyncWorkID].Add(_workOption.AsyncWorkID);

            _powerPool.QueueWorkItem(() =>
            {
                var prevCtx = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(this);
                d(state);
                if (_originalTask.IsCompleted &&
                Interlocked.Exchange(ref _done, 1) == 0)
                {
                    _workOption.AllowEventsAndCallback = true;
                }
                return default;
            }, _workOption);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            d(state);
        }
    }
}
