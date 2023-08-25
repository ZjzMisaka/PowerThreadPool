using System;
using System.Collections.Concurrent;

namespace PowerThreadPool
{
    public class PowerPool
    {
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

        public void StopAllThread()
        { 
            foreach (Thread thread in runningThreadDic.Values) 
            {
                thread.Interrupt();
                thread.Join();
            }
        }

        public async void StopAllThreadAsync()
        {
            await Task.Run(() => StopAllThread());
        }
    }
}