using System;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    /// <summary>
    /// Since we need to support .NET Framework 4.0, we can't use source generators.
    /// Therefore, we use overloads to ensure strong typing and ease of use.
    /// </summary>
    public partial class PowerPool
    {
        private long _workIDIncrement = 0;

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1>(Action<T1> action, T1 param1, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1>(Action<T1, CancellationToken> action, T1 param1, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1>(Action<T1> action, T1 param1, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1>(action, param1), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1>(Action<T1, CancellationToken> action, T1 param1, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1>(action, param1), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, param2, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2>(Action<T1, T2, CancellationToken> action, T1 param1, T2 param2, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, param2, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1, T2>(action, param1, param2), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2>(Action<T1, T2, CancellationToken> action, T1 param1, T2 param2, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1, T2>(action, param1, param2), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, param2, param3, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3, CancellationToken> action, T1 param1, T2 param2, T3 param3, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, param2, param3, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1, T2, T3>(action, param1, param2, param3), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3, CancellationToken> action, T1 param1, T2 param2, T3 param3, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1, T2, T3>(action, param1, param2, param3), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, param2, param3, param4, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4, CancellationToken> action, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, param2, param3, param4, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1, T2, T3, T4>(action, param1, param2, param3, param4), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4, CancellationToken> action, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1, T2, T3, T4>(action, param1, param2, param3, param4), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, param2, param3, param4, param5, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5, CancellationToken> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param1, param2, param3, param4, param5, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1, T2, T3, T4, T5>(action, param1, param2, param3, param4, param5), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5, CancellationToken> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction<T1, T2, T3, T4, T5>(action, param1, param2, param3, param4, param5), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem(Action action, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem(Action<CancellationToken> action, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem(Action action, WorkOption option)
        {
            return QueueWorkItemCore<object>(action, null, option, null, new WorkAction<object>());
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem(Action<CancellationToken> action, WorkOption option)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
            Action act = () => action(cts.Token);
            return QueueWorkItemCore<object>(act, null, option, cts, new WorkAction<object>());
        }

        internal WorkID QueueAsyncWorkItemInner(Action action, WorkOption option, CancellationTokenSource cts, WorkBase work)
        {
            return QueueWorkItemCore<object>(action, null, option, cts, work);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem(Action<object[]> action, object[] param, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem(Action<object[], CancellationToken> action, object[] param, Action<ExecuteResultBase> callBack = null)
            => QueueWorkItem(action, param, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem(Action<object[]> action, object[] param, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction(action, param), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem(Action<object[], CancellationToken> action, object[] param, WorkOption option)
            => QueueWorkItem(DelegateHelper.ToNormalAction(action, param), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, TResult>(function, param1, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, TResult>(Func<T1, CancellationToken, TResult> function, T1 param1, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, TResult>(function, param1, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, TResult>(function, param1), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, TResult>(Func<T1, CancellationToken, TResult> function, T1 param1, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, CancellationToken, TResult>(function, param1), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, T2, TResult>(function, param1, param2, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, TResult>(Func<T1, T2, CancellationToken, TResult> function, T1 param1, T2 param2, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, T2, TResult>(function, param1, param2, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, TResult>(function, param1, param2), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, TResult>(Func<T1, T2, CancellationToken, TResult> function, T1 param1, T2 param2, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, CancellationToken, TResult>(function, param1, param2), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, T2, T3, TResult>(function, param1, param2, param3, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, CancellationToken, TResult> function, T1 param1, T2 param2, T3 param3, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, T2, T3, TResult>(function, param1, param2, param3, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, T3, TResult>(function, param1, param2, param3), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, CancellationToken, TResult> function, T1 param1, T2 param2, T3 param3, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, T3, CancellationToken, TResult>(function, param1, param2, param3), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, T2, T3, T4, TResult>(function, param1, param2, param3, param4, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, CancellationToken, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, T2, T3, T4, TResult>(function, param1, param2, param3, param4, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, T3, T4, TResult>(function, param1, param2, param3, param4), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, CancellationToken, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, T3, T4, CancellationToken, TResult>(function, param1, param2, param3, param4), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, T2, T3, T4, T5, TResult>(function, param1, param2, param3, param4, param5, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, CancellationToken, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<T1, T2, T3, T4, T5, TResult>(function, param1, param2, param3, param4, param5, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, T3, T4, T5, TResult>(function, param1, param2, param3, param4, param5), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <param name="param3"></param>
        /// <param name="param4"></param>
        /// <param name="param5"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, CancellationToken, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, T3, T4, T5, TResult>(function, param1, param2, param3, param4, param5), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<TResult>(Func<TResult> function, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<TResult>(function, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<TResult>(Func<CancellationToken, TResult> function, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<TResult>(function, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<TResult>(Func<TResult> function, WorkOption option)
        {
            return QueueWorkItemCore(null, function, option, null, new WorkFunc<TResult>());
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<TResult>(Func<CancellationToken, TResult> function, WorkOption option)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
            Func<TResult> func = () => function(cts.Token);
            return QueueWorkItemCore(null, func, option, cts, new WorkFunc<TResult>());
        }

        internal WorkID QueueAsyncWorkItemInner<TResult>(Func<TResult> function, WorkOption option, CancellationTokenSource cts, WorkBase workBase)
        {
            return QueueWorkItemCore(null, function, option, cts, workBase);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<TResult>(function, param, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<TResult>(Func<object[], CancellationToken, TResult> function, object[] param, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<TResult>(function, param, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<TResult>(function, param), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public WorkID QueueWorkItem<TResult>(Func<object[], CancellationToken, TResult> function, object[] param, WorkOption option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<TResult>(function, param), option);

        private WorkID QueueWorkItemCore<TResult>(Action action, Func<TResult> function, WorkOption option, CancellationTokenSource cts, WorkBase workBase)
        {
            CheckDisposed();
            CheckPowerPoolOption();

            WorkID workID = CreateID(option);
            WorkBase work = InitWork(workID, action, function, option, cts, workBase);

            bool registeredDependents = _workDependencyController.Register(work, option.Dependents, out bool workNotSuccessfullyCompleted);
            if (work._dependencyStatus.InterlockedValue == DependencyStatus.Failed)
            {
                return workID;
            }

            if (work.Group != null)
            {
                _workGroupDic.AddOrUpdate(work.Group, new ConcurrentSet<WorkID>() { work.ID }, (key, oldValue) => { oldValue.Add(work.ID); return oldValue; });
            }

            if (!PowerPoolOption.StartSuspended && PoolStopping)
            {
                _stopSuspendedWork[workID] = work;
                _stopSuspendedWorkQueue.Enqueue(workID);
                return workID;
            }

            if (!workNotSuccessfullyCompleted)
            {
                Interlocked.Increment(ref _waitingWorkCount);
            }

            SuspendOrSetWork(PowerPoolOption.StartSuspended, registeredDependents, workID, work);

            return workID;
        }

        private WorkBase InitWork<TResult>(WorkID workID, Action action, Func<TResult> function, WorkOption option, CancellationTokenSource cts, WorkBase workBase)
        {
            WorkBase work = null;
            if (action != null)
            {
                work = workBase.Init(this, workID, option, cts);
                work.SetAction(action, true);
            }
            else
            {
                work = workBase.Init(this, workID, option, cts);
                work.SetFunction(function, true);
            }
            return work;
        }

        private void SuspendOrSetWork(bool startSuspended, bool registeredDependents, WorkID workID, WorkBase work)
        {
            if (startSuspended)
            {
                _suspendedWork[workID] = work;
                _suspendedWorkQueue.Enqueue(workID);
            }
            else
            {
                if (!registeredDependents)
                {
                    SetWork(work);
                }
            }
        }

        private WorkID CreateID(WorkOption option)
        {
            WorkID workID;

            if (option.CustomWorkID != null)
            {
                if (_suspendedWork.ContainsKey(WorkID.FromString(option.CustomWorkID)) || _aliveWorkDic.ContainsKey(WorkID.FromString(option.CustomWorkID)))
                {
                    throw new InvalidOperationException($"The work ID '{option.CustomWorkID}' already exists.");
                }
                workID = WorkID.FromString(option.CustomWorkID);
            }
            else
            {
                if (PowerPoolOption.WorkIDType == WorkIDType.LongIncrement)
                {
                    workID = WorkID.FromLong(Interlocked.Increment(ref _workIDIncrement));
                }
                else
                {
                    workID = WorkID.FromGuid(Guid.NewGuid());
                }
            }

            return workID;
        }

        private void CheckPowerPoolOption()
        {
            if (PowerPoolOption == null)
            {
                PowerPoolOption = new PowerPoolOption();
            }
        }

        /// <summary>
        /// Queues a work for execution.
        /// </summary>
        /// <param name="powerPool"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static WorkID operator +(PowerPool powerPool, Action action)
            => powerPool.QueueWorkItem(action);

        /// <summary>
        /// Queues a work for execution.
        /// </summary>
        /// <param name="powerPool"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static PowerPool operator |(PowerPool powerPool, Action action)
        {
            powerPool.QueueWorkItem(action);
            return powerPool;
        }

        /// <summary>
        /// Create and return a WorkOption instance with callback.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callBack"></param>
        /// <returns>A WorkOption instance</returns>
        private WorkOption GetOption<TResult>(Action<ExecuteResult<TResult>> callBack)
        {
            WorkOption option = null;
            if (callBack == null)
            {
                option = WorkOption.DefaultInstance;
            }
            else
            {
                option = new WorkOption<TResult>
                {
                    Callback = callBack,
                };
            }
            return option;
        }

        /// <summary>
        /// Create and return a WorkOption instance with callback.
        /// </summary>
        /// <param name="callBack"></param>
        /// <returns>A WorkOption instance</returns>
        private WorkOption GetOption(Action<ExecuteResultBase> callBack)
        {
            WorkOption option = null;
            if (callBack == null)
            {
                option = WorkOption.DefaultInstance;
            }
            else
            {
                option = new WorkOption
                {
                    Callback = callBack
                };
            }
            return option;
        }
    }
}
