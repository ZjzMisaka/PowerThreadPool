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

        public PriorityQueue()
        {
            queueDic = new SortedDictionary<int, ConcurrentQueue<T>>();
        }

        public void Enqueue(T item, int priority)
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

        public T Steal(AutoResetEvent stealSignal, int count)
        {
            if (queueDic.Count == 0)
            {
                return default;
            }

            assignSignal.WaitOne();

            stealSignal.Reset();

            var pair = queueDic.Last();

            pair.Value.TryDequeue(out T item);

            if (!pair.Value.Any())
            {
                queueDic.Remove(pair.Key);
            }

            stealSignal.Set();

            return item;
        }

        public T Dequeue()
        {
            if (queueDic.Count == 0)
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

        public int Count
        {
            get
            {
                return queueDic.Sum(p => p.Value.Count);
            }
        }
    }
}
