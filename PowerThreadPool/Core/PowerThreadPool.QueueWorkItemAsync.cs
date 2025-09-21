using System;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Helpers.Asynchronous;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
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

            Interlocked.Increment(ref _asyncWorkCount);
            _asyncWorkIDDict[option.AsyncWorkID] = new ConcurrentSet<string>();
        }

        private static void ThrowInnerIfNeeded(Task task)
        {
            if (task.Exception?.InnerException != null)
            {
                throw task.Exception.InnerException;
            }
        }

        private void RegisterCompletionWithResult<TResult>(Task<TResult> task, SynchronizationContext prevCtx, string baseAsyncWorkId)
        {
            if (task.IsCompleted && _aliveWorkDic.TryGetValue(baseAsyncWorkId, out WorkBase workDone))
            {
                // If the incoming asynchronous work doesn't execute await, this branch will be entered.
                // Requires direct setting of AllowEventsAndCallback and ExecuteResult.
                workDone.AllowEventsAndCallback = true;
                workDone.SetExecuteResult(task.Result, task.Exception, Status.Succeed);
            }

            RegisterCompletion(task, prevCtx, baseAsyncWorkId, true);
        }

        private void RegisterCompletion(Task task, SynchronizationContext prevCtx, string baseAsyncWorkId, bool hasRes = false)
        {
            if (!hasRes && task.IsCompleted && _aliveWorkDic.TryGetValue(baseAsyncWorkId, out WorkBase workDone))
            {
                // If the incoming asynchronous work doesn't execute await, this branch will be entered.
                // Requires direct setting of AllowEventsAndCallback and ExecuteResult.
                workDone.AllowEventsAndCallback = true;
                workDone.SetExecuteResult(null, task.Exception, Status.Succeed);
            }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            task.GetAwaiter().OnCompleted(() =>
#else
            task.ContinueWith(_ =>
#endif
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);

                if (_aliveWorkDic.TryGetValue(baseAsyncWorkId, out WorkBase work))
                {
                    if (work.WaitSignal != null)
                    {
                        work.WaitSignal.Set();
                    }

                    if (!work.ShouldStoreResult)
                    {
                        TryRemoveAsyncWork(baseAsyncWorkId, true);
                    }
                }

                CheckPoolIdle();
            });
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public string QueueWorkItemAsync(Func<Task> asyncFunc, Action<ExecuteResult<object>> callBack = null)
        {
            return QueueWorkItemAsync(asyncFunc, out _, callBack);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public string QueueWorkItemAsync(Func<Task> asyncFunc, WorkOption option)
        {
            return QueueWorkItemAsync(asyncFunc, out _, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public string QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, Action<ExecuteResult<TResult>> callBack = null)
        {
            return QueueWorkItemAsync<TResult>(asyncFunc, out _, callBack);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public string QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc,
                                                  WorkOption<TResult> option)
        {
            return QueueWorkItemAsync<TResult>(asyncFunc, out _, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public string QueueWorkItemAsync(Func<Task> asyncFunc, out Task task, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption workOption = new WorkOption { Callback = callBack };
            return QueueWorkItemAsync(asyncFunc, out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public string QueueWorkItemAsync(Func<Task> asyncFunc, out Task task, WorkOption option)
        {
            TaskCompletionSourceBox<ExecuteResult<object>> taskCompletionSource = new TaskCompletionSourceBox<ExecuteResult<object>>();
            task = taskCompletionSource.Task;

            PrepareAsyncWork(option);

            string id = QueueWorkItem(() =>
            {
                SynchronizationContext prevCtx = SynchronizationContext.Current;
                PowerPoolSynchronizationContext ctx = new PowerPoolSynchronizationContext(this, option);
                SynchronizationContext.SetSynchronizationContext(ctx);

                Task taskFunc = asyncFunc();
                ThrowInnerIfNeeded(taskFunc);

                ctx.SetTask(taskFunc);
                RegisterCompletion(taskFunc, prevCtx, option.BaseAsyncWorkID);
            }, option);

            _tcsDict[id] = taskCompletionSource;

            return id;
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public string QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, out Task<ExecuteResult<TResult>> task, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> workOption = new WorkOption<TResult> { Callback = callBack };
            return QueueWorkItemAsync(asyncFunc, out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public string QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, out Task<ExecuteResult<TResult>> task, WorkOption<TResult> option)
        {
            TaskCompletionSourceBox<ExecuteResult<TResult>> taskCompletionSource = new TaskCompletionSourceBox<ExecuteResult<TResult>>();
            task = taskCompletionSource.TypedTask;

            PrepareAsyncWork(option);

            string id = QueueWorkItem(() =>
            {
                SynchronizationContext prevCtx = SynchronizationContext.Current;
                PowerPoolSynchronizationContext<TResult> ctx = new PowerPoolSynchronizationContext<TResult>(this, option);
                SynchronizationContext.SetSynchronizationContext(ctx);

                Task<TResult> taskFunc = asyncFunc();
                ThrowInnerIfNeeded(taskFunc);

                ctx.SetTask(taskFunc);
                RegisterCompletionWithResult(taskFunc, prevCtx, option.BaseAsyncWorkID);

                return default;
            }, option);

            _tcsDict[id] = taskCompletionSource;

            return id;
        }
    }
}
