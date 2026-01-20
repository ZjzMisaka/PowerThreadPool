using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
        private void PrepareAsyncWork(WorkOption option, AsyncWorkInfo asyncWorkInfo)
        {
            CheckPowerPoolOption();

            asyncWorkInfo.AsyncWorkID = CreateID(option);
            asyncWorkInfo.BaseAsyncWorkID = asyncWorkInfo.AsyncWorkID;
            asyncWorkInfo.AllowEventsAndCallback = false;

            Interlocked.Increment(ref _asyncWorkCount);
            _asyncWorkIDDict[asyncWorkInfo.AsyncWorkID] = new ConcurrentSet<WorkID>();
        }

        private static void ThrowInnerIfNeeded(Task task)
        {
            if (task.Exception?.InnerException != null)
            {
                throw task.Exception.InnerException;
            }
        }

        private void RegisterCompletionWithResult<TResult>(Task<TResult> task, SynchronizationContext prevCtx, WorkID baseAsyncWorkId)
        {
            if (task.IsCompleted && _aliveWorkDic.TryGetValue(baseAsyncWorkId, out WorkBase workDone))
            {
                // If the incoming asynchronous work doesn't execute await,
                // and successfully completes without failure or being stopped, this branch will be entered.
                // Requires direct setting of AllowEventsAndCallback and ExecuteResult.
                workDone.AllowEventsAndCallback = true;
                workDone.SetExecuteResult(task.Result, task.Exception, Status.Succeed);
            }

            RegisterCompletion(task, prevCtx, baseAsyncWorkId, true);
        }

        private void RegisterCompletion(Task task, SynchronizationContext prevCtx, WorkID baseAsyncWorkId, bool hasRes = false)
        {
            if (!hasRes && task.IsCompleted && _aliveWorkDic.TryGetValue(baseAsyncWorkId, out WorkBase workDone))
            {
                // If the incoming asynchronous work doesn't execute await,
                // and successfully completes without failure or being stopped, this branch will be entered.
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
                }

                CheckPoolIdle();
            });
        }

        [ObsoleteAttribute("Use QueueWorkItem instead.", false)]
        [ExcludeFromCodeCoverage]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public WorkID QueueWorkItemAsync(Func<Task> asyncFunc, Action<ExecuteResultBase> callBack = null)
        {
            return QueueWorkItem(asyncFunc, callBack);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<Task> asyncFunc, Action<ExecuteResultBase> callBack = null)
        {
            return QueueWorkItem(asyncFunc, out _, callBack);
        }

        [ObsoleteAttribute("Use QueueWorkItem instead.", false)]
        [ExcludeFromCodeCoverage]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public WorkID QueueWorkItemAsync(Func<Task> asyncFunc, WorkOption option)
        {
            return QueueWorkItem(asyncFunc, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<Task> asyncFunc, WorkOption option)
        {
            return QueueWorkItem(asyncFunc, out _, option);
        }

        [ObsoleteAttribute("Use QueueWorkItem instead.", false)]
        [ExcludeFromCodeCoverage]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public WorkID QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, Action<ExecuteResult<TResult>> callBack = null)
        {
            return QueueWorkItem(asyncFunc, callBack);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<Task<TResult>> asyncFunc, Action<ExecuteResult<TResult>> callBack = null)
        {
            return QueueWorkItem<TResult>(asyncFunc, out _, callBack);
        }

        [ObsoleteAttribute("Use QueueWorkItem instead.", false)]
        [ExcludeFromCodeCoverage]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public WorkID QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc,
                                                  WorkOption option)
        {
            return QueueWorkItem(asyncFunc, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<Task<TResult>> asyncFunc,
                                                  WorkOption option)
        {
            return QueueWorkItem<TResult>(asyncFunc, out _, option);
        }

        [ObsoleteAttribute("Use QueueWorkItem instead.", false)]
        [ExcludeFromCodeCoverage]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public WorkID QueueWorkItemAsync(Func<Task> asyncFunc, out Task task, Action<ExecuteResultBase> callBack = null)
        {
            return QueueWorkItem(asyncFunc, out task, callBack);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<Task> asyncFunc, out Task task, Action<ExecuteResultBase> callBack = null)
        {
            WorkOption workOption = new WorkOption { Callback = callBack };
            return QueueWorkItem(asyncFunc, out task, workOption);
        }

        [ObsoleteAttribute("Use QueueWorkItem instead.", false)]
        [ExcludeFromCodeCoverage]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public WorkID QueueWorkItemAsync(Func<Task> asyncFunc, out Task task, WorkOption option)
        {
            return QueueWorkItem(asyncFunc, out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<Task> asyncFunc, out Task task, WorkOption option)
        {
            TaskCompletionSourceBox<ExecuteResult<object>> taskCompletionSource = new TaskCompletionSourceBox<ExecuteResult<object>>();
            task = taskCompletionSource.Task;

            AsyncWorkInfo asyncWorkInfo = new AsyncWorkInfo();
            PrepareAsyncWork(option, asyncWorkInfo);

            WorkID id = QueueWorkItemInnerAsync(() =>
            {
                SynchronizationContext prevCtx = SynchronizationContext.Current;
                PowerPoolSynchronizationContext ctx = new PowerPoolSynchronizationContext(this, option, asyncWorkInfo);
                SynchronizationContext.SetSynchronizationContext(ctx);

                Task taskFunc = asyncFunc();
                ThrowInnerIfNeeded(taskFunc);

                ctx.SetTask(taskFunc);
                RegisterCompletion(taskFunc, prevCtx, asyncWorkInfo.BaseAsyncWorkID);
            }, option, asyncWorkInfo);

            _tcsDict[id] = taskCompletionSource;

            return id;
        }

        [ObsoleteAttribute("Use QueueWorkItem instead.", false)]
        [ExcludeFromCodeCoverage]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public WorkID QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, out Task<ExecuteResult<TResult>> task, Action<ExecuteResult<TResult>> callBack = null)
        {
            return QueueWorkItem(asyncFunc, out task, callBack);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<Task<TResult>> asyncFunc, out Task<ExecuteResult<TResult>> task, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption workOption = new WorkOption();
            workOption.SetCallback(callBack);
            return QueueWorkItem(asyncFunc, out task, workOption);
        }

        [ObsoleteAttribute("Use QueueWorkItem instead.", false)]
        [ExcludeFromCodeCoverage]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public WorkID QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, out Task<ExecuteResult<TResult>> task, WorkOption option)
        {
            return QueueWorkItem(asyncFunc, out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<Task<TResult>> asyncFunc, out Task<ExecuteResult<TResult>> task, WorkOption option)
        {
            TaskCompletionSourceBox<ExecuteResult<TResult>> taskCompletionSource = new TaskCompletionSourceBox<ExecuteResult<TResult>>();
            task = taskCompletionSource.TypedTask;

            AsyncWorkInfo asyncWorkInfo = new AsyncWorkInfo();
            PrepareAsyncWork(option, asyncWorkInfo);

            WorkID id = QueueWorkItemInnerAsync<TResult>(() =>
            {
                SynchronizationContext prevCtx = SynchronizationContext.Current;
                PowerPoolSynchronizationContext<TResult> ctx = new PowerPoolSynchronizationContext<TResult>(this, option, asyncWorkInfo);
                SynchronizationContext.SetSynchronizationContext(ctx);

                Task<TResult> taskFunc = asyncFunc();
                ThrowInnerIfNeeded(taskFunc);

                ctx.SetTask(taskFunc);
                RegisterCompletionWithResult(taskFunc, prevCtx, asyncWorkInfo.BaseAsyncWorkID);

                return default;
            }, option, asyncWorkInfo);

            _tcsDict[id] = taskCompletionSource;

            return id;
        }
    }
}
