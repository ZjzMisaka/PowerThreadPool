using System;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool.Collections;
using PowerThreadPool.Helpers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Utils;
using PowerThreadPool.Works;

namespace PowerThreadPool
{
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
        public string QueueWorkItem<T1>(Action<T1> action, T1 param1, Action<ExecuteResult<object>> callBack = null)
            => QueueWorkItem(action, param1, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<T1>(Action<T1> action, T1 param1, WorkOption option)
            => QueueWorkItem<object>(DelegateHelper.ToNormalFunc<T1, object>(action, param1), option);

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
        public string QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, WorkOption option)
            => QueueWorkItem<object>(DelegateHelper.ToNormalFunc<T1, T2, object>(action, param1, param2), option);

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
        public string QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, WorkOption option)
            => QueueWorkItem<object>(DelegateHelper.ToNormalFunc<T1, T2, T3, object>(action, param1, param2, param3), option);

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
        public string QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption option)
            => QueueWorkItem<object>(DelegateHelper.ToNormalFunc<T1, T2, T3, T4, object>(action, param1, param2, param3, param4), option);

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
        public string QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption option)
            => QueueWorkItem<object>(DelegateHelper.ToNormalFunc<T1, T2, T3, T4, T5, object>(action, param1, param2, param3, param4, param5), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action action, Action<ExecuteResult<object>> callBack = null)
            => QueueWorkItem(action, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action action, WorkOption option)
            => QueueWorkItem<object>(DelegateHelper.ToNormalFunc<object>(action), option);

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action<object[]> action, object[] param, Action<ExecuteResult<object>> callBack = null)
            => QueueWorkItem(action, param, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action<object[]> action, object[] param, WorkOption option)
            => QueueWorkItem<object>(DelegateHelper.ToNormalFunc<object[]>(action, param), option);

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
        public string QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, WorkOption<TResult> option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, TResult>(function, param1), option);

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
        public string QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, WorkOption<TResult> option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, TResult>(function, param1, param2), option);

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
        public string QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, WorkOption<TResult> option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, T3, TResult>(function, param1, param2, param3), option);

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
        public string QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, WorkOption<TResult> option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, T3, T4, TResult>(function, param1, param2, param3, param4), option);

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
        public string QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, WorkOption<TResult> option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<T1, T2, T3, T4, T5, TResult>(function, param1, param2, param3, param4, param5), option);


        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<TResult> function, Action<ExecuteResult<TResult>> callBack = null)
            => QueueWorkItem<TResult>(function, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<TResult> function, WorkOption<TResult> option)
        {
            CheckDisposed();

            string workID;

            CheckPowerPoolOption();

            if (option.AsyncWorkID != null)
            {
                workID = option.AsyncWorkID;
            }
            else
            {
                workID = CreateID(option);
            }

            Work<TResult> work = new Work<TResult>(this, workID, function, option);

            _workDependencyController.Register(work, option.Dependents);
            if (work.DependencyFailed)
            {
                return workID;
            }

            if (work.Group != null)
            {
                _workGroupDic.AddOrUpdate(work.Group, new ConcurrentSet<string>() { work.ID }, (key, oldValue) => { oldValue.Add(work.ID); return oldValue; });
            }

            if (!PowerPoolOption.StartSuspended && PoolStopping)
            {
                _stopSuspendedWork[workID] = work;
                _stopSuspendedWorkQueue.Enqueue(workID);
                return workID;
            }

            Interlocked.Increment(ref _waitingWorkCount);

            if (PowerPoolOption.StartSuspended)
            {
                _suspendedWork[workID] = work;
                _suspendedWorkQueue.Enqueue(workID);
            }
            else
            {
                if (option.Dependents == null || option.Dependents.Count == 0)
                {
                    SetWork(work);
                }
            }

            return workID;
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
            => QueueWorkItem<TResult>(function, param, GetOption(callBack));

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, WorkOption<TResult> option)
            => QueueWorkItem<TResult>(DelegateHelper.ToNormalFunc<TResult>(function, param), option);





        public string QueueWorkItemAsync(Func<Task> asyncFunc, Action<ExecuteResult<object>> callBack = null)
        {
            return QueueWorkItemAsync(asyncFunc, GetOption(callBack));
        }

        public string QueueWorkItemAsync(Func<Task> asyncFunc, WorkOption option)
        {
            CheckPowerPoolOption();

            option.AsyncWorkID = CreateID(option);
            option.BaseAsyncWorkID = option.AsyncWorkID;
            option.AllowEventsAndCallback = false;
            _asyncWorkIDDict[option.AsyncWorkID] = new ConcurrentSet<string>();
            return QueueWorkItem(() =>
            {
                SynchronizationContext prevCtx = SynchronizationContext.Current;
                Task task;
                PowerPoolSynchronizationContext ctx = new PowerPoolSynchronizationContext(this, option);
                SynchronizationContext.SetSynchronizationContext(ctx);
                task = asyncFunc();
                ctx.SetTask(task);
                task.ContinueWith(t =>
                {
                    SynchronizationContext.SetSynchronizationContext(prevCtx);

                    _asyncWorkIDDict.TryRemove(option.AsyncWorkID, out _);
                });
            }, option);
        }

        public string QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, Action<ExecuteResult<TResult>> callBack = null)
        {
            return QueueWorkItemAsync(asyncFunc, GetOption(callBack));
        }

        public string QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, WorkOption<TResult> option)
        {
            CheckPowerPoolOption();

            option.AsyncWorkID = CreateID(option);
            option.BaseAsyncWorkID = option.AsyncWorkID;
            option.AllowEventsAndCallback = false;
            _asyncWorkIDDict[option.AsyncWorkID] = new ConcurrentSet<string>();
            return QueueWorkItem(() =>
            {
                SynchronizationContext prevCtx = SynchronizationContext.Current;
                PowerPoolSynchronizationContext<TResult> ctx = new PowerPoolSynchronizationContext<TResult>(this, option);
                SynchronizationContext.SetSynchronizationContext(ctx);

                Task task = asyncFunc();
                ctx.SetTask(task);
                task.ContinueWith(t =>
                {
                    SynchronizationContext.SetSynchronizationContext(prevCtx);

                    _asyncWorkIDDict.TryRemove(option.AsyncWorkID, out _);
                });
                return default;
            }, option);
        }

        internal string CreateID<TResult>(WorkOption<TResult> option = null)
        {
            string workID;

            if (option != null && option.CustomWorkID != null)
            {
                if (_suspendedWork.ContainsKey(option.CustomWorkID) || _aliveWorkDic.ContainsKey(option.CustomWorkID))
                {
                    throw new InvalidOperationException($"The work ID '{option.CustomWorkID}' already exists.");
                }
                workID = option.CustomWorkID;
            }
            else
            {
                if (PowerPoolOption.WorkIDType == WorkIDType.LongIncrement)
                {
                    workID = Interlocked.Increment(ref _workIDIncrement).ToString();
                }
                else
                {
                    workID = Guid.NewGuid().ToString();
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
        public static string operator +(PowerPool powerPool, Action action)
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
        /// Create and return a WorkOption<TResult> instance with callback.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callBack"></param>
        /// <returns>A WorkOption<TResult> instance</returns>
        private WorkOption<TResult> GetOption<TResult>(Action<ExecuteResult<TResult>> callBack)
        {
            WorkOption<TResult> option = null;
            if (callBack == null)
            {
                option = WorkOption<TResult>.DefaultInstance;
            }
            else
            {
                option = new WorkOption<TResult>
                {
                    Callback = callBack
                };
            }
            return option;
        }

        /// <summary>
        /// Create and return a WorkOption<TResult> instance with callback.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callBack"></param>
        /// <returns>A WorkOption<TResult> instance</returns>
        private WorkOption GetOption(Action<ExecuteResult<object>> callBack)
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
