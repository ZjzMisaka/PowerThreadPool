using PowerThreadPool.Helper;
using PowerThreadPool.Option;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace PowerThreadPool
{
    public class PowerPool
    {
        private ManualResetEvent manualResetEvent = new ManualResetEvent(true);
        private ConcurrentDictionary<string, ManualResetEvent> manualResetEventDic = new ConcurrentDictionary<string, ManualResetEvent>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private ConcurrentDictionary<string, CancellationTokenSource> cancellationTokenSourceDic = new ConcurrentDictionary<string, CancellationTokenSource>();

        private ConcurrentQueue<Worker> idleWorkerQueue = new ConcurrentQueue<Worker>();
        private PriorityQueue<string> waitingThreadIdQueue = new PriorityQueue<string>();
        private ConcurrentDictionary<string, WorkBase> waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();
        private ConcurrentDictionary<string, Worker> runningWorkerDic = new ConcurrentDictionary<string, Worker>();
        private ThreadPoolOption threadPoolOption;
        public ThreadPoolOption ThreadPoolOption { get => threadPoolOption; set => threadPoolOption = value; }

        public delegate void ThreadPoolStartEventHandler(object sender, EventArgs e);
        public event ThreadPoolStartEventHandler ThreadPoolStart;
        public delegate void ThreadPoolIdleEventHandler(object sender, EventArgs e);
        public event ThreadPoolIdleEventHandler ThreadPoolIdle;
        public delegate void ThreadStartEventHandler(object sender, ThreadStartEventArgs e);
        public event ThreadStartEventHandler ThreadStart;
        public delegate void ThreadEndEventHandler(object sender, ThreadEndEventArgs e);
        public event ThreadEndEventHandler ThreadEnd;
        public delegate void ThreadPoolTimeoutEventHandler(object sender, EventArgs e);
        public event ThreadPoolTimeoutEventHandler ThreadPoolTimeout;
        public delegate void ThreadTimeoutEventHandler(object sender, ThreadTimeoutEventArgs e);
        public event ThreadTimeoutEventHandler ThreadTimeout;

        private System.Timers.Timer threadPoolTimer;
        private ConcurrentDictionary<string, System.Timers.Timer> threadPoolTimerDic = new ConcurrentDictionary<string, System.Timers.Timer>();
        private List<System.Timers.Timer> idleWorkerTimerList = new List<System.Timers.Timer>();
        private object idleWorkerTimerListLock = new object();

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
        }

        public PowerPool(ThreadPoolOption threadPoolOption)
        {
            ThreadPoolOption = threadPoolOption;

            if (threadPoolOption.Timeout != null)
            {
                threadPoolTimer = new System.Timers.Timer(threadPoolOption.Timeout.Duration);
                threadPoolTimer.AutoReset = false;
                threadPoolTimer.Elapsed += (s, e) => 
                {
                    if (ThreadPoolTimeout != null)
                    {
                        ThreadPoolTimeout.Invoke(this, new EventArgs());
                    }
                    this.Stop(threadPoolOption.Timeout.ForceStop);
                };
            }
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="callBack"></param>
        /// <returns>thread id</returns>
        public string QueueWorkItem(Action action, Action<ExecuteResult<object>> callBack = null)
        {
            ThreadOption option = new ThreadOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), new object[0], option);
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="option"></param>
        /// <returns>thread id</returns>
        public string QueueWorkItem(Action action, ThreadOption option)
        {
            return QueueWorkItem<object>(DelegateHelper<object>.ToNormalFunc(action), new object[0], option);
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="callBack"></param>
        /// <returns>thread id</returns>
        public string QueueWorkItem(Action<object[]> action, object[] param, Action<ExecuteResult<object>> callBack = null)
        {
            ThreadOption option = new ThreadOption();
            option.Callback = callBack;
            return QueueWorkItem<object>(DelegateHelper<object[]>.ToNormalFunc(action, param), param, option);
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <param name="option"></param>
        /// <returns>thread id</returns>
        public string QueueWorkItem(Action<object[]> action, object[] param, ThreadOption option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1>(Action<T1> action, T1 param1, Action<ExecuteResult<object>> callBack = null)
        {
            ThreadOption option = new ThreadOption();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1>(Action<T1> action, T1 param1, ThreadOption option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, Action<ExecuteResult<object>> callBack = null)
        {
            ThreadOption option = new ThreadOption();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, ThreadOption option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, Action<ExecuteResult<object>> callBack = null)
        {
            ThreadOption option = new ThreadOption();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, ThreadOption option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResult<object>> callBack = null)
        {
            ThreadOption option = new ThreadOption();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, ThreadOption option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResult<object>> callBack = null)
        {
            ThreadOption option = new ThreadOption();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, ThreadOption option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, Action<ExecuteResult<TResult>> callBack = null)
        {
            ThreadOption<TResult> option = new ThreadOption<TResult>();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, ThreadOption<TResult> option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, Action<ExecuteResult<TResult>> callBack = null)
        {
            ThreadOption<TResult> option = new ThreadOption<TResult>();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, ThreadOption<TResult> option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, Action<ExecuteResult<TResult>> callBack = null)
        {
            ThreadOption<TResult> option = new ThreadOption<TResult>();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, ThreadOption<TResult> option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExecuteResult<TResult>> callBack = null)
        {
            ThreadOption<TResult> option = new ThreadOption<TResult>();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, ThreadOption<TResult> option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExecuteResult<TResult>> callBack = null)
        {
            ThreadOption<TResult> option = new ThreadOption<TResult>();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, ThreadOption<TResult> option)
        {
            return QueueWorkItem<TResult>(DelegateHelper<T1, T2, T3, T4, T5, TResult>.ToNormalFunc(function, param1, param2, param3, param4, param5), new object[] { param1, param2, param3, param4, param5 }, option);
        }


        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="callBack"></param>
        /// <returns>thread id</returns>
        public string QueueWorkItem<TResult>(Func<TResult> function, Action<ExecuteResult<TResult>> callBack = null)
        {
            ThreadOption<TResult> option = new ThreadOption<TResult>();
            option.Callback = callBack;
            return QueueWorkItem<TResult>(DelegateHelper<TResult>.ToNormalFunc(function), new object[] { }, option);
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="option"></param>
        /// <returns>thread id</returns>
        public string QueueWorkItem<TResult>(Func<TResult> function, ThreadOption<TResult> option)
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, Action<ExecuteResult<TResult>> callBack = null)
        {
            ThreadOption<TResult> option = new ThreadOption<TResult>();
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
        /// <returns>thread id</returns>
        public string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, ThreadOption<TResult> option)
        {
            ExecuteResult<TResult> excuteResult = new ExecuteResult<TResult>();
            string guid = Guid.NewGuid().ToString();
            excuteResult.ID = guid;

            TimeoutOption threadTimeoutOption = null;
            if (option.Timeout != null)
            {
                threadTimeoutOption = option.Timeout;
            }
            else if (threadPoolOption.DefaultThreadTimeout != null)
            {
                threadTimeoutOption = threadPoolOption.DefaultThreadTimeout;
            }
            if (threadTimeoutOption != null)
            {
                System.Timers.Timer timer = new System.Timers.Timer(threadTimeoutOption.Duration);
                timer.AutoReset = false;
                timer.Elapsed += (s, e) =>
                {
                    if (ThreadTimeout != null)
                    {
                        ThreadTimeout.Invoke(this, new ThreadTimeoutEventArgs() { ID = guid });
                    }
                    this.Stop(guid, threadTimeoutOption.ForceStop);
                };
                threadPoolTimerDic[guid] = timer;
            }

            Work<TResult> work = new Work<TResult>(guid, function, param, option);
            manualResetEventDic[guid] = new ManualResetEvent(true);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSourceDic[guid] = cancellationTokenSource;
            waitingThreadIdQueue.Enqueue(guid, option.Priority);
            waitingWorkDic[guid] = work;
            
            CheckAndRunThread();

            return guid;
        }

        /// <summary>
        /// Invoke thread end event
        /// </summary>
        /// <param name="executeResult"></param>
        public void InvokeThreadEndEvent(ExecuteResultBase executeResult)
        {
            if (ThreadEnd != null)
            {
                ThreadEnd.Invoke(this, new ThreadEndEventArgs()
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
        public void WorkEnd(string guid)
        {
            Worker worker;
            if (runningWorkerDic.TryRemove(guid, out worker))
            {
                idleWorkerQueue.Enqueue(worker);
                SetDestroyTimerForIdleWorker();
            }
            manualResetEventDic.TryRemove(guid, out _);
            cancellationTokenSourceDic.TryRemove(guid, out _);
            CheckAndRunThread();
            CheckIdle();
        }

        /// <summary>
        /// Manage idle worker queue
        /// </summary>
        private void ManageIdleWorkerQueue()
        {
            if (threadPoolOption.DestroyThreadOption != null)
            {
                if (threadPoolOption.DestroyThreadOption.MinThreads > threadPoolOption.MaxThreads)
                {
                    throw new ArgumentException("The minimum number of threads cannot be greater than the maximum number of threads.");
                }
            }

            int minThreads = threadPoolOption.MaxThreads;
            if (threadPoolOption.DestroyThreadOption != null)
            {
                minThreads = threadPoolOption.DestroyThreadOption.MinThreads;
            }
            while (IdleThreadCount + RunningWorkerCount < minThreads)
            {
                idleWorkerQueue.Enqueue(new Worker(this));
                SetDestroyTimerForIdleWorker();
            }

            while (WaitingWorkCount > 0 && IdleThreadCount < 1 && IdleThreadCount + RunningWorkerCount < threadPoolOption.MaxThreads)
            {
                idleWorkerQueue.Enqueue(new Worker(this));
                SetDestroyTimerForIdleWorker();
            }

            while (IdleThreadCount + RunningWorkerCount > threadPoolOption.MaxThreads)
            {
                idleWorkerQueue.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Set destroy timer for idle worker
        /// </summary>
        private void SetDestroyTimerForIdleWorker()
        {
            if (threadPoolOption.DestroyThreadOption != null)
            {
                System.Timers.Timer timer = new System.Timers.Timer(threadPoolOption.DestroyThreadOption.KeepAliveTime);
                timer.AutoReset = false;
                timer.Elapsed += (s, e) =>
                {
                    if (IdleThreadCount > threadPoolOption.DestroyThreadOption.MinThreads)
                    {
                        Worker worker;
                        if (idleWorkerQueue.TryDequeue(out worker))
                        {
                            worker.Kill();

                            timer.Stop();
                            lock (idleWorkerTimerListLock)
                            {
                                idleWorkerTimerList.Remove(timer);
                            }
                        }
                    }
                };
                timer.Start();
                lock (idleWorkerTimerListLock)
                {
                    idleWorkerTimerList.Add(timer);
                }
            }
        }

        /// <summary>
        /// Check if a thread pool thread becomes available. when available, get methods from queue and executes them.
        /// </summary>
        private void CheckAndRunThread()
        {
            ManageIdleWorkerQueue();

            while (RunningWorkerCount < threadPoolOption.MaxThreads && WaitingWorkCount > 0)
            {
                WorkBase work;
                if (IdleThreadCount == 0)
                {
                    return;
                }
                string id = waitingThreadIdQueue.Dequeue();
                if (id != null && id != default(string))
                {
                    bool dequeueRes = waitingWorkDic.TryRemove(id, out work);
                    if (dequeueRes)
                    {
                        CheckThreadPoolStart();
                        Worker worker;
                        if (idleWorkerQueue.TryDequeue(out worker))
                        {
                            runningWorkerDic[work.ID] = worker;
                            worker.AssignTask(work);
                            
                            if (threadPoolTimerDic.ContainsKey(id))
                            {
                                threadPoolTimerDic[id].Start();
                            }

                            if (ThreadStart != null)
                            {
                                ThreadStart.Invoke(this, new ThreadStartEventArgs() { ID = work.ID });
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
            if (RunningWorkerCount == 0 && WaitingWorkCount == 0)
            {
                if (ThreadPoolStart != null)
                {
                    ThreadPoolStart.Invoke(this, new EventArgs());
                }
                if (threadPoolTimer != null)
                {
                    threadPoolTimer.Start();
                }
            }
        }

        /// <summary>
        /// Check if thread pool is idle
        /// </summary>
        private void CheckIdle()
        {
            if (RunningWorkerCount == 0 && WaitingWorkCount == 0)
            {
                if (ThreadPoolIdle != null)
                {
                    ThreadPoolIdle.Invoke(this, new EventArgs());
                }

                manualResetEvent = new ManualResetEvent(true);
                manualResetEventDic = new ConcurrentDictionary<string, ManualResetEvent>();
                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSourceDic = new ConcurrentDictionary<string, CancellationTokenSource>();

                waitingThreadIdQueue = new PriorityQueue<string>();
                waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();
                runningWorkerDic = new ConcurrentDictionary<string, Worker>();

                threadPoolTimerDic = new ConcurrentDictionary<string, System.Timers.Timer>();
            }
        }

        /// <summary>
        /// Blocks the calling thread until all of the threads terminates.
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
        /// Blocks the calling thread until the thread terminates.
        /// </summary>
        /// <param name="id">thread id</param>
        /// <returns>Return false if the thread isn't running</returns>
        public bool Wait(string id)
        {
            if (runningWorkerDic.ContainsKey(id))
            {
                runningWorkerDic[id].Wait();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Blocks the calling thread until all of the threads terminates.
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
        /// Blocks the calling thread until the thread terminates.
        /// </summary>
        /// <param name="id">thread id</param>
        /// <returns>Return false if the thread isn't running</returns>
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

            waitingThreadIdQueue = new PriorityQueue<string>();
            waitingWorkDic = new ConcurrentDictionary<string, WorkBase>();

            cancellationTokenSource.Cancel();

            if (forceStop)
            {
                foreach (Worker worker in runningWorkerDic.Values)
                {
                    worker.ForceStop();
                }
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
        /// <param name="id">thread id</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>Return false if the thread isn't running</returns>
        public bool Stop(string id, bool forceStop = false)
        {
            bool res = false;
            foreach (string runningId in runningWorkerDic.Keys)
            {
                if (id == runningId)
                {
                    cancellationTokenSourceDic[runningId].Cancel();

                    if (forceStop)
                    {
                        runningWorkerDic[runningId].ForceStop();
                    }

                    res = true;
                    break;
                }
            }
            return res;
        }

        /// <summary>
        /// ForceStop thread by id
        /// </summary>
        /// <param name="id">thread id</param>
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
            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
            foreach (string id in cancellationTokenSourceDic.Keys)
            {
                if (Thread.CurrentThread.Name == id)
                {
                    if (cancellationTokenSourceDic[id].Token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                }
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
        /// <param name="id">thread id</param>
        public void Pause(string id)
        {
            if (threadPoolTimerDic.ContainsKey(id))
            {
                threadPoolTimerDic[id].Stop();
            }
            manualResetEventDic[id].Reset();
        }

        /// <summary>
        /// Resume thread by id
        /// </summary>
        /// <param name="id">thread id</param>
        public void Resume(string id)
        {
            if (threadPoolTimerDic.ContainsKey(id))
            {
                threadPoolTimerDic[id].Start();
            }
            manualResetEventDic[id].Set();
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
        /// <param name="id">thread id</param>
        /// <returns>is succeed</returns>
        public bool Cancel(string id)
        {
            return waitingWorkDic.TryRemove(id, out _);
        }
    }
}