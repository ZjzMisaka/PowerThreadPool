using System;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Utils;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    public partial class PowerPool
    {
        private void PrepareAsyncWork<T>(WorkOption<T> option)
        {
            CheckPowerPoolOption();

            option.AsyncWorkID = CreateID(option);
            option.BaseAsyncWorkID = option.AsyncWorkID;
            option.AllowEventsAndCallback = false;

            _asyncWorkIDDict[option.AsyncWorkID] = new ConcurrentSet<string>();
        }

        private static void ThrowInnerIfNeeded(Task task)
        {
            if (task.Exception?.InnerException != null)
            {
                throw task.Exception.InnerException;
            }
        }

        private void RegisterCompletion(Task task, SynchronizationContext prevCtx, string baseAsyncWorkId)
        {
            task.ContinueWith(_ =>
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
                
                if (_aliveWorkDic.TryGetValue(baseAsyncWorkId, out WorkBase work))
                {
                    if (work.WaitSignal != null)
                    {
                        work.WaitSignal.Set();
                    }
                }

                if (!work.ShouldStoreResult)
                {
                    _asyncWorkIDDict.TryRemove(baseAsyncWorkId, out ConcurrentSet<string> set);
                }

                CheckPoolIdle();
            });
        }

        public string QueueWorkItemAsync(Func<Task> asyncFunc, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption workOption = new WorkOption { Callback = callBack };
            return QueueWorkItemAsync(asyncFunc, workOption);
        }

        public string QueueWorkItemAsync(Func<Task> asyncFunc, WorkOption option)
        {
            PrepareAsyncWork(option);

            return QueueWorkItem(() =>
            {
                var prevCtx = SynchronizationContext.Current;
                var ctx = new PowerPoolSynchronizationContext(this, option);
                SynchronizationContext.SetSynchronizationContext(ctx);

                Task task = asyncFunc();
                ThrowInnerIfNeeded(task);

                ctx.SetTask(task);
                RegisterCompletion(task, prevCtx, option.BaseAsyncWorkID);
            }, option);
        }

        public string QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> workOption = new WorkOption<TResult> { Callback = callBack };
            return QueueWorkItemAsync(asyncFunc, workOption);
        }

        public string QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc,
                                                  WorkOption<TResult> option)
        {
            PrepareAsyncWork(option);

            return QueueWorkItem(() =>
            {
                var prevCtx = SynchronizationContext.Current;
                var ctx = new PowerPoolSynchronizationContext<TResult>(this, option);
                SynchronizationContext.SetSynchronizationContext(ctx);

                Task task = asyncFunc();
                ThrowInnerIfNeeded(task);

                ctx.SetTask(task);
                RegisterCompletion(task, prevCtx, option.BaseAsyncWorkID);

                return default;
            }, option);
        }
    }
}
