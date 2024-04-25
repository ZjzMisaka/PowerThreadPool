using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Helpers;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using System;
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
        internal ConcurrentDictionary<string, ConcurrentSet<string>> workGroupDic = new ConcurrentDictionary<string, ConcurrentSet<string>>();
        internal ConcurrentDictionary<string, Worker> aliveWorkerDic = new ConcurrentDictionary<string, Worker>();
        internal IEnumerable<Worker> aliveWorkerList = new List<Worker>();

        internal ConcurrentQueue<string> suspendedWorkQueue = new ConcurrentQueue<string>();
        internal ConcurrentDictionary<string, WorkBase> suspendedWork = new ConcurrentDictionary<string, WorkBase>();
        private bool suspended;

        private int createWorkerLock = WorkerCreationFlags.Unlocked;

        private static readonly object[] EmptyArray = new object[0];

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

        public delegate void PoolStartedEventHandler(object sender, EventArgs e);
        public event EventHandler<EventArgs> PoolStarted;
        public delegate void PoolIdledEventHandler(object sender, EventArgs e);
        public event EventHandler<EventArgs> PoolIdled;
        public delegate void WorkStartedEventHandler(object sender, WorkStartedEventArgs e);
        public event EventHandler<WorkStartedEventArgs> WorkStarted;
        public delegate void WorkEndedEventHandler(object sender, WorkEndedEventArgs e);
        public event EventHandler<WorkEndedEventArgs> WorkEnded;
        public delegate void PoolTimedOutEventHandler(object sender, EventArgs e);
        public event EventHandler<EventArgs> PoolTimedOut;
        public delegate void WorkTimedOutEventHandler(object sender, WorkTimedOutEventArgs e);
        public event EventHandler<WorkTimedOutEventArgs> WorkTimedOut;
        public delegate void WorkStoppedEventHandler(object sender, WorkStoppedEventArgs e);
        public event EventHandler<WorkStoppedEventArgs> WorkStopped;
        public delegate void WorkCanceledEventHandler(object sender, WorkCanceledEventArgs e);
        public event EventHandler<WorkCanceledEventArgs> WorkCanceled;
        public delegate void ErrorOccurredEventHandler(object sender, ErrorOccurredEventArgs e);
        public event EventHandler<ErrorOccurredEventArgs> ErrorOccurred;

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
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), EmptyArray, option);
        }

        /// <summary>
        /// Queues a work for execution. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action action, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), EmptyArray, option);
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
            return QueueWorkItem<TResult>(DelegateHelper<TResult>.ToNormalFunc(function), EmptyArray, option);
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
            return QueueWorkItem<TResult>(DelegateHelper<TResult>.ToNormalFunc(function), EmptyArray, option);
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

            if (workOption.TimeoutOption == null && powerPoolOption.DefaultWorkTimeoutOption != null)
            {
                workOption.TimeoutOption = powerPoolOption.DefaultWorkTimeoutOption;
            }

            Work<TResult> work = new Work<TResult>(this, workID, function, param, workOption);

            Interlocked.Increment(ref waitingWorkCount);

            if (powerPoolOption.StartSuspended)
            {
                suspendedWork[workID] = work;
                suspendedWorkQueue.Enqueue(workID);
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
            while (suspendedWorkQueue.TryDequeue(out string key))
            {
                 if (suspendedWork.TryGetValue(key, out WorkBase work))
                {
                    ConcurrentSet<string> dependents = work.Dependents;
                    if (dependents == null || dependents.Count == 0)
                    {
                        CheckPoolStart();
                        SetWork(work);
                    }
                }
            }
            suspendedWork.Clear();
            suspendedWorkQueue = new ConcurrentQueue<string>();
        }

        /// <summary>
        /// Invoke work end event
        /// </summary>
        /// <param name="executeResult"></param>
        internal void InvokeWorkEndedEvent(ExecuteResultBase executeResult)
        {
            executeResult.EndDateTime = DateTime.Now;
            if (WorkEnded != null)
            {
                WorkEndedEventArgs e = new WorkEndedEventArgs()
                {
                    ID = executeResult.ID,
                    Exception = executeResult.Exception,
                    Result = executeResult.GetResult(),
                    Succeed = executeResult.Status == Status.Succeed,
                    QueueDateTime = executeResult.QueueDateTime,
                    StartDateTime = executeResult.StartDateTime,
                    EndDateTime = executeResult.EndDateTime,
                    RetryInfo = executeResult.RetryInfo,
                };

                if (executeResult.RetryInfo != null)
                {
                    executeResult.RetryInfo.StopRetry = e.RetryInfo.StopRetry;
                }

                SafeInvoke(WorkEnded, e, ErrorFrom.WorkEnded, executeResult);
            }
        }

        /// <summary>
        /// Invoke work stopped event
        /// </summary>
        /// <param name="executeResult"></param>
        internal void InvokeWorkStoppedEvent(ExecuteResultBase executeResult)
        {
            executeResult.EndDateTime = DateTime.Now;
            if (WorkStopped != null)
            {
                WorkStoppedEventArgs e = new WorkStoppedEventArgs()
                {
                    ID = executeResult.ID,
                    ForceStop = executeResult.Status == Status.ForceStopped,
                    QueueDateTime = executeResult.QueueDateTime,
                    StartDateTime = executeResult.StartDateTime,
                    EndDateTime = executeResult.EndDateTime,
                };
                SafeInvoke(WorkStopped, e, ErrorFrom.WorkStopped, executeResult);
            }
        }

        /// <summary>
        /// Invoke work canceled event
        /// </summary>
        /// <param name="executeResult"></param>
        internal void InvokeWorkCanceledEvent(ExecuteResultBase executeResult)
        {
            executeResult.EndDateTime = DateTime.Now;
            if (WorkCanceled != null)
            {
                WorkCanceledEventArgs e = new WorkCanceledEventArgs()
                {
                    ID = executeResult.ID,
                    QueueDateTime = executeResult.QueueDateTime,
                    StartDateTime = executeResult.StartDateTime,
                    EndDateTime = executeResult.EndDateTime,
                };
                SafeInvoke(WorkCanceled, e, ErrorFrom.WorkCanceled, executeResult);
            }
        }

        /// <summary>
        /// Work end
        /// </summary>
        /// <param name="guid"></param>
        internal void WorkCallbackEnd(WorkBase work, Status status)
        {
            if (status == Status.Failed)
            {
                failedWorkSet.Add(work.ID);
            }

            if (CallbackEnd != null)
            {
                CallbackEnd.Invoke(work.ID);
            }

            settedWorkDic.TryRemove(work.ID, out _);
            if (work.Group != null)
            {
                if (workGroupDic.TryGetValue(work.Group, out ConcurrentSet<string> idSet))
                {
                    idSet.Remove(work.ID);
                }
            }
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
            work.QueueDateTime = DateTime.Now;
            worker.SetWork(work, false);
        }

        /// <summary>
        /// Get a Worker
        /// </summary>
        /// <returns>worker</returns>
        private Worker GetWorker(bool longRunning)
        {
            Worker worker = null;
            while (idleWorkerQueue.TryDequeue(out string firstWorkerID))
            {
                if (idleWorkerDic.TryRemove(firstWorkerID, out worker))
                {
                    SpinWait.SpinUntil(() =>
                    {
                        int gettedStatus = Interlocked.CompareExchange(ref worker.gettedLock, WorkerGettedFlags.Locked, WorkerGettedFlags.Unlocked);
                        return (gettedStatus == WorkerGettedFlags.Unlocked);
                    });
                    Interlocked.Decrement(ref idleWorkerCount);
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
                if (PoolStarted != null)
                {
                    SafeInvoke(PoolStarted, new EventArgs(), ErrorFrom.PoolStarted, null);
                }

                failedWorkSet = new ConcurrentSet<string>();
                waitAllSignal.Reset();

                if (powerPoolOption.TimeoutOption != null)
                {
                    poolTimer = new System.Timers.Timer(powerPoolOption.TimeoutOption.Duration);
                    poolTimer.AutoReset = false;
                    poolTimer.Elapsed += (s, e) =>
                    {
                        if (PoolTimedOut != null)
                        {
                            SafeInvoke(PoolTimedOut, new EventArgs(), ErrorFrom.PoolTimedOut, null);
                        }
                        this.Stop(powerPoolOption.TimeoutOption.ForceStop);
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
                if (PoolIdled != null)
                {
                    try
                    {
                        SafeInvoke(PoolIdled, new EventArgs(), ErrorFrom.PoolIdled, null);
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

        /// <summary>
        /// Reset some flags
        /// </summary>
        private void IdleSetting()
        {
            if (poolTimer != null)
            {
                poolTimer.Stop();
                poolTimer.Enabled = false;
            }

            suspended = powerPoolOption.StartSuspended;

            cancellationTokenSource.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            Interlocked.Exchange(ref poolRunning, PoolRunningFlags.NotRunning);
            if (poolStopping)
            {
                poolStopping = false;
            }

            waitAllSignal.Set();
        }

        /// <summary>
        /// Add worker into settedWorkDic
        /// </summary>
        /// <param name="workId"></param>
        /// <param name="worker"></param>
        internal void SetWorkOwner(WorkBase work, Worker worker)
        {
            settedWorkDic[work.ID] = worker;
            if (work.Group != null)
            {
                workGroupDic.AddOrUpdate(work.Group, new ConcurrentSet<string>() { work.ID }, (key, oldValue) => { oldValue.Add(work.ID); return oldValue; });
            }
        }

        /// <summary>
        /// Invoke WorkTimedOut event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnWorkTimedOut(object sender, WorkTimedOutEventArgs e)
        {
            if (WorkTimedOut != null)
            {
                SafeInvoke(WorkTimedOut, e, ErrorFrom.WorkTimedOut, null);
            }
        }

        /// <summary>
        /// Invoke WorkStarted event
        /// </summary>
        /// <param name="workID"></param>
        internal void OnWorkStarted(string workID)
        {
            if (WorkStarted != null)
            {
                SafeInvoke(WorkStarted, new WorkStartedEventArgs() { ID = workID }, ErrorFrom.WorkStarted, null);
            }
        }

        /// <summary>
        /// Safe invoke
        /// </summary>
        /// <typeparam name="TEventArgs"></typeparam>
        /// <param name="eventHandler"></param>
        /// <param name="e"></param>
        /// <param name="errorFrom"></param>
        /// <param name="executeResult"></param>
        internal void SafeInvoke<TEventArgs>(EventHandler<TEventArgs> eventHandler, TEventArgs e, ErrorFrom errorFrom, ExecuteResultBase executeResult)
        {
            try
            {
                eventHandler.Invoke(this, e);
            }
            catch (ThreadInterruptedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ErrorOccurred != null)
                {
                    ErrorOccurredEventArgs ea = new ErrorOccurredEventArgs(ex, errorFrom, executeResult);

                    ErrorOccurred.Invoke(this, ea);
                }
            }
        }

        /// <summary>
        /// On work error occurred
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="errorFrom"></param>
        /// <param name="executeResult"></param>
        internal void OnWorkErrorOccurred(Exception exception, ErrorFrom errorFrom, ExecuteResultBase executeResult)
        {
            if (ErrorOccurred != null)
            {
                ErrorOccurredEventArgs e = new ErrorOccurredEventArgs(exception, errorFrom, executeResult);

                ErrorOccurred.Invoke(this, e);
            }
        }

        /// <summary>
        /// Safe callback
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="callback"></param>
        /// <param name="errorFrom"></param>
        /// <param name="executeResult"></param>
        internal void SafeCallback<TResult>(Action<ExecuteResult<TResult>> callback, ErrorFrom errorFrom, ExecuteResultBase executeResult)
        {
            try
            {
                callback((ExecuteResult<TResult>)executeResult);
            }
            catch (ThreadInterruptedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (ErrorOccurred != null)
                {
                    ErrorOccurredEventArgs e = new ErrorOccurredEventArgs(ex, errorFrom, executeResult);

                    ErrorOccurred.Invoke(this, e);
                }
            }
        }

        /// <summary>
        /// Call this function inside the work logic where you want to pause when user call Pause(...)
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
        /// Call this function inside the work logic where you want to stop when user call Stop(...)
        /// To exit the logic, the function will throw a PowerThreadPool.Exceptions.WorkStopException. Do not catch it. 
        /// If you do not want to exit the logic in this way (for example, if you have some unmanaged resources that need to be released before exiting), it is recommended to use CheckIfRequestedStop. 
        /// </summary>
        public void StopIfRequested()
        {
            WorkBase work = null;
            bool res = CheckIfRequestedStopAndGetWork(ref work);

            if (!res)
            {
                settedWorkDic.Clear();
                workGroupDic.Clear();
                throw new WorkStopException();
            }
            else if (work != null)
            {
                settedWorkDic.TryRemove(work.ID, out _);
                if (work.Group != null)
                {
                    if (workGroupDic.TryGetValue(work.Group, out ConcurrentSet<string> idSet))
                    {
                        idSet.Remove(work.ID);
                    }
                }
                throw new WorkStopException();
            }
        }

        /// <summary>
        /// Call this function inside the work logic where you want to check if requested stop (if user call Stop(...))
        /// When returning true, you can perform some pre operations (such as releasing unmanaged resources) and then safely exit the logic.
        /// </summary>
        /// <returns>Requested stop or not</returns>
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
        /// Call this function inside the work logic where you want to check if requested stop (if user call Stop(...))
        /// </summary>
        /// <param name="work">The work executing now in current thread</param>
        /// <returns>Return false if stop all</returns>
        private bool CheckIfRequestedStopAndGetWork(ref WorkBase work)
        {
            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                return false;
            }

            foreach (Worker worker in aliveWorkerList)
            {
                if (worker.workerState == WorkerStates.Running && worker.thread == Thread.CurrentThread && worker.IsCancellationRequested())
                {
                    work = worker.Work;
                    return true;
                }
            }

            return true;
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
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
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
                settedWorkDic.Clear();
                workGroupDic.Clear();
                IEnumerable<Worker> workersToStop = aliveWorkerList;
                foreach (Worker worker in workersToStop)
                {
                    worker.ForceStop();
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
        /// Stop work by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
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
        /// <param name="forceStop">Call Thread.Interrupt() for force stop</param>
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
            bool res = false;
            if (string.IsNullOrEmpty(id))
            {
                res = false;
            }
            else if (settedWorkDic.TryGetValue(id, out Worker workerToPause))
            {
                res = workerToPause.Resume(id);
            }
            return res;
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
        /// Get all members of a group
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns>Work id list</returns>
        public IEnumerable<string> GetGroupMemberList(string groupName)
        {
            if (workGroupDic.TryGetValue(groupName, out ConcurrentSet<string> groupMemberList))
            {
                return groupMemberList;
            }
            return new ConcurrentSet<string>();
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
                        worker.Dispose();
                    }
                    cancellationTokenSource.Dispose();
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