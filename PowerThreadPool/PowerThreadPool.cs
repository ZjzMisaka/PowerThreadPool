using System;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace PowerThreadPool
{
    public class PowerPool
    {
        private ManualResetEvent manualResetEvent = new ManualResetEvent(true);
        private ConcurrentDictionary<string, ManualResetEvent> manualResetEventDic = new ConcurrentDictionary<string, ManualResetEvent>();
        private ConcurrentQueue<string> waitingThreadIdQueue = new ConcurrentQueue<string>();
        private ConcurrentDictionary<string, Thread> waitingThreadDic = new ConcurrentDictionary<string, Thread>();
        private ConcurrentDictionary<string, Thread> runningThreadDic = new ConcurrentDictionary<string, Thread>();
        private ThreadPoolOption threadPoolOption;
        public ThreadPoolOption ThreadPoolOption { get => threadPoolOption; set => threadPoolOption = value; }
        public int WaitingThreadCount
        {
            get 
            { 
                return waitingThreadDic.Count; 
            }
        }
        public int RunningThreadCount
        {
            get 
            {
                return runningThreadDic.Count;
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
        public string QueueWorkItem(Action action, Action<ExcuteResult<object>> callBack = null)
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
        public string QueueWorkItem(Action<object[]> action, object[] param, Action<ExcuteResult<object>> callBack = null)
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
        public string QueueWorkItem<T1>(Action<T1> action, T1 param1, Action<ExcuteResult<object>> callBack = null)
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
        public string QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, Action<ExcuteResult<object>> callBack = null)
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
        public string QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, Action<ExcuteResult<object>> callBack = null)
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
        public string QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExcuteResult<object>> callBack = null)
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
        public string QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExcuteResult<object>> callBack = null)
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
        public string QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, Action<ExcuteResult<TResult>> callBack = null)
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
        public string QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, Action<ExcuteResult<TResult>> callBack = null)
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
        public string QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, Action<ExcuteResult<TResult>> callBack = null)
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
        public string QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExcuteResult<TResult>> callBack = null)
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
        public string QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExcuteResult<TResult>> callBack = null)
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
        public string QueueWorkItem<TResult>(Func<TResult> function, Action<ExcuteResult<TResult>> callBack = null)
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
        public string QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, Action<ExcuteResult<TResult>> callBack = null)
        {
            ExcuteResult<TResult> excuteResult = new ExcuteResult<TResult>();
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
                runningThreadDic.Remove(guid, out _);
                manualResetEventDic.Remove(guid, out _);
                CheckAndRunThread();
                if (callBack != null)
                {
                    callBack(excuteResult);
                }
            });
            manualResetEventDic[guid] = new ManualResetEvent(true);
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
                    }
                }
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
        public void Stop()
        {
            waitingThreadIdQueue = new ConcurrentQueue<string>();
            waitingThreadDic = new ConcurrentDictionary<string, Thread>();
            foreach (Thread thread in runningThreadDic.Values) 
            {
                thread.Interrupt();
                thread.Join();
            }
            runningThreadDic = new ConcurrentDictionary<string, Thread>();
        }

        /// <summary>
        /// Stop all threads
        /// </summary>
        /// <returns>A Task</returns>
        public async Task StopAsync()
        {
            await Task.Run(() =>
            {
                Stop();
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