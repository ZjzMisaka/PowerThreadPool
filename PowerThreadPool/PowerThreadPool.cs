using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
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
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        internal ConcurrentSet<string> failedWorkSet = new ConcurrentSet<string>();

        internal ConcurrentDictionary<string, Worker> idleWorkerDic = new ConcurrentDictionary<string, Worker>();
        internal ConcurrentQueue<string> idleWorkerQueue = new ConcurrentQueue<string>();

        internal ConcurrentDictionary<string, Worker> settedWorkDic = new ConcurrentDictionary<string, Worker>();
        internal ConcurrentDictionary<string, Worker> aliveWorkerDic = new ConcurrentDictionary<string, Worker>();
        internal IEnumerable<Worker> aliveWorkerList = new List<Worker>();

        internal Dictionary<string, KeyValuePair<WorkBase, ConcurrentSet<string>>> suspendedWork = new Dictionary<string, KeyValuePair<WorkBase, ConcurrentSet<string>>>();
        private bool suspended;

        private int createWorkerLock = WorkerCreationFlags.Unlocked;

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

        private int poolRunning = 0;
        public bool PoolRunning { get => poolRunning == PoolRunningFlags.Running; }

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

        internal int idleWorkerCount = 0;
        public int IdleWorkerCount
        {
            get
            {
                return idleWorkerCount;
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
                    if (worker.workerState == WorkerStates.Running)
                    {
                        list.Remove(worker.WorkID);
                    }
                }
                return list;
            }
        }

        /// <summary>
        /// Failed work count
        /// Will be reset to zero when the thread pool starts again
        /// </summary>
        public int FailedWorkCount
        {
            get
            {
                return failedWorkSet.Count;
            }
        }

        /// <summary>
        /// ID list of failed works
        /// Will be cleared when the thread pool starts again
        /// </summary>
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

        internal int longRunningWorkerCount = 0;
        public int LongRunningWorkerCount
        {
            get
            {
                return longRunningWorkerCount;
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

            Interlocked.Increment(ref waitingWorkCount);

            if (powerPoolOption.StartSuspended)
            {
                suspendedWork[workID] = new KeyValuePair<WorkBase, ConcurrentSet<string>>(work, workOption.Dependents);
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
            foreach (KeyValuePair<string, KeyValuePair<WorkBase, ConcurrentSet<string>>> kv in suspendedWork)
            {
                WorkBase work = kv.Value.Key;
                ConcurrentSet<string> dependents = kv.Value.Value;
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
                Interlocked.Increment(ref idleWorkerCount);
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
                worker = GetWorker(work.LongRunning);
            }
            worker.SetWork(work, false);
        }

        /// <summary>
        /// Get a Worker
        /// </summary>
        /// <returns></returns>
        private Worker GetWorker(bool longRunning)
        {
            Worker worker = null;
            while (idleWorkerQueue.TryDequeue(out string firstWorkerID))
            {
                if (idleWorkerDic.TryRemove(firstWorkerID, out worker))
                {
                    Interlocked.Decrement(ref idleWorkerCount);
                    Interlocked.Exchange(ref worker.gettedLock, WorkerGettedFlags.Locked);
                    if (longRunning)
                    {
                        Interlocked.Increment(ref longRunningWorkerCount);
                    }
                    return worker;
                }
            }

            if (aliveWorkerCount < powerPoolOption.MaxThreads + longRunningWorkerCount)
            {
                if (Interlocked.CompareExchange(ref createWorkerLock, WorkerCreationFlags.Locked, WorkerCreationFlags.Unlocked) == WorkerCreationFlags.Unlocked)
                {
                    if (aliveWorkerCount < powerPoolOption.MaxThreads + longRunningWorkerCount)
                    {
                        worker = new Worker(this);
                        Interlocked.Exchange(ref worker.gettedLock, WorkerGettedFlags.Locked);
                        if (aliveWorkerDic.TryAdd(worker.ID, worker))
                        {
                            Interlocked.Increment(ref aliveWorkerCount);
                            aliveWorkerList = aliveWorkerDic.Values;
                        }
                        if (longRunning)
                        {
                            Interlocked.Increment(ref longRunningWorkerCount);
                        }
                    }

                    Interlocked.Exchange(ref createWorkerLock, WorkerCreationFlags.Unlocked);
                }
            }

            if (worker == null && !longRunning)
            {
                int min = int.MaxValue;
                foreach (Worker aliveWorker in aliveWorkerList)
                {
                    if (aliveWorker.LongRunning)
                    {
                        continue;
                    }

                    int waitingWorkCountTemp = aliveWorker.WaitingWorkCount;
                    if (waitingWorkCountTemp < min)
                    {
                        if (Interlocked.CompareExchange(ref aliveWorker.gettedLock, WorkerGettedFlags.Locked, WorkerGettedFlags.Unlocked) == WorkerGettedFlags.Unlocked)
                        {
                            if (worker != null)
                            {
                                Interlocked.CompareExchange(ref worker.gettedLock, WorkerGettedFlags.Unlocked, WorkerGettedFlags.Locked);
                            }
                            min = waitingWorkCountTemp;
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
            if (Interlocked.CompareExchange(ref poolRunning, PoolRunningFlags.Running, PoolRunningFlags.NotRunning) == PoolRunningFlags.NotRunning)
            {
                if (PoolStart != null)
                {
                    PoolStart.Invoke(this, new EventArgs());
                }

                failedWorkSet = new ConcurrentSet<string>();
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

            InitWorkerQueue();

            if (runningWorkerCount == 0 && waitingWorkCount == 0 && Interlocked.CompareExchange(ref poolRunning, PoolRunningFlags.IdleChecked, PoolRunningFlags.Running) == PoolRunningFlags.Running)
            {
                if (PoolIdle != null)
                {
                    try
                    {
                        PoolIdle.Invoke(this, new EventArgs());
                    }
                    finally
                    {
                        IdleSetting();
                    }
                }
                else
                {
                    IdleSetting();
                }
            }
        }

        private void IdleSetting()
        {
            if (poolTimer != null)
            {
                poolTimer.Stop();
                poolTimer.Enabled = false;
            }

            suspended = powerPoolOption.StartSuspended;

            cancellationTokenSource = new CancellationTokenSource();
            waitAllSignal.Set();

            Interlocked.Exchange(ref poolRunning, PoolRunningFlags.NotRunning);
            if (poolStopping)
            {
                poolStopping = false;
            }
        }

        internal void SetWorkOwner(string workId, Worker worker)
        {
            settedWorkDic[workId] = worker;
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
        /// Call this function inside the thread logic where you want to pause when user call Pause(...)
        /// </summary>
        public void PauseIfRequested()
        {
            pauseSignal.WaitOne();


            foreach (Worker worker in aliveWorkerList)
            {
                if (worker.workerState == WorkerStates.Running && worker.thread == Thread.CurrentThread && worker.IsPausing())
                {
                    worker.PauseTimer();
                    worker.WaitForResume();
                    worker.ResumeTimer();
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

            foreach (Worker worker in aliveWorkerList)
            {
                if (worker.workerState == WorkerStates.Running && worker.thread == Thread.CurrentThread && worker.IsCancellationRequested())
                {
                    return true;
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

            foreach (Worker worker in aliveWorkerList)
            {
                if (worker.workerState == WorkerStates.Running && worker.thread == Thread.CurrentThread && worker.IsCancellationRequested())
                {
                    return worker.WorkID;
                }
            }

            return null;
        }

        /// <summary>
        /// Blocks the calling thread until all of the works terminates.
        /// </summary>
        public void Wait()
        {
            if (poolRunning == PoolRunningFlags.NotRunning)
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
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of ID for work that doesn't running</returns>
        public List<string> Wait(IEnumerable<string> idList)
        {
            List<string> failedIDList = new List<string>();

            foreach (string id in idList)
            {
                if (!Wait(id))
                {
                    failedIDList.Add(id);
                }
            }

            return failedIDList;
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
        /// Blocks the calling thread until the work terminates.
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of ID for work that doesn't running</returns>
        public async Task<List<string>> WaitAsync(IEnumerable<string> idList)
        {
            return await Task.Run(() =>
            {
                List<string> failedIDList = new List<string>();

                foreach (string id in idList)
                {
                    if (!Wait(id))
                    {
                        failedIDList.Add(id);
                    }
                }

                return failedIDList;
            });
        }

        /// <summary>
        /// Stop all works
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        public bool Stop(bool forceStop = false)
        {
            if (poolRunning == PoolRunningFlags.NotRunning)
            {
                return false;
            }

            poolStopping = true;

            if (forceStop)
            {
                while (poolRunning == PoolRunningFlags.Running)
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

            Wait();

            return true;
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
                res = workerToStop.Stop(id, forceStop);
            }

            return res;
        }

        /// <summary>
        /// Stop works by id list
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return a list of ID for work that either doesn't exist or hasn't been done</returns>
        public List<string> Stop(IEnumerable<string> idList, bool forceStop = false)
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
        /// <param name="idList">work id list</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return a list of ID for work that either doesn't exist or hasn't been done</returns>
        public async Task<List<string>> StopAsync(IEnumerable<string> idList, bool forceStop = false)
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
            if (settedWorkDic.TryGetValue(id, out Worker workerToPause))
            {
                return workerToPause.Pause(id);
            }
            return false;
        }

        /// <summary>
        /// Pause threads by id list
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Pause(IEnumerable<string> idList)
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
                foreach (Worker worker in aliveWorkerList)
                {
                    if (worker.workerState == WorkerStates.Running)
                    {
                        worker.Resume();
                    }
                }
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
            if (settedWorkDic.TryGetValue(id, out Worker workerToPause))
            {
                return workerToPause.Resume(id);
            }
            return false;
        }

        /// <summary>
        /// Resume threads by id list
        /// </summary>
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Resume(IEnumerable<string> idList)
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
        /// <param name="idList">work id list</param>
        /// <returns>Return a list of IDs for work that doesn't exist</returns>
        public List<string> Cancel(IEnumerable<string> idList)
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
                    waitAllSignal.Set();
                    Stop();
                    Stop(true);
                    foreach (Worker worker in aliveWorkerList)
                    {
                        worker.Kill();
                    }
                    aliveWorkerDic = new ConcurrentDictionary<string, Worker>();
                    idleWorkerDic = new ConcurrentDictionary<string, Worker>();
                    idleWorkerQueue = new ConcurrentQueue<string>();
                    idleWorkerCount = 0;
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