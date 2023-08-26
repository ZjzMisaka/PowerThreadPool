using System;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace PowerThreadPool
{
    public class PowerPool
    {
        private ManualResetEvent manualResetEvent = new ManualResetEvent(true);
        private ConcurrentQueue<Thread> waitingThreadQueue = new ConcurrentQueue<Thread>();
        private ConcurrentDictionary<string, Thread> runningThreadDic = new ConcurrentDictionary<string, Thread>();
        private ThreadPoolOption threadPoolOption;
        public ThreadPoolOption ThreadPoolOption { get => threadPoolOption; set => threadPoolOption = value; }
        public int WaitingThreadCount
        {
            get { return waitingThreadQueue.Count; }
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

        public void QueueWorkItem(Action action, Action<ExcuteResult<object>> callBack = null)
        {
            Func<object> func = () => { action(); return null; };
            QueueWorkItem<object>(func, callBack);
        }

        public void QueueWorkItem(Action<object[]> action, object[] param, Action<ExcuteResult<object>> callBack = null)
        {
            Func<object[], object> func = (param) => { action(param); return null; };
            QueueWorkItem<object>(func, param, callBack);
        }

        public void QueueWorkItem<T1>(Action<T1> action, T1 param1, Action<ExcuteResult<object>> callBack = null)
        {
            Func<T1, object> func = (param) => { action(param1); return null; };
            QueueWorkItem<T1, object>(func, param1, callBack);
        }

        public void QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2, Action<ExcuteResult<object>> callBack = null)
        {
            Func<T1, T2, object> func = (param1, param2) => { action(param1, param2); return null; };
            QueueWorkItem<T1, T2, object>(func, param1, param2, callBack);
        }

        public void QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3, Action<ExcuteResult<object>> callBack = null)
        {
            Func<T1, T2, T3, object> func = (param1, param2, param3) => { action(param1, param2, param3); return null; };
            QueueWorkItem<T1, T2, T3, object>(func, param1, param2, param3, callBack);
        }

        public void QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExcuteResult<object>> callBack = null)
        {
            Func<T1, T2, T3, T4, object> func = (param1, param2, param3, param4) => { action(param1, param2, param3, param4); return null; };
            QueueWorkItem<T1, T2, T3, T4, object>(func, param1, param2, param3, param4, callBack);
        }

        public void QueueWorkItem<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExcuteResult<object>> callBack = null)
        {
            Func<T1, T2, T3, T4, T5, object> func = (param1, param2, param3, param4, param5) => { action(param1, param2, param3, param4, param5); return null; };
            QueueWorkItem<T1, T2, T3, T4, T5, object>(func, param1, param2, param3, param4, param5, callBack);
        }


        public void QueueWorkItem<T1, TResult>(Func<T1, TResult> function, T1 param1, Action<ExcuteResult<TResult>> callBack = null)
        {
            Func<TResult> func = () => { return function(param1); };
            QueueWorkItem<TResult>(func, callBack);
        }

        public void QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> function, T1 param1, T2 param2, Action<ExcuteResult<TResult>> callBack = null)
        {
            Func<TResult> func = () => { return function(param1, param2); };
            QueueWorkItem<TResult>(func, callBack);
        }

        public void QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, T1 param1, T2 param2, T3 param3, Action<ExcuteResult<TResult>> callBack = null)
        {
            Func<TResult> func = () => { return function(param1, param2, param3); };
            QueueWorkItem<TResult>(func, callBack);
        }

        public void QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, Action<ExcuteResult<TResult>> callBack = null)
        {
            Func<TResult> func = () => { return function(param1, param2, param3, param4); };
            QueueWorkItem<TResult>(func, callBack);
        }

        public void QueueWorkItem<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, Action<ExcuteResult<TResult>> callBack = null)
        {
            Func<TResult> func = () => { return function(param1, param2, param3, param4, param5); };
            QueueWorkItem<TResult>(func, callBack);
        }


        public void QueueWorkItem<TResult>(Func<TResult> function, Action<ExcuteResult<TResult>> callBack = null)
        {
            Func<object[], TResult> func = (param) => { return function(); };
            QueueWorkItem<TResult>(func, new object[0], callBack);
        }


        public void QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, Action<ExcuteResult<TResult>> callBack = null)
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
                CheckAndRunThread();
                if (callBack != null)
                {
                    callBack(excuteResult);
                }
            });
            thread.Name = guid;
            waitingThreadQueue.Enqueue(thread);
            CheckAndRunThread();
        }

        private void CheckAndRunThread()
        {
            while (RunningThreadCount < threadPoolOption.MaxThreads && WaitingThreadCount > 0)
            {
                Thread thread;
                bool dequeueRes = waitingThreadQueue.TryDequeue(out thread);
                if (dequeueRes)
                {
                    runningThreadDic[thread.Name] = thread;
                    thread.Start();
                }
            }
        }

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

        public async Task WaitAsync()
        {
            await Task.Run(() =>
            {
                Wait();
            });
        }


        public void Stop()
        {
            waitingThreadQueue = new ConcurrentQueue<Thread>();
            foreach (Thread thread in runningThreadDic.Values) 
            {
                thread.Interrupt();
                thread.Join();
            }
            runningThreadDic = new ConcurrentDictionary<string, Thread>();
        }

        public async Task StopAsync()
        {
            await Task.Run(() =>
            {
                Stop();
            });
        }

        public void PauseIfRequested()
        {
            manualResetEvent.WaitOne();
        }

        public void Pause()
        {
            manualResetEvent.Reset();
        }

        public void Resume()
        {
            manualResetEvent.Set();
        }
    }
}