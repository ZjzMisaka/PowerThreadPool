using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerThreadPool.Collections
{
    public class PriorityQueue<T>
    {
        private SortedDictionary<int, ConcurrentQueue<T>> queueDic;

        internal AutoResetEvent assignSignal = new AutoResetEvent(true);

        object lockObj = new object();

        public PriorityQueue()
        {
            queueDic = new SortedDictionary<int, ConcurrentQueue<T>>();
        }

        public void Enqueue(T item, int priority)
        {
            lock (lockObj)
            {
                if (queueDic.ContainsKey(priority))
                {
                    queueDic[priority].Enqueue(item);
                }
                else
                {
                    var queue = new ConcurrentQueue<T>();
                    queue.Enqueue(item);
                    queueDic.Add(priority, queue);
                }
            }
        }

        public List<T> Steal(AutoResetEvent stealSignal, int count)
        {
            var stolenItems = new List<T>();

            if (queueDic.Count == 0)
            {
                return stolenItems;
            }

            assignSignal.WaitOne();
            assignSignal.Set();

            stealSignal.Reset();

            lock (lockObj)
            {
                for (int i = 0; i < count; i++)
                {
                    if (queueDic.Count <= 0)
                    {
                        break;
                    }

                    var pair = queueDic.Last();

                    if (pair.Value.TryDequeue(out T item))
                    {
                        stolenItems.Add(item);
                    }

                    if (!pair.Value.Any())
                    {
                        queueDic.Remove(pair.Key);
                    }
                }
            }
           
            stealSignal.Set();

            return stolenItems;
        }

        public T Dequeue()
        {
            lock (lockObj)
            {
                if (queueDic.Count <= 0)
                {
                    return default;
                }

                var pair = queueDic.Last();

                pair.Value.TryDequeue(out T item);

                if (!pair.Value.Any())
                {
                    queueDic.Remove(pair.Key);
                }

                return item;
            }
        }

        public int Count
        {
            get
            {
                return queueDic.Sum(p => p.Value.Count);
            }
        }
    }
}
