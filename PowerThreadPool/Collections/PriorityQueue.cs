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
        private int updated;

        public PriorityQueue()
        {
            queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
            sortedPrioritySet = new SortedSet<int>();
            updated = 0;
        }

        public void Enqueue(T item, int priority)
        {
            ConcurrentQueue<T> queue = queueDic.GetOrAdd(priority, _ =>
            {
                sortedPrioritySet.Add(priority);
                Interlocked.Exchange(ref updated, 1);
                return new ConcurrentQueue<T>();
            });

            queue.Enqueue(item);
        }

        public T Dequeue()
        {
            T item = default;

            if (Interlocked.CompareExchange(ref updated, 0, 1) == 1)
            {
                reversed = sortedPrioritySet.Reverse().ToList();
            }

            for (int i = 0; i < reversed.Count; ++i)
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
