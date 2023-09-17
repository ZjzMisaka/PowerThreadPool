using PowerThreadPool.Collections;
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
        private ManualResetEvent manualResetEvent = new ManualResetEvent(true);
        private ConcurrentDictionary<string, ManualResetEvent> manualResetEventDic = new ConcurrentDictionary<string, ManualResetEvent>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private ConcurrentDictionary<string, CancellationTokenSource> cancellationTokenSourceDic = new ConcurrentDictionary<string, CancellationTokenSource>();

        private ConcurrentQueue<Worker> idleWorkerQueue = new ConcurrentQueue<Worker>();
        private ConcurrentDictionary<string, int> waitingDependentDic = new ConcurrentDictionary<string, int>();
        private PriorityQueue<string> waitingWorkIdQueue = new PriorityQueue<string>();
        private ConcurrentDictionary<string, WorkBase> waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();
        private ConcurrentDictionary<string, Worker> runningWorkerDic = new ConcurrentDictionary<string, Worker>();
        private PowerPoolOption powerPoolOption;
        public PowerPoolOption PowerPoolOption { get => powerPoolOption; set => powerPoolOption = value; }

        public delegate void ThreadPoolStartEventHandler(object sender, EventArgs e);
        public event ThreadPoolStartEventHandler ThreadPoolStart;
        public delegate void ThreadPoolIdleEventHandler(object sender, EventArgs e);
        public event ThreadPoolIdleEventHandler ThreadPoolIdle;
        public delegate void ThreadStartEventHandler(object sender, WorkStartEventArgs e);
        public event ThreadStartEventHandler ThreadStart;
        public delegate void ThreadEndEventHandler(object sender, WorkEndEventArgs e);
        public event ThreadEndEventHandler ThreadEnd;
        public delegate void ThreadPoolTimeoutEventHandler(object sender, EventArgs e);
        public event ThreadPoolTimeoutEventHandler ThreadPoolTimeout;
        public delegate void ThreadTimeoutEventHandler(object sender, TimeoutEventArgs e);
        public event ThreadTimeoutEventHandler ThreadTimeout;
        public delegate void ThreadForceStopEventHandler(object sender, ForceStopEventArgs e);
        public event ThreadForceStopEventHandler ThreadForceStop;

        internal delegate void CallbackEndEventHandler(string id);
        internal event CallbackEndEventHandler CallbackEnd;

        private System.Timers.Timer threadPoolTimer;
        private ConcurrentDictionary<string, System.Timers.Timer> threadPoolTimerDic = new ConcurrentDictionary<string, System.Timers.Timer>();
        private ConcurrentDictionary<string, System.Timers.Timer> idleWorkerTimerDic = new ConcurrentDictionary<string, System.Timers.Timer>();

        private object lockObj = new object();

        private bool threadPoolRunning = false;
        public bool ThreadPoolRunning { get => threadPoolRunning; }

        private bool threadPoolStopping = false;
        public bool ThreadPoolStopping { get => threadPoolStopping; }

        public int IdleThreadCount
        {
            get
            {
                return idleWorkerQueue.Count;
            }
        }
        public int WaitingWorkCount
        {
            get 
            { 
                return waitingWorkDic.Count; 
            }
        }
        public IEnumerable<string> WaitingWorkList
        {
            get
            {
                return waitingWorkDic.Keys.ToList();
            }
        }
        public int RunningWorkerCount
        {
            get 
            {
                return runningWorkerDic.Count;
            }
        }
        public IEnumerable<string> RunningWorkerList
        {
            get
            {
                return runningWorkerDic.Keys.ToList();
            }
        }

        public PowerPool()
        {
            PowerPoolOption = new PowerPoolOption();
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
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), new object[0], option);
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="option"></param>
        /// <returns>work id</returns>
        public string QueueWorkItem(Action action, WorkOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), new object[0], option);
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
            return QueueWorkItem<TResult>(DelegateHelper<TResult>.ToNormalFunc(function), new object[] { }, option);
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
            return QueueWorkItem<TResult>(DelegateHelper<TResult>.ToNormalFunc(function), new object[] { }, option);
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

            if (ThreadPoolStopping)
            {
                return null;
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

            TimeoutOption threadTimeoutOption = null;

            
            if (option.Timeout != null)
            {
                threadTimeoutOption = option.Timeout;
            }
            else if (powerPoolOption.DefaultWorkTimeout != null)
            {
                threadTimeoutOption = powerPoolOption.DefaultWorkTimeout;
            }
            if (threadTimeoutOption != null)
            {
                System.Timers.Timer timer = new System.Timers.Timer(threadTimeoutOption.Duration);
                timer.AutoReset = false;
                timer.Elapsed += (s, e) =>
                {
                    if (ThreadTimeout != null)
                    {
                        ThreadTimeout.Invoke(this, new TimeoutEventArgs() { ID = workID });
                    }
                    this.Stop(workID, threadTimeoutOption.ForceStop);
                };

                threadPoolTimerDic[workID] = timer;
            }
            
            Work<TResult> work = new Work<TResult>(this, workID, function, param, option);
            manualResetEventDic[workID] = new ManualResetEvent(true);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSourceDic[workID] = cancellationTokenSource;

            lock (lockObj)
            {
                if (option.Dependents == null || option.Dependents.Count == 0)
                {
                    waitingWorkIdQueue.Enqueue(workID, option.WorkPriority);
                }
                else
                {
                    waitingDependentDic[workID] = option.WorkPriority;
                }
            
                waitingWorkDic[workID] = work;
            
                CheckAndRunThread();
            }

            return workID;
        }

        /// <summary>
        /// Set work into waiting queue
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="workId"></param>
        internal void SetWorkIntoWaitingQueue<TResult>(string workId)
        {
            int priority;
            if (waitingDependentDic.TryRemove(workId, out priority))
            {
                waitingWorkIdQueue.Enqueue(workId, priority);
            }
        }


        /// <summary>
        /// One thread end
        /// </summary>
        /// <param name="executeResult"></param>
        internal void OneThreadEnd(ExecuteResultBase executeResult)
        {
            System.Timers.Timer timer;
            if (threadPoolTimerDic.TryRemove(executeResult.ID, out timer))
            {
                timer.Stop();
                timer.Enabled = false;
            }

            InvokeThreadEndEvent(executeResult);
        }

        /// <summary>
        /// One thread end error
        /// </summary>
        /// <param name="executeResult"></param>
        internal void OneThreadEndByForceStop(string id)
        {
            System.Timers.Timer timer;
            if (threadPoolTimerDic.TryRemove(id, out timer))
            {
                timer.Stop();
                timer.Enabled = false;
            }
            if (ThreadForceStop != null)
            {
                ThreadForceStop.Invoke(this, new ForceStopEventArgs()
                {
                    ID = id
                });
            }
        }

        /// <summary>
        /// Invoke thread end event
        /// </summary>
        /// <param name="executeResult"></param>
        private void InvokeThreadEndEvent(ExecuteResultBase executeResult)
        {
            if (ThreadEnd != null)
            {
                ThreadEnd.Invoke(this, new WorkEndEventArgs()
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
        internal void WorkEnd(string guid, bool isForceStop)
        {
            if (CallbackEnd != null)
            {
                CallbackEnd.Invoke(guid);
            }

            Worker worker;

            lock (lockObj)
            {
                if (!isForceStop)
                {
                    if (runningWorkerDic.TryRemove(guid, out worker))
                    {
                        idleWorkerQueue.Enqueue(worker);
                        SetDestroyTimerForIdleWorker(worker.Id);
                    }
                }

                manualResetEventDic.TryRemove(guid, out _);
                cancellationTokenSourceDic.TryRemove(guid, out _);

                CheckAndRunThread();
                CheckIdle();
            }
        }

        /// <summary>
        /// Manage idle worker queue
        /// </summary>
        private void ManageIdleWorkerQueue()
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
            while (IdleThreadCount + RunningWorkerCount < minThreads)
            {
                Worker worker = new Worker(this);
                idleWorkerQueue.Enqueue(worker);
                SetDestroyTimerForIdleWorker(worker.Id);
            }

            while (WaitingWorkCount > 0 && IdleThreadCount < 1 && IdleThreadCount + RunningWorkerCount < powerPoolOption.MaxThreads)
            {
                Worker worker = new Worker(this);
                idleWorkerQueue.Enqueue(worker);
                SetDestroyTimerForIdleWorker(worker.Id);
            }

            while (IdleThreadCount + RunningWorkerCount > powerPoolOption.MaxThreads)
            {
                idleWorkerQueue.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Set destroy timer for idle worker
        /// </summary>
        private void SetDestroyTimerForIdleWorker(string workerID)
        {
            if (powerPoolOption.DestroyThreadOption != null)
            {
                System.Timers.Timer timer = new System.Timers.Timer(powerPoolOption.DestroyThreadOption.KeepAliveTime);
                timer.AutoReset = false;
                timer.Elapsed += (s, e) =>
                {
                    if (IdleThreadCount > powerPoolOption.DestroyThreadOption.MinThreads)
                    {
                        Worker worker;
                        if (idleWorkerQueue.TryDequeue(out worker))
                        {
                            worker.Kill();

                            timer.Stop();
                            idleWorkerTimerDic.TryRemove(workerID, out _);
                        }
                    }
                };
                timer.Start();
                idleWorkerTimerDic[workerID] = timer;
            }
        }

        /// <summary>
        /// Check if a thread pool thread becomes available. when available, get methods from queue and executes them.
        /// </summary>
        private void CheckAndRunThread()
        {
            ManageIdleWorkerQueue();

            while (RunningWorkerCount < powerPoolOption.MaxThreads && waitingWorkIdQueue.Count > 0)
            {
                WorkBase work;
                if (IdleThreadCount == 0)
                {
                    return;
                }
                string id = waitingWorkIdQueue.Dequeue();
                if (id != null && id != default(string))
                {
                    if (waitingWorkDic.TryRemove(id, out work))
                    {
                        CheckThreadPoolStart();
                        Worker worker;
                        if (idleWorkerQueue.TryDequeue(out worker))
                        {
                            runningWorkerDic[work.ID] = worker;
                            worker.AssignTask(work);

                            if (threadPoolTimerDic.TryGetValue(id, out System.Timers.Timer timer))
                            {
                                timer.Start();
                            }

                            if (ThreadStart != null)
                            {
                                ThreadStart.Invoke(this, new WorkStartEventArgs() { ID = work.ID });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if it's the start of thread pool
        /// </summary>
        private void CheckThreadPoolStart()
        {
            if (!ThreadPoolRunning)
            {
                threadPoolRunning = true;

                if (ThreadPoolStart != null)
                {
                    ThreadPoolStart.Invoke(this, new EventArgs());
                }

                if (powerPoolOption.Timeout != null)
                {
                    threadPoolTimer = new System.Timers.Timer(powerPoolOption.Timeout.Duration);
                    threadPoolTimer.AutoReset = false;
                    threadPoolTimer.Elapsed += (s, e) =>
                    {
                        if (ThreadPoolTimeout != null)
                        {
                            ThreadPoolTimeout.Invoke(this, new EventArgs());
                        }
                        this.Stop(powerPoolOption.Timeout.ForceStop);
                    };
                    threadPoolTimer.Start();
                }
            }
        }

        /// <summary>
        /// Check if thread pool is idle
        /// </summary>
        private void CheckIdle()
        {
            if (RunningWorkerCount == 0 && WaitingWorkCount == 0 && threadPoolRunning)
            {
                threadPoolRunning = false;
                if (threadPoolStopping)
                {
                    threadPoolStopping = false;
                }

                if (ThreadPoolIdle != null)
                {
                    ThreadPoolIdle.Invoke(this, new EventArgs());
                }

                if (threadPoolTimer != null)
                {
                    threadPoolTimer.Stop();
                    threadPoolTimer.Enabled = false;
                }

                manualResetEvent = new ManualResetEvent(true);
                manualResetEventDic = new ConcurrentDictionary<string, ManualResetEvent>();
                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSourceDic = new ConcurrentDictionary<string, CancellationTokenSource>();

                waitingWorkIdQueue = new PriorityQueue<string>();
                waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();
                waitingDependentDic = new ConcurrentDictionary<string, int>();
                runningWorkerDic = new ConcurrentDictionary<string, Worker>();

                threadPoolTimerDic = new ConcurrentDictionary<string, System.Timers.Timer>();
            }
        }

        /// <summary>
        /// Blocks the calling thread until all of the works terminates.
        /// </summary>
        public void Wait()
        {
            while (true)
            {
                if (RunningWorkerCount > 0)
                {
                    runningWorkerDic.Values.First().Wait();
                }
                else
                {
                    break;
                }
            }
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
            if (runningWorkerDic.TryGetValue(id, out Worker worker))
            {
                worker.Wait();
                return true;
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
        /// ForceStop all threads
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return false if no thread running</returns>
        public bool Stop(bool forceStop = false)
        {
            if (RunningWorkerCount == 0 && WaitingWorkCount == 0)
            {
                return false;
            }

            threadPoolStopping = true;

            waitingWorkIdQueue = new PriorityQueue<string>();
            waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();
            waitingDependentDic = new ConcurrentDictionary<string, int>();

            if (forceStop)
            {
                List<Worker> workersToStop = new List<Worker>(runningWorkerDic.Values);
                runningWorkerDic.Clear();
                foreach (Worker worker in workersToStop)
                {
                    worker.ForceStop();
                }
            }
            else
            {
                cancellationTokenSource.Cancel();
            }

            return true;
        }

        /// <summary>
        /// ForceStop all threads
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
        /// ForceStop thread by id
        /// </summary>
        /// <param name="id">work id</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return false if the thread isn't running</returns>
        public bool Stop(string id, bool forceStop = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            bool res = false;
            foreach (string runningId in runningWorkerDic.Keys)
            {
                if (id == runningId)
                {
                    if (forceStop)
                    {
                        if (runningWorkerDic.TryRemove(runningId, out Worker workerTpStop))
                        {
                            workerTpStop.ForceStop();
                            res = true;
                        }
                    }
                    else
                    {
                        cancellationTokenSourceDic[runningId].Cancel();
                        res = true;
                    }
                    
                    break;
                }
            }
            return res;
        }

        /// <summary>
        /// ForceStop thread by id
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
            manualResetEvent.WaitOne();
            foreach (string id in manualResetEventDic.Keys)
            {
                if (Thread.CurrentThread.Name == id)
                {
                    manualResetEventDic[id].WaitOne();
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
            if (threadPoolTimer != null)
            {
                threadPoolTimer.Stop();
            }
            manualResetEvent.Reset();
        }

        /// <summary>
        /// Resume all threads
        /// </summary>
        /// <param name="resumeThreadPausedById"></param>
        public void Resume(bool resumeThreadPausedById = false)
        {
            if (threadPoolTimer != null)
            {
                threadPoolTimer.Start();
            }
            manualResetEvent.Set();
            if (resumeThreadPausedById)
            {
                foreach (ManualResetEvent manualResetEvent in manualResetEventDic.Values)
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
            if (threadPoolTimerDic.TryGetValue(id, out System.Timers.Timer timer))
            {
                timer.Stop();
            }
            if (manualResetEventDic.TryGetValue(id, out ManualResetEvent manualResetEvent))
            {
                manualResetEvent.Reset();
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
            if (threadPoolTimerDic.TryGetValue(id, out System.Timers.Timer timer))
            {
                timer.Start();
            }
            if (manualResetEventDic.TryGetValue(id, out ManualResetEvent manualResetEvent))
            {
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
            waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();
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
            return waitingWorkDic.TryRemove(id, out _);
        }
    }
}