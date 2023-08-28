using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerThreadPool
{
    public class PriorityQueue<T>
    {
        private readonly object syncLock = new object();
        private SortedDictionary<int, ConcurrentQueue<T>> queueDic;

        public PriorityQueue()
        {
            this.queueDic = new SortedDictionary<int, ConcurrentQueue<T>>();
        }

        public void Enqueue(T item, int priority)
        {
            lock (syncLock)
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

        public T Dequeue()
        {
            lock (syncLock)
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
        }

        public int Count
        {
            get
            {
                lock (syncLock)
                {
                    return queueDic.Sum(p => p.Value.Count);
                }
            }
        }
    }
}
