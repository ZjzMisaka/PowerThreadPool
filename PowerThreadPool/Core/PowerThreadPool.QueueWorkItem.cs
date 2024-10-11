using System;
using System.Threading;
using PowerThreadPool.Collections;
using PowerThreadPool.Helpers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
    public partial class PowerPool
    {
        private static readonly object[] s_emptyArray = new object[0];

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<T1>(Action<T1> action, T1 param1, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption option = new WorkOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<T1, object>.ToNormalFunc(action, param1), new object[] { param1 }, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<T1>(Action<T1> action, T1 param1, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<T1, object>.ToNormalFunc(action, param1), new object[] { param1 }, option);
        }

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
        public string QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption option = new WorkOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<T1, T2, object>.ToNormalFunc(action, param1, param2), new object[] { param1, param2 }, option);
        }

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
        public string QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<T1, T2, object>.ToNormalFunc(action, param1, param2), new object[] { param1, param2 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption option = new WorkOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<T1, T2, T3, object>.ToNormalFunc(action, param1, param2, param3), new object[] { param1, param2, param3 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<T1, T2, T3, object>.ToNormalFunc(action, param1, param2, param3), new object[] { param1, param2, param3 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption option = new WorkOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<T1, T2, T3, T4, object>.ToNormalFunc(action, param1, param2, param3, param4), new object[] { param1, param2, param3, param4 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<T1, T2, T3, T4, object>.ToNormalFunc(action, param1, param2, param3, param4), new object[] { param1, param2, param3, param4 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption option = new WorkOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<T1, T2, T3, T4, T5, object>.ToNormalFunc(action, param1, param2, param3, param4, param5), new object[] { param1, param2, param3, param4, param5 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<T1, T2, T3, T4, T5, object>.ToNormalFunc(action, param1, param2, param3, param4, param5), new object[] { param1, param2, param3, param4, param5 }, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action action, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption option = new WorkOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), s_emptyArray, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action action, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), s_emptyArray, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action<object[]> action, object[] param, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption option = new WorkOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<object[]>.ToNormalFunc(action, param), param, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action<object[]> action, object[] param, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<object[]>.ToNormalFunc(action, param), param, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> option = new WorkOption<TResult>();
            option.Callback = callBack;
            return QueueWorkItem<TResult>(DelegateHelper<T1, TResult>.ToNormalFunc(function, param1), new object[] { param1 }, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, WorkOption<TResult> option)
        {
            return QueueWorkItem<TResult>(DelegateHelper<T1, TResult>.ToNormalFunc(function, param1), new object[] { param1 }, option);
        }

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
        public string QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> option = new WorkOption<TResult>();
            option.Callback = callBack;
            return QueueWorkItem<TResult>(DelegateHelper<T1, T2, TResult>.ToNormalFunc(function, param1, param2), new object[] { param1, param2 }, option);
        }

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
        public string QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, WorkOption<TResult> option)
        {
            return QueueWorkItem<TResult>(DelegateHelper<T1, T2, TResult>.ToNormalFunc(function, param1, param2), new object[] { param1, param2 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> option = new WorkOption<TResult>();
            option.Callback = callBack;
            return QueueWorkItem<TResult>(DelegateHelper<T1, T2, T3, TResult>.ToNormalFunc(function, param1, param2, param3), new object[] { param1, param2, param3 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, WorkOption<TResult> option)
        {
            return QueueWorkItem<TResult>(DelegateHelper<T1, T2, T3, TResult>.ToNormalFunc(function, param1, param2, param3), new object[] { param1, param2, param3 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> option = new WorkOption<TResult>();
            option.Callback = callBack;
            return QueueWorkItem<TResult>(DelegateHelper<T1, T2, T3, T4, TResult>.ToNormalFunc(function, param1, param2, param3, param4), new object[] { param1, param2, param3, param4 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption<TResult> option)
        {
            return QueueWorkItem<TResult>(DelegateHelper<T1, T2, T3, T4, TResult>.ToNormalFunc(function, param1, param2, param3, param4), new object[] { param1, param2, param3, param4 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> option = new WorkOption<TResult>();
            option.Callback = callBack;
            return QueueWorkItem<TResult>(DelegateHelper<T1, T2, T3, T4, T5, TResult>.ToNormalFunc(function, param1, param2, param3, param4, param5), new object[] { param1, param2, param3, param4, param5 }, option);
        }

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
        public string QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption<TResult> option)
        {
            return QueueWorkItem<TResult>(DelegateHelper<T1, T2, T3, T4, T5, TResult>.ToNormalFunc(function, param1, param2, param3, param4, param5), new object[] { param1, param2, param3, param4, param5 }, option);
        }


        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<TResult> function, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> option = new WorkOption<TResult>();
            option.Callback = callBack;
            return QueueWorkItem<TResult>(DelegateHelper<TResult>.ToNormalFunc(function), s_emptyArray, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<TResult> function, WorkOption<TResult> option)
        {
            return QueueWorkItem<TResult>(DelegateHelper<TResult>.ToNormalFunc(function), s_emptyArray, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> option = new WorkOption<TResult>();
            option.Callback = callBack;
            return QueueWorkItem<TResult>(function, param, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, WorkOption<TResult> workOption)
        {
            if (_disposing || _disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            string workID;

            if (PoolStopping)
            {
                return null;
            }

            if (PowerPoolOption == null)
            {
                PowerPoolOption = new PowerPoolOption();
            }

            if (workOption.CustomWorkID != null)
            {
                if (_suspendedWork.ContainsKey(workOption.CustomWorkID) || _aliveWorkDic.ContainsKey(workOption.CustomWorkID))
                {
                    throw new InvalidOperationException($"The work ID '{workOption.CustomWorkID}' already exists.");
                }
                workID = workOption.CustomWorkID;
            }
            else
            {
                workID = Guid.NewGuid().ToString();
            }

            if (workOption.TimeoutOption == null && PowerPoolOption.DefaultWorkTimeoutOption != null)
            {
                workOption.TimeoutOption = PowerPoolOption.DefaultWorkTimeoutOption;
            }

            Work<TResult> work = new Work<TResult>(this, workID, function, param, workOption);

            if (work.DependencyFailed)
            {
                return workID;
            }

            Interlocked.Increment(ref _waitingWorkCount);

            if (work.Group != null)
            {
                _workGroupDic.AddOrUpdate(work.Group, new ConcurrentSet<string>() { work.ID }, (key, oldValue) => { oldValue.Add(work.ID); return oldValue; });
            }

            if (PowerPoolOption.StartSuspended)
            {
                _suspendedWork[workID] = work;
                _suspendedWorkQueue.Enqueue(workID);
            }
            else
            {
                if (workOption.Dependents == null || workOption.Dependents.Count == 0)
                {
                    SetWork(work);
                }
            }

            return workID;
        }
    }
}
