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
    }
}
