using PowerThreadPool.Collections;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Helper;
using PowerThreadPool.Option;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PowerThreadPool
{
    public class PowerPool : IDisposable
    {
        private bool disposed = false;

        private ManualResetEvent waitAllSignal = new ManualResetEvent(false);
        private ManualResetEvent pauseSignal = new ManualResetEvent(true);
        private ConcurrentDictionary<string, bool> pauseStatusDic = new ConcurrentDictionary<string, bool>();
        private ConcurrentDictionary<string, ManualResetEvent> pauseSignalDic = new ConcurrentDictionary<string, ManualResetEvent>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private ConcurrentDictionary<string, CancellationTokenSource> cancellationTokenSourceDic = new ConcurrentDictionary<string, CancellationTokenSource>();

        internal ConcurrentSet<string> failedWorkSet = new ConcurrentSet<string>();

        internal ConcurrentDictionary<string, Worker> idleWorkerDic = new ConcurrentDictionary<string, Worker>();
        internal ConcurrentQueue<string> idleWorkerQueue = new ConcurrentQueue<string>();

        private ConcurrentDictionary<string, Worker> settedWorkDic = new ConcurrentDictionary<string, Worker>();
        internal ConcurrentDictionary<string, Worker> aliveWorkerDic = new ConcurrentDictionary<string, Worker>();
        internal IEnumerable<Worker> aliveWorkerList = new List<Worker>();

        private Dictionary<WorkBase, ConcurrentSet<string>> suspendedWork = new Dictionary<WorkBase, ConcurrentSet<string>>();
        private bool suspended;

        private PowerPoolOption powerPoolOption;
        public PowerPoolOption PowerPoolOption 
        { 
            get => powerPoolOption;
            set
            { 
                powerPoolOption = value;
                suspended = value.StartSuspended;
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

        internal delegate void CallbackEndEventHandler(string id);
        internal event CallbackEndEventHandler CallbackEnd;

        private System.Timers.Timer poolTimer;

        private bool poolRunning = false;
        public bool PoolRunning { get => poolRunning; }

        private bool poolStopping = false;
        public bool PoolStopping { get => poolStopping; }

        private bool enablePoolIdleCheck = true;
        /// <summary>
        /// Indicates whether to perform pool idle check.
        /// </summary>
        public bool EnablePoolIdleCheck
        {
            get 
            { 
                return enablePoolIdleCheck; 
            } 
            set 
            {
                enablePoolIdleCheck = value;
                if (enablePoolIdleCheck)
                {
                    CheckPoolIdle();
                }
            }
        }

        public int IdleWorkerCount
        {
            get
            {
                return idleWorkerDic.Count;
            }
        }

        internal int waitingWorkCount = 0;
        public int WaitingWorkCount
        {
            get
            {
                return waitingWorkCount;
            }
        }

        public IEnumerable<string> WaitingWorkList
        {
            get
            {
                List<string> list = settedWorkDic.Keys.ToList();
                foreach (Worker worker in aliveWorkerList) 
                {
                    if (worker.workerState == 1)
                    {
                        list.Remove(worker.WorkID);
                    }
                }
                return list;
            }
        }

        public int FailedWorkCount
        {
            get
            {
                return failedWorkSet.Count;
            }
        }

        public IEnumerable<string> FailedWorkList
        {
            get
            {
                return failedWorkSet;
            }
        }

        internal int runningWorkerCount = 0;
        public int RunningWorkerCount
        {
            get 
            {
                return runningWorkerCount;
            }
        }

        internal int aliveWorkerCount = 0;
        public int AliveWorkerCount
        {
            get
            {
                return aliveWorkerCount;
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
        public string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, WorkOption<TResult> workOption)
        {
            if (disposed)
            { 
                throw new ObjectDisposedException(GetType().FullName);
            }

            string workID;

            if (PoolStopping)
            {
                return null;
            }

            if (powerPoolOption == null)
            {
                PowerPoolOption = new PowerPoolOption();
            }

            if (workOption.CustomWorkID != null)
            {
                workID = workOption.CustomWorkID;
            }
            else
            {
                workID = Guid.NewGuid().ToString();
            }

            if (workOption.Timeout == null && powerPoolOption.DefaultWorkTimeout != null)
            {
                workOption.Timeout = powerPoolOption.DefaultWorkTimeout;
            }

            Work<TResult> work = new Work<TResult>(this, workID, function, param, workOption);
            pauseSignalDic[workID] = new ManualResetEvent(true);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSourceDic[workID] = cancellationTokenSource;

            Interlocked.Increment(ref waitingWorkCount);

            if (powerPoolOption.StartSuspended)
            {
                suspendedWork[work] = workOption.Dependents;
            }
            else
            {
                if (workOption.Dependents == null || workOption.Dependents.Count == 0)
                {
                    CheckPoolStart();
                    SetWork(work);
                }
            }

            return workID;
        }

        /// <summary>
        /// Start the pool, but only if PowerPoolOption.StartSuspended is set to true.
        /// </summary>
        public void Start()
        {
            if (!suspended)
            {
                return;
            }

            suspended = false;
            foreach (WorkBase work in suspendedWork.Keys)
            {
                ConcurrentSet<string> dependents = suspendedWork[work];
                if (dependents == null || dependents.Count == 0)
                {
                    CheckPoolStart();
                    SetWork(work);
                }
            }
            suspendedWork.Clear();
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
        internal void WorkCallbackEnd(string guid, Status status)
        {
            if (status == Status.Failed)
            {
                failedWorkSet.Add(guid);
            }

            if (CallbackEnd != null)
            {
                CallbackEnd.Invoke(guid);
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
            while (aliveWorkerCount < minThreads)
            {
                Worker worker = new Worker(this);
                if (aliveWorkerDic.TryAdd(worker.ID, worker))
                {
                    Interlocked.Increment(ref aliveWorkerCount);
                    aliveWorkerList = aliveWorkerDic.Values;
                }
                idleWorkerDic[worker.ID] = worker;
                idleWorkerQueue.Enqueue(worker.ID);
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
            worker.SetWork(work, false);
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

            lock (this)
            {
                if (aliveWorkerCount < powerPoolOption.MaxThreads)
                {
                    worker = new Worker(this);
                    Interlocked.Increment(ref worker.gettedLock);
                    if (aliveWorkerDic.TryAdd(worker.ID, worker))
                    {
                        Interlocked.Increment(ref aliveWorkerCount);
                        aliveWorkerList = aliveWorkerDic.Values;
                    }
                }

                if (worker == null)
                {
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
            if (!enablePoolIdleCheck)
            {
                return;
            }

            if (runningWorkerCount == 0 && waitingWorkCount == 0 && poolRunning)
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

                if (poolTimer != null)
                {
                    poolTimer.Stop();
                    poolTimer.Enabled = false;
                }

                suspended = powerPoolOption.StartSuspended;

                waitAllSignal.Set();
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

            if (forceStop)
            {
                while (poolRunning)
                {
                    settedWorkDic.Clear();
                    IEnumerable<Worker> workersToStop = aliveWorkerList;
                    foreach (Worker worker in workersToStop)
                    {
                        worker.ForceStop();
                    }
                }
            }
            else
            {
                cancellationTokenSource.Cancel();
                IEnumerable<Worker> workersToStop = aliveWorkerList;
                foreach (Worker worker in workersToStop)
                {
                    worker.Cancel();
                }
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

            bool res = false;
            if (settedWorkDic.TryGetValue(id, out Worker workerToStop))
            {
                if (forceStop)
                {
                    res = true;
                    workerToStop.ForceStop(id);
                }
                else
                {
                    if (!workerToStop.Cancel(id))
                    {
                        if (cancellationTokenSourceDic.TryGetValue(id, out CancellationTokenSource cancellationTokenSource))
                        {
                            res = true;
                            cancellationTokenSource.Cancel();
                        }
                    }
                    else
                    {
                        res = true;
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
        /// Stop works by id list
        /// </summary>
        /// <param name="id">work id list</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return a list of ID for work that either doesn't exist or hasn't been done</returns>
        public IEnumerable<string> Stop(IEnumerable<string> idList, bool forceStop = false)
        {
            List<string> failedIDList = new List<string>();

            foreach (string id in idList)
            {
                if (!Stop(id, forceStop))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
        }

        /// <summary>
        /// Stop works by id list
        /// </summary>
        /// <param name="id">work id list</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return a list of ID for work that either doesn't exist or hasn't been done</returns>
        public async Task<IEnumerable<string>> StopAsync(IEnumerable<string> idList, bool forceStop = false)
        {
            return await Task.Run(() =>
            {
                List<string> failedIDList = new List<string>();

                foreach (string id in idList)
                {
                    if (!Stop(id, forceStop))
                    {
                        failedIDList.Add(id);
                    }
                }

                return failedIDList;
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
                if (settedWorkDic.TryGetValue(id, out Worker worker))
                {
                    if (worker.thread == Thread.CurrentThread && worker.WorkID == id)
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
            string workID = CheckIfRequestedStopAndReturnWorkID();
            if (workID != null)
            {
                if (workID == "")
                {
                    settedWorkDic.Clear();
                }
                else
                {
                    settedWorkDic.TryRemove(workID, out _);
                }
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
                if (settedWorkDic.TryGetValue(id, out Worker worker))
                {
                    if (worker.thread == Thread.CurrentThread && worker.WorkID == id)
                    {
                        if (cancellationTokenSourceDic[id].Token.IsCancellationRequested)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Call this function inside the thread logic where you want to check if requested stop (if user call ForceStop(...))
        /// </summary>
        /// <returns></returns>
        private string CheckIfRequestedStopAndReturnWorkID()
        {
            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                return "";
            }
            foreach (string id in cancellationTokenSourceDic.Keys)
            {
                if (settedWorkDic.TryGetValue(id, out Worker worker))
                {
                    if (worker.thread == Thread.CurrentThread && worker.WorkID == id)
                    {
                        if (cancellationTokenSourceDic[id].Token.IsCancellationRequested)
                        {
                            return id;
                        }
                    }
                }
            }
            return null;
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

            bool res = false;
            if (pauseSignalDic.TryGetValue(id, out ManualResetEvent manualResetEvent))
            {
                pauseStatusDic[id] = false;
                manualResetEvent.Set();
                res =  true;
            }
            return res;
        }

        /// <summary>
        /// Pause threads by id list
        /// </summary>
        /// <param name="id">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public IEnumerable<string> Pause(IEnumerable<string> idList)
        {
            List<string> failedIDList = new List<string>();

            foreach (string id in idList)
            {
                if (!Pause(id))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
        }

        /// <summary>
        /// Resume threads by id list
        /// </summary>
        /// <param name="id">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public IEnumerable<string> Resume(IEnumerable<string> idList)
        {
            List<string> failedIDList = new List<string>();

            foreach (string id in idList)
            {
                if (!Resume(id))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
        }

        /// <summary>
        /// Cancel all tasks that have not started running
        /// </summary>
        public void Cancel()
        {
            foreach (Worker worker in aliveWorkerList)
            {
                worker.Cancel();
            }
        }

        /// <summary>
        /// Cancel the work by id if the work has not started running
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
        /// Cancel the works by id if the work has not started running
        /// </summary>
        /// <param name="id">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public IEnumerable<string> Cancel(IEnumerable<string> idList)
        {
            List<string> failedIDList = new List<string>();

            foreach (string id in idList)
            {
                if (!Cancel(id))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
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

        /// <summary>
        /// Will try stop, force stop and kill all of the workers. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the instance
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Stop();
                    Stop(true);
                    foreach (Worker worker in aliveWorkerList)
                    {
                        worker.Kill();
                    }
                    aliveWorkerDic = new ConcurrentDictionary<string, Worker>();
                    idleWorkerDic = new ConcurrentDictionary<string, Worker>();
                    idleWorkerQueue = new ConcurrentQueue<string>();
                    aliveWorkerCount = 0;
                    runningWorkerCount = 0;
                    waitingWorkCount = 0;
                }

                disposed = true;
            }
        }

        ~PowerPool()
        {
            Dispose(false);
        }
    }
}