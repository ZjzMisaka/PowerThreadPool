using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Xml.Linq;

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

        public delegate void IdleEventHandler(object sender, EventArgs e);
        public event IdleEventHandler Idle;
        public delegate void ThreadStartEventHandler(object sender, ThreadStartEventArgs e);
        public event ThreadStartEventHandler ThreadStart;
        public delegate void ThreadEndEventHandler(object sender, ThreadEndEventArgs e);
        public event ThreadEndEventHandler ThreadEnd;


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
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="callBack"></param>
        /// <returns>thread id</returns>
        public string QueueWorkItem(Action action, Action<ExecuteResult<object>> callBack = null)
        {
            Func<object> func = () => { action(); return null; };
            return QueueWorkItem<object>(func, callBack);
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
            Func<object[], object> func = (param) => { action(param); return null; };
            return QueueWorkItem<object>(func, param, callBack);
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
            Func<T1, object> func = (param) => { action(param1); return null; };
            return QueueWorkItem<T1, object>(func, param1, callBack);
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
            Func<T1, T2, object> func = (param1, param2) => { action(param1, param2); return null; };
            return QueueWorkItem<T1, T2, object>(func, param1, param2, callBack);
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
            Func<T1, T2, T3, object> func = (param1, param2, param3) => { action(param1, param2, param3); return null; };
            return QueueWorkItem<T1, T2, T3, object>(func, param1, param2, param3, callBack);
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
            Func<T1, T2, T3, T4, object> func = (param1, param2, param3, param4) => { action(param1, param2, param3, param4); return null; };
            return QueueWorkItem<T1, T2, T3, T4, object>(func, param1, param2, param3, param4, callBack);
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
            Func<T1, T2, T3, T4, T5, object> func = (param1, param2, param3, param4, param5) => { action(param1, param2, param3, param4, param5); return null; };
            return QueueWorkItem<T1, T2, T3, T4, T5, object>(func, param1, param2, param3, param4, param5, callBack);
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
            Func<TResult> func = () => { return function(param1); };
            return QueueWorkItem<TResult>(func, callBack);
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
            Func<TResult> func = () => { return function(param1, param2); };
            return QueueWorkItem<TResult>(func, callBack);
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
            Func<TResult> func = () => { return function(param1, param2, param3); };
            return QueueWorkItem<TResult>(func, callBack);
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
            Func<TResult> func = () => { return function(param1, param2, param3, param4); };
            return QueueWorkItem<TResult>(func, callBack);
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
            Func<TResult> func = () => { return function(param1, param2, param3, param4, param5); };
            return QueueWorkItem<TResult>(func, callBack);
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
            Func<object[], TResult> func = (param) => { return function(); };
            return QueueWorkItem<TResult>(func, new object[0], callBack);
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
            ExecuteResult<TResult> excuteResult = new ExecuteResult<TResult>();
            string guid = Guid.NewGuid().ToString();
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
                if (callBack != null)
                {
                    callBack(excuteResult);
                }

                runningThreadDic.Remove(guid, out _);
                manualResetEventDic.Remove(guid, out _);
                cancellationTokenSourceDic.Remove(guid, out _);
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
                    dequeueRes = waitingThreadDic.Remove(id, out thread);
                    if (dequeueRes)
                    {
                        runningThreadDic[thread.Name] = thread;
                        thread.Start();

                        if (ThreadStart != null)
                        {
                            ThreadStart.Invoke(this, new ThreadStartEventArgs() { ThreadId = thread.Name });
                        }
                    }
                }
            }
        }

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
            manualResetEvent.Reset();
        }

        /// <summary>
        /// Resume all threads
        /// </summary>
        /// <param name="resumeThreadPausedById"></param>
        public void Resume(bool resumeThreadPausedById = false)
        {
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
            manualResetEventDic[id].Reset();
        }

        /// <summary>
        /// Resume thread by id
        /// </summary>
        /// <param name="id">thread id</param>
        public void Resume(string id)
        {
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
            return waitingThreadDic.Remove(id, out _);
        }
    }
}