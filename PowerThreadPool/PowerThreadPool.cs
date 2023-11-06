using PowerThreadPool.EventArguments;
using PowerThreadPool.Helper;
using PowerThreadPool.Option;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PowerThreadPool
{
    public class PowerPool
    {
        private ManualResetEvent waitAllSignal = new ManualResetEvent(false);
        private ManualResetEvent pauseSignal = new ManualResetEvent(true);
        private ConcurrentDictionary<string, bool> pauseStatusDic = new ConcurrentDictionary<string, bool>();
        private ConcurrentDictionary<string, ManualResetEvent> pauseSignalDic = new ConcurrentDictionary<string, ManualResetEvent>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private ConcurrentDictionary<string, CancellationTokenSource> cancellationTokenSourceDic = new ConcurrentDictionary<string, CancellationTokenSource>();

        internal ConcurrentDictionary<string, Worker> idleWorkerDic = new ConcurrentDictionary<string, Worker>();
        internal ConcurrentQueue<string> idleWorkerQueue = new ConcurrentQueue<string>();
        private ConcurrentDictionary<string, WorkBase> waitingDependentDic = new ConcurrentDictionary<string, WorkBase>();
        
        private ConcurrentDictionary<string, Worker> settedWorkDic = new ConcurrentDictionary<string, Worker>();
        internal ConcurrentDictionary<string, Worker> aliveWorkerDic = new ConcurrentDictionary<string, Worker>();
        private PowerPoolOption powerPoolOption;
        public PowerPoolOption PowerPoolOption 
        { 
            get => powerPoolOption;
            set
            { 
                powerPoolOption = value;
                InitWorkerQueue();
            }
        }

        public delegate void PoolStartEventHandler(object sender, EventArgs e);
        public event PoolStartEventHandler PoolStart;
        public delegate void PoolIdleEventHandler(object sender, EventArgs e);
        public event PoolIdleEventHandler PoolIdle;
        public delegate void WorkStartEventHandler(object sender, WorkStartEventArgs e);
        public event WorkStartEventHandler WorkStart;
        public delegate void WorkEndEventHandler(object sender, WorkEndEventArgs e);
        public event WorkEndEventHandler WorkEnd;
        public delegate void PoolTimeoutEventHandler(object sender, EventArgs e);
        public event PoolTimeoutEventHandler PoolTimeout;
        public delegate void WorkTimeoutEventHandler(object sender, TimeoutEventArgs e);
        public event WorkTimeoutEventHandler WorkTimeout;
        public delegate void ForceStopEventHandler(object sender, ForceStopEventArgs e);
        public event ForceStopEventHandler ForceStop;

        internal delegate void CallbackEndEventHandler(string id, bool succeed);
        internal event CallbackEndEventHandler CallbackEnd;

        private System.Timers.Timer poolTimer;

        private bool poolRunning = false;
        public bool PoolRunning { get => poolRunning; }

        private bool poolStopping = false;
        public bool PoolStopping { get => poolStopping; }

        public int IdleWorkerCount
        {
            get
            {
                return idleWorkerDic.Count;
            }
        }
        public int WaitingWorkCount
        {
            get
            {
                int count = 0;
                List<Worker> workerList = aliveWorkerDic.Values.ToList();
                foreach (Worker worker in workerList)
                {
                    if (worker.workerState == 1)
                    {
                        count += worker.WaitingWorkCount;
                    }
                }
                return count;
            }
        }
        public IEnumerable<string> WaitingWorkList
        {
            get
            {
                List<string> list = settedWorkDic.Keys.ToList();
                List<Worker> workerList = aliveWorkerDic.Values.ToList();
                foreach (Worker worker in workerList) 
                {
                    if (worker.workerState == 1)
                    {
                        list.Remove(worker.WorkID);
                    }
                }
                return list;
            }
        }

        internal int runningWorkerCount;
        public int RunningWorkerCount
        {
            get 
            {
                return runningWorkerCount;
            }
        }

        public int AliveWorkerCount
        {
            get
            {
                return aliveWorkerDic.Count;
            }
        }

        public PowerPool()
        {
            
        }

        public PowerPool(PowerPoolOption powerPoolOption)
        {
            PowerPoolOption = powerPoolOption;
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action action, Action<ExecuteResult<object>> callBack = null)
        {
            WorkOption option = new WorkOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), Array.Empty<object>(), option);
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action action, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), Array.Empty<object>(), option);
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<TResult> function, Action<ExecuteResult<TResult>> callBack = null)
        {
            WorkOption<TResult> option = new WorkOption<TResult>();
            option.Callback = callBack;
            return QueueWorkItem<TResult>(DelegateHelper<TResult>.ToNormalFunc(function), Array.Empty<object>(), option);
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<TResult> function, WorkOption<TResult> option)
        {
            return QueueWorkItem<TResult>(DelegateHelper<TResult>.ToNormalFunc(function), Array.Empty<object>(), option);
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
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
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, WorkOption<TResult> option)
        {
            string workID = null;

            if (PoolStopping)
            {
                return null;
            }

            if (powerPoolOption == null)
            {
                PowerPoolOption = new PowerPoolOption();
            }

            ExecuteResult<TResult> excuteResult = new ExecuteResult<TResult>();

            if (option.CustomWorkID != null)
            {
                workID = option.CustomWorkID;
            }
            else
            {
                workID = Guid.NewGuid().ToString();
            }
            excuteResult.ID = workID;

            
            if (option.Timeout == null && powerPoolOption.DefaultWorkTimeout != null)
            {
                option.Timeout = powerPoolOption.DefaultWorkTimeout;
            }

            Work<TResult> work = new Work<TResult>(this, workID, function, param, option);
            pauseSignalDic[workID] = new ManualResetEvent(true);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSourceDic[workID] = cancellationTokenSource;

            if (option.Dependents != null && option.Dependents.Count > 0)
            {
                waitingDependentDic[workID] = work;
            }
            else
            {
                CheckPoolStart();
                SetWork(work);
            }

            return workID;
        }


        /// <summary>
        /// One thread end
        /// </summary>
        /// <param name="executeResult"></param>
        internal void OneWorkEnd(ExecuteResultBase executeResult)
        {
            InvokeWorkEndEvent(executeResult);
        }

        /// <summary>
        /// One thread end error
        /// </summary>
        /// <param name="executeResult"></param>
        internal void OneThreadEndByForceStop(string id)
        {
            if (ForceStop != null)
            {
                ForceStop.Invoke(this, new ForceStopEventArgs()
                {
                    ID = id
                });
            }
        }

        /// <summary>
        /// Invoke thread end event
        /// </summary>
        /// <param name="executeResult"></param>
        private void InvokeWorkEndEvent(ExecuteResultBase executeResult)
        {
            if (WorkEnd != null)
            {
                WorkEnd.Invoke(this, new WorkEndEventArgs()
                {
                    ID = executeResult.ID,
                    Exception = executeResult.Exception,
                    Result = executeResult.GetResult(),
                    Status = executeResult.Status
                });
            }
        }

        /// <summary>
        /// Work end
        /// </summary>
        /// <param name="guid"></param>
        internal void WorkCallbackEnd(string guid, bool succeed)
        {
            if (CallbackEnd != null)
            {
                CallbackEnd.Invoke(guid, succeed);
            }

            settedWorkDic.TryRemove(guid, out _);

            pauseStatusDic.TryRemove(guid, out _);
            pauseSignalDic.TryRemove(guid, out _);
            cancellationTokenSourceDic.TryRemove(guid, out _);
        }

        /// <summary>
        /// Init worker queue
        /// </summary>
        private void InitWorkerQueue()
        {
            if (powerPoolOption.DestroyThreadOption != null)
            {
                if (powerPoolOption.DestroyThreadOption.MinThreads > powerPoolOption.MaxThreads)
                {
                    throw new ArgumentException("The minimum number of threads cannot be greater than the maximum number of threads.");
                }
            }

            int minThreads = powerPoolOption.MaxThreads;
            if (powerPoolOption.DestroyThreadOption != null)
            {
                minThreads = powerPoolOption.DestroyThreadOption.MinThreads;
            }
            while (AliveWorkerCount < minThreads)
            {
                Worker worker = new Worker(this);
                aliveWorkerDic[worker.ID] = worker;
                idleWorkerQueue.Enqueue(worker.ID);
                idleWorkerDic[worker.ID] = worker;
            }
        }

        /// <summary>
        /// Set a work into a worker's work queue.
        /// </summary>
        internal void SetWork(WorkBase work)
        {
            CheckPoolStart();

            Worker worker = null;
            while (worker == null)
            {
                worker = GetWorker();
            }
            settedWorkDic[work.ID] = worker;
            worker.SetWork(work, this, false);
        }

        /// <summary>
        /// Get a Worker
        /// </summary>
        /// <returns></returns>
        private Worker GetWorker()
        {
            Worker worker = null;
            while (idleWorkerQueue.TryDequeue(out string firstWorkerID))
            {
                if (idleWorkerDic.TryRemove(firstWorkerID, out worker))
                {
                    if (Interlocked.Increment(ref worker.gettedLock) == -99)
                    {
                        Interlocked.Exchange(ref worker.gettedLock, -100);
                        continue;
                    }
                    return worker;
                }
            }

            if (aliveWorkerDic.Count < powerPoolOption.MaxThreads)
            {
                worker = new Worker(this);
                Interlocked.Increment(ref worker.gettedLock);
                aliveWorkerDic[worker.ID] = worker;
            }
            else
            {
                List<Worker> aliveWorkerList = aliveWorkerDic.Values.ToList();
                int min = int.MaxValue;
                foreach (Worker aliveWorker in aliveWorkerList)
                {
                    int waittingWorkCountTemp = aliveWorker.WaitingWorkCount;
                    if (waittingWorkCountTemp < min)
                    {
                        if (Interlocked.Increment(ref aliveWorker.gettedLock) == -99)
                        {
                            Interlocked.Exchange(ref aliveWorker.gettedLock, -100);
                            continue;
                        }
                        if (worker != null)
                        {
                            Interlocked.Decrement(ref worker.gettedLock);
                        }
                        min = waittingWorkCountTemp;
                        worker = aliveWorker;
                    }
                }
            }

            return worker;
        }

        /// <summary>
        /// Check if it's the start of thread pool
        /// </summary>
        private void CheckPoolStart()
        {
            if (!PoolRunning)
            {
                poolRunning = true;

                if (PoolStart != null)
                {
                    PoolStart.Invoke(this, new EventArgs());
                }

                waitAllSignal.Reset();

                if (powerPoolOption.Timeout != null)
                {
                    poolTimer = new System.Timers.Timer(powerPoolOption.Timeout.Duration);
                    poolTimer.AutoReset = false;
                    poolTimer.Elapsed += (s, e) =>
                    {
                        if (PoolTimeout != null)
                        {
                            PoolTimeout.Invoke(this, new EventArgs());
                        }
                        this.Stop(powerPoolOption.Timeout.ForceStop);
                    };
                    poolTimer.Start();
                }
            }
        }

        /// <summary>
        /// Check if thread pool is idle
        /// </summary>
        internal void CheckPoolIdle()
        {
            if (RunningWorkerCount == 0 && poolRunning)
            {
                poolRunning = false;
                if (poolStopping)
                {
                    poolStopping = false;
                }

                if (PoolIdle != null)
                {
                    PoolIdle.Invoke(this, new EventArgs());
                }

                waitAllSignal.Set();

                if (poolTimer != null)
                {
                    poolTimer.Stop();
                    poolTimer.Enabled = false;
                }

                pauseSignal = new ManualResetEvent(true);
                pauseStatusDic = new ConcurrentDictionary<string, bool>();
                pauseSignalDic = new ConcurrentDictionary<string, ManualResetEvent>();
                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSourceDic = new ConcurrentDictionary<string, CancellationTokenSource>();

                waitingDependentDic = new ConcurrentDictionary<string, WorkBase>();
                settedWorkDic = new ConcurrentDictionary<string, Worker>();
                aliveWorkerDic = new ConcurrentDictionary<string, Worker>();
            }
        }

        /// <summary>
        /// Blocks the calling thread until all of the works terminates.
        /// </summary>
        public void Wait()
        {
            if (!PoolRunning)
            {
                return;
            }
            waitAllSignal.WaitOne();
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Return false if the work isn't running</returns>
        public bool Wait(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            if (settedWorkDic.TryGetValue(id, out Worker worker))
            {
                return worker.Wait(id);
            }
            return false;
        }

        /// <summary>
        /// Blocks the calling thread until all of the works terminates.
        /// </summary>
        /// <returns>A Task</returns>
        public async Task WaitAsync()
        {
            await Task.Run(() =>
            {
                Wait();
            });
        }

        /// <summary>
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>Return false if the work isn't running</returns>
        public async Task<bool> WaitAsync(string id)
        {
            return await Task.Run(() =>
            {
                return Wait(id);
            });
        }

        /// <summary>
        /// Stop all works
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        public bool Stop(bool forceStop = false)
        {
            if (!poolRunning)
            {
                return false;
            }

            poolStopping = true;

            waitingDependentDic = new ConcurrentDictionary<string, WorkBase>();

            if (forceStop)
            {
                while (poolRunning)
                {
                    settedWorkDic.Clear();
                    List<Worker> workersToStop = aliveWorkerDic.Values.ToList();
                    foreach (Worker worker in workersToStop)
                    {
                        worker.ForceStop();
                    }
                }
            }
            else
            {
                cancellationTokenSource.Cancel();
            }

            return true;
        }

        /// <summary>
        /// Stop all works
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        public async Task<bool> StopAsync(bool forceStop = false)
        {
            return await Task.Run(() =>
            {
                return Stop(forceStop);
            });
        }

        /// <summary>
        /// Stop work by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return false if the work does not exist or has been done</returns>
        public bool Stop(string id, bool forceStop = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            bool res = settedWorkDic.TryRemove(id, out Worker workerToStop);
            if (res)
            {
                if (forceStop)
                {
                    workerToStop.ForceStop(id);
                }
                else
                {
                    if (cancellationTokenSourceDic.TryGetValue(id, out CancellationTokenSource cancellationTokenSource))
                    {
                        cancellationTokenSource.Cancel();
                    }
                    else
                    {
                        res = false;
                    }
                }
            }
            
            return res;
        }

        /// <summary>
        /// Stop work by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return false if the thread isn't running</returns>
        public async Task<bool> StopAsync(string id, bool forceStop = false)
        {
            return await Task.Run(() =>
            {
                return Stop(id, forceStop);
            });
        }

        /// <summary>
        /// Call this function inside the thread logic where you want to pause when user call Pause(...)
        /// </summary>
        public void PauseIfRequested()
        {
            pauseSignal.WaitOne();
            ICollection<string> workIDs = pauseSignalDic.Keys;
            foreach (string id in workIDs)
            {
                if (Thread.CurrentThread.Name == id)
                {
                    if (settedWorkDic.TryGetValue(id, out Worker worker))
                    {
                        if (pauseStatusDic.TryGetValue(id, out bool status))
                        {
                            if (status)
                            {
                                if (pauseSignalDic.TryGetValue(id, out ManualResetEvent manualResetEvent))
                                {
                                    worker.PauseTimer();
                                    manualResetEvent.WaitOne();
                                    worker.ResumeTimer();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Call this function inside the thread logic where you want to stop when user call ForceStop(...)
        /// </summary>
        public void StopIfRequested()
        {
            if (CheckIfRequestedStop())
            {
                throw new OperationCanceledException();
            }
        }

        /// <summary>
        /// Call this function inside the thread logic where you want to check if requested stop (if user call ForceStop(...))
        /// </summary>
        /// <returns></returns>
        public bool CheckIfRequestedStop()
        {
            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                return true;
            }
            foreach (string id in cancellationTokenSourceDic.Keys)
            {
                if (Thread.CurrentThread.Name == id)
                {
                    if (cancellationTokenSourceDic[id].Token.IsCancellationRequested)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Pause all threads
        /// </summary>
        public void Pause()
        {
            if (poolTimer != null)
            {
                poolTimer.Stop();
            }
            pauseSignal.Reset();
        }

        /// <summary>
        /// Resume all threads
        /// </summary>
        /// <param name="resumeWorkPausedByID">if resume work paused by ID</param>
        public void Resume(bool resumeWorkPausedByID = false)
        {
            if (poolTimer != null)
            {
                poolTimer.Start();
            }
            pauseSignal.Set();
            if (resumeWorkPausedByID)
            {
                foreach (ManualResetEvent manualResetEvent in pauseSignalDic.Values)
                {
                    manualResetEvent.Set();
                }
            }
        }

        /// <summary>
        /// Pause thread by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>If the work id exists</returns>
        public bool Pause(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            if (pauseSignalDic.TryGetValue(id, out ManualResetEvent manualResetEvent))
            {
                manualResetEvent.Reset();
                pauseStatusDic[id] = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Resume thread by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>If the work id exists</returns>
        public bool Resume(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }
            if (pauseSignalDic.TryGetValue(id, out ManualResetEvent manualResetEvent))
            {
                pauseStatusDic[id] = false;
                manualResetEvent.Set();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Cancel all tasks that have not started running
        /// </summary>
        public void Cancel()
        {
            List<Worker> aliveWorkerList = aliveWorkerDic.Values.ToList();
            foreach (Worker worker in aliveWorkerList)
            {
                worker.Cancel();
            }
        }

        /// <summary>
        /// Cancel the task by id if the task has not started running
        /// </summary>
        /// <param name="id">work id</param>
        /// <returns>is succeed</returns>
        public bool Cancel(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (settedWorkDic.TryGetValue(id, out Worker worker))
            {
                return worker.Cancel(id);
            }

            return false;
        }

        /// <summary>
        /// Invoke WorkTimeout event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnWorkTimeout(object sender, TimeoutEventArgs e)
        {
            if (WorkTimeout != null)
            {
                WorkTimeout.Invoke(this, e);
            }
        }

        /// <summary>
        /// Invoke WorkStart event
        /// </summary>
        /// <param name="workID"></param>
        internal void OnWorkStart(string workID)
        {
            if (WorkStart != null)
            {
                WorkStart.Invoke(this, new WorkStartEventArgs() { ID = workID });
            }
        }
    }
}