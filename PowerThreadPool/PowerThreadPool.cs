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
        private ConcurrentQueue<string> waitingThreadIdQueue = new ConcurrentQueue<string>();
        private ConcurrentDictionary<string, Thread> waitingThreadDic = new ConcurrentDictionary<string, Thread>();
        private ConcurrentDictionary<string, Thread> runningThreadDic = new ConcurrentDictionary<string, Thread>();
        private ThreadPoolOption threadPoolOption;
        public ThreadPoolOption ThreadPoolOption { get => threadPoolOption; set => threadPoolOption = value; }

        public delegate void ThreadPoolStartEventHandler(object sender, EventArgs e);
        public event ThreadPoolStartEventHandler ThreadPoolStart;
        public delegate void IdleEventHandler(object sender, EventArgs e);
        public event IdleEventHandler Idle;
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


        public int WaitingThreadCount
        {
            get 
            { 
                return waitingThreadDic.Count; 
            }
        }
        public IEnumerable<string> WaitingThreadList
        {
            get
            {
                return waitingThreadDic.Keys.ToList();
            }
        }
        public int RunningThreadCount
        {
            get 
            {
                return runningThreadDic.Count;
            }
        }
        public IEnumerable<string> RunningThreadList
        {
            get
            {
                return runningThreadDic.Keys.ToList();
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
            
            Thread thread = new Thread(() =>
            {
                try
                {
                    excuteResult.Result = function(param);
                    excuteResult.Status = Status.Succeed;
                }
                catch (Exception ex)
                {
                    excuteResult.Status = Status.Failed;
                    excuteResult.Exception = ex;
                }

                if (ThreadEnd != null)
                {
                    ThreadEnd.Invoke(this, new ThreadEndEventArgs() { Exception = excuteResult.Exception, Result = excuteResult.Result, Status = excuteResult.Status });
                }
                if (option.Callback != null)
                {
                    option.Callback(excuteResult);
                }
                else if (threadPoolOption.DefaultCallback != null)
                {
                    threadPoolOption.DefaultCallback(excuteResult as ExecuteResult<object>);
                }

                runningThreadDic.TryRemove(guid, out _);
                manualResetEventDic.TryRemove(guid, out _);
                cancellationTokenSourceDic.TryRemove(guid, out _);
                CheckAndRunThread();
                CheckIdle();
            });
            manualResetEventDic[guid] = new ManualResetEvent(true);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSourceDic[guid] = cancellationTokenSource;
            thread.Name = guid;
            waitingThreadIdQueue.Enqueue(guid);
            waitingThreadDic[guid] = thread;
            
            CheckAndRunThread();

            return guid;
        }

        /// <summary>
        /// Check if a thread pool thread becomes available. when available, get methods from queue and executes them.
        /// </summary>
        private void CheckAndRunThread()
        {
            while (RunningThreadCount < threadPoolOption.MaxThreads && WaitingThreadCount > 0)
            {
                string id;
                Thread thread;
                bool dequeueRes = waitingThreadIdQueue.TryDequeue(out id);
                if (dequeueRes)
                {
                    dequeueRes = waitingThreadDic.TryRemove(id, out thread);
                    if (dequeueRes)
                    {
                        CheckThreadPoolStart();
                        runningThreadDic[thread.Name] = thread;
                        if (threadPoolTimerDic.ContainsKey(id))
                        {
                            threadPoolTimerDic[id].Start();
                        }

                        thread.Start();

                        if (ThreadStart != null)
                        {
                            ThreadStart.Invoke(this, new ThreadStartEventArgs() { ThreadId = thread.Name });
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
            if (RunningThreadCount == 0 && WaitingThreadCount == 0)
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
            if (RunningThreadCount == 0 && WaitingThreadCount == 0)
            {
                if (Idle != null)
                {
                    Idle.Invoke(this, new EventArgs());
                }

                manualResetEvent = new ManualResetEvent(true);
                manualResetEventDic = new ConcurrentDictionary<string, ManualResetEvent>();
                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSourceDic = new ConcurrentDictionary<string, CancellationTokenSource>();

                waitingThreadIdQueue = new ConcurrentQueue<string>();
                waitingThreadDic = new ConcurrentDictionary<string, Thread>();
                runningThreadDic = new ConcurrentDictionary<string, Thread>();
            }
        }

        /// <summary>
        /// Blocks the calling thread until all of the threads terminates.
        /// </summary>
        public void Wait()
        {
            while (true)
            {
                Thread threadToWait;

                if (RunningThreadCount > 0)
                {
                    threadToWait = runningThreadDic.Values.First();
                }
                else
                {
                    break;
                }

                threadToWait.Join();
            }
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
        /// Stop all threads
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        public void Stop(bool forceStop = false)
        {
            waitingThreadIdQueue = new ConcurrentQueue<string>();
            waitingThreadDic = new ConcurrentDictionary<string, Thread>();

            cancellationTokenSource.Cancel();

            if (forceStop)
            {
                foreach (Thread thread in runningThreadDic.Values)
                {
                    thread.Interrupt();
                    thread.Join();
                }
            }
        }

        /// <summary>
        /// Stop all threads
        /// </summary>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>A Task</returns>
        public async Task StopAsync(bool forceStop = false)
        {
            await Task.Run(() =>
            {
                Stop(forceStop);
            });
        }

        /// <summary>
        /// Stop thread by id
        /// </summary>
        /// <param name="id">thread id</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>If thread is in progress during the invocation</returns>
        public bool Stop(string id, bool forceStop = false)
        {
            bool res = false;
            foreach (string runningId in runningThreadDic.Keys)
            {
                if (id == runningId)
                {
                    cancellationTokenSourceDic[runningId].Cancel();

                    if (forceStop)
                    {
                        runningThreadDic[runningId].Interrupt();
                        runningThreadDic[runningId].Join();
                    }

                    res = true;
                    break;
                }
            }
            return res;
        }

        /// <summary>
        /// Stop thread by id
        /// </summary>
        /// <param name="id">thread id</param>
        /// <param name="forceStop">Call Thread.Interrupt() and Thread.Join() for force stop</param>
        /// <returns>If thread is in progress during the invocation</returns>
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
        /// Call this function inside the thread logic where you want to stop when user call Stop(...)
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
        /// Call this function inside the thread logic where you want to check if requested stop (if user call Stop(...))
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
            waitingThreadDic = new ConcurrentDictionary<string, Thread>();
        }

        /// <summary>
        /// Cancel the task by id if the task has not started running
        /// </summary>
        /// <param name="id">thread id</param>
        /// <returns>is succeed</returns>
        public bool Cancel(string id)
        {
            return waitingThreadDic.TryRemove(id, out _);
        }
    }
}