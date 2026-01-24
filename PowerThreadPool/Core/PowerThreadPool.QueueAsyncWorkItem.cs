using System;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Helpers;
using PowerThreadPool.Helpers.Asynchronous;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    public partial class PowerPool
    {
        // Together with `PowerPool.StopIfRequested`, you might write code like this:
        // powerPool.QueueWorkItem(() =>
        // {
        //     while (true)
        //     {
        //         powerPool.StopIfRequested();
        //     }
        // });
        // However, this is actually incorrect: from the compiler’s perspective,
        // this lambda has no return path, so type inference fails.
        // In this situation, the lambda is implicitly treated as a `Func<Task>`,
        // and `QueueWorkItem(Func<Task> asyncFunc, Action<ExecuteResultBase> callBack = null)`
        // is called instead of the intended overload.
        // This is not a bug, and even if the overload resolution is "wrong", the behavior still matches expectations:
        // the work will be executed, and its lifetime/events/callbacks will still be managed correctly.
        // See unit test `TestBadOverload`:
        // https://github.com/ZjzMisaka/PowerThreadPool/blob/c4d8a6/UnitTest/QueueWorkItemTest.cs#L2064-L2089
        // Task.Run has the same issue:
        // Task.Run(() =>
        // {
        //     while (true)
        //     {
        //         // ...
        //     }
        // });
        // This will likewise resolve to the `Run(Func<Task?> function);` overload.
        // The conditions that trigger this kind of misuse are:
        // 1. Synchronous invocation
        // 2. No return path (e.g., an infinite loop)
        // 3. The call is made using a lambda expression
        // 4. The type is not explicitly specified
        // For PTP, `PowerPool.StopIfRequested` may increase the likelihood of this misuse,
        // because it exits the function via an exception. From the compiler’s point of view,
        // this does not count as a return path.
        // But since such calls:
        // 1. Require very specific conditions
        // 2. Have no side effects
        // 3. Match the behavior of `Task.Run`
        // PTP chooses to tolerate this and will not add special handling or warnings.
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

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<Task> asyncFunc, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(asyncFunc, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<Task> asyncFunc, WorkOption option)
            => QueueWorkItem(asyncFunc, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<object[], Task> asyncFunc, object[] param, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(asyncFunc, param, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="param"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<object[], Task> asyncFunc, object[] param, WorkOption option)
            => QueueWorkItem(asyncFunc, param, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1>(Func<T1, Task> asyncFunc, T1 param1, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(asyncFunc, param1, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1>(Func<T1, Task> asyncFunc, T1 param1, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2>(Func<T1, T2, Task> asyncFunc, T1 param1, T2 param2, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(asyncFunc, param1, param2, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2>(Func<T1, T2, Task> asyncFunc, T1 param1, T2 param2, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, param2, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3>(Func<T1, T2, T3, Task> asyncFunc, T1 param1, T2 param2, T3 param3, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(asyncFunc, param1, param2, param3, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3>(Func<T1, T2, T3, Task> asyncFunc, T1 param1, T2 param2, T3 param3, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, param2, param3, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(asyncFunc, param1, param2, param3, param4, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, param2, param3, param4, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(asyncFunc, param1, param2, param3, param4, param5, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, param2, param3, param4, param5, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<Task<TResult>> asyncFunc, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem(asyncFunc, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<Task<TResult>> asyncFunc, WorkOption option)
            => QueueWorkItem(asyncFunc, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<object[], Task<TResult>> asyncFunc, object[] param, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem(asyncFunc, param, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<object[], Task<TResult>> asyncFunc, object[] param, WorkOption option)
            => QueueWorkItem(asyncFunc, param, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, TResult>(Func<T1, Task<TResult>> asyncFunc, T1 param1, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem(asyncFunc, param1, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, TResult>(Func<T1, Task<TResult>> asyncFunc, T1 param1, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, TResult>(Func<T1, T2, Task<TResult>> asyncFunc, T1 param1, T2 param2, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem(asyncFunc, param1, param2, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, TResult>(Func<T1, T2, Task<TResult>> asyncFunc, T1 param1, T2 param2, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, param2, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem(asyncFunc, param1, param2, param3, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, param2, param3, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem(asyncFunc, param1, param2, param3, param4, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, param2, param3, param4, out _, option);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem(asyncFunc, param1, param2, param3, param4, param5, out _, callBack);

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption option)
            => QueueWorkItem(asyncFunc, param1, param2, param3, param4, param5, out _, option);

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

            WorkID id = QueueAsyncWorkItemInner(() =>
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

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="param"></param>
        /// <param name="task"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<object[], Task> asyncFunc, object[] param, out Task task, Action<ExecuteResultBase> callBack = null)
        {
            WorkOption workOption = new WorkOption { Callback = callBack };
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <param name="asyncFunc"></param>
        /// <param name="param"></param>
        /// <param name="task"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem(Func<object[], Task> asyncFunc, object[] param, out Task task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1>(Func<T1, Task> asyncFunc, T1 param1, out Task task, Action<ExecuteResultBase> callBack = null)
        {
            WorkOption workOption = new WorkOption { Callback = callBack };
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1>(Func<T1, Task> asyncFunc, T1 param1, out Task task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2>(Func<T1, T2, Task> asyncFunc, T1 param1, T2 param2, out Task task, Action<ExecuteResultBase> callBack = null)
        {
            WorkOption workOption = new WorkOption { Callback = callBack };
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2>(Func<T1, T2, Task> asyncFunc, T1 param1, T2 param2, out Task task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3>(Func<T1, T2, T3, Task> asyncFunc, T1 param1, T2 param2, T3 param3, out Task task, Action<ExecuteResultBase> callBack = null)
        {
            WorkOption workOption = new WorkOption { Callback = callBack };
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3>(Func<T1, T2, T3, Task> asyncFunc, T1 param1, T2 param2, T3 param3, out Task task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, out Task task, Action<ExecuteResultBase> callBack = null)
        {
            WorkOption workOption = new WorkOption { Callback = callBack };
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3, param4), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, out Task task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3, param4), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, out Task task, Action<ExecuteResultBase> callBack = null)
        {
            WorkOption workOption = new WorkOption { Callback = callBack };
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3, param4, param5), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, out Task task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3, param4, param5), out task, option);
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

            WorkID id = QueueAsyncWorkItemInner<TResult>(() =>
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

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param"></param>
        /// <param name="task"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<object[], Task<TResult>> asyncFunc, object[] param, out Task<ExecuteResult<TResult>> task, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption workOption = new WorkOption();
            workOption.SetCallback(callBack);
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="param"></param>
        /// <param name="task"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<TResult>(Func<object[], Task<TResult>> asyncFunc, object[] param, out Task<ExecuteResult<TResult>> task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, TResult>(Func<T1, Task<TResult>> asyncFunc, T1 param1, out Task<ExecuteResult<TResult>> task, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption workOption = new WorkOption();
            workOption.SetCallback(callBack);
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, TResult>(Func<T1, Task<TResult>> asyncFunc, T1 param1, out Task<ExecuteResult<TResult>> task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, TResult>(Func<T1, T2, Task<TResult>> asyncFunc, T1 param1, T2 param2, out Task<ExecuteResult<TResult>> task, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption workOption = new WorkOption();
            workOption.SetCallback(callBack);
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, TResult>(Func<T1, T2, Task<TResult>> asyncFunc, T1 param1, T2 param2, out Task<ExecuteResult<TResult>> task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, out Task<ExecuteResult<TResult>> task, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption workOption = new WorkOption();
            workOption.SetCallback(callBack);
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, out Task<ExecuteResult<TResult>> task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, out Task<ExecuteResult<TResult>> task, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption workOption = new WorkOption();
            workOption.SetCallback(callBack);
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3, param4), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, out Task<ExecuteResult<TResult>> task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3, param4), out task, option);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, out Task<ExecuteResult<TResult>> task, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption workOption = new WorkOption();
            workOption.SetCallback(callBack);
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3, param4, param5), out task, workOption);
        }

        /// <summary>
        /// Queues a async work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="asyncFunc"></param>
        /// <param name="task"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> asyncFunc, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, out Task<ExecuteResult<TResult>> task, WorkOption option)
        {
            return QueueWorkItem(DelegateHelper.ToNormalFunc(asyncFunc, param1, param2, param3, param4, param5), out task, option);
        }
    }
}
