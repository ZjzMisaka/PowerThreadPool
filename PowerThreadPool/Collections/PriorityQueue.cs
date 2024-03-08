using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerThreadPool.Collections
{
    public class PriorityQueue<T>
    {
        private ConcurrentDictionary<int, ConcurrentQueue<T>> queueDic;
        private SortedSet<int> sortedPrioritySet;
        private List<int> reversed;

        public PriorityQueue()
        {
            queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
            sortedPrioritySet = new SortedSet<int>();
        }

        public void Enqueue(T item, int priority)
        {
            ConcurrentQueue<T> queue = queueDic.GetOrAdd(priority, _ =>
            {
                sortedPrioritySet.Add(priority);
                reversed = sortedPrioritySet.Reverse().ToList();
                return new ConcurrentQueue<T>();
            });

            queue.Enqueue(item);
        }

        public T Dequeue()
        {
            T item = default;

            int reversedCount = reversed.Count;
            for (int i = 0; i < reversedCount; ++i)
            {
                int priority = reversed[i];
                if (queueDic.TryGetValue(priority, out ConcurrentQueue<T> queue))
                {
                    if (queue.TryDequeue(out item))
                    {
                        break;
                    }
                }
            }

            return item;
        }
    }
}
