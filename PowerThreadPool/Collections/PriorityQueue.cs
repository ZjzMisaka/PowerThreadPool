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

        public PriorityQueue()
        {
            queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
            sortedPrioritySet = new SortedSet<int>();
        }

        public void Enqueue(T item, int priority)
        {
            var queue = queueDic.GetOrAdd(priority, _ => new ConcurrentQueue<T>());
            queue.Enqueue(item);
            sortedPrioritySet.Add(priority);
        }

        public T Dequeue()
        {
            T item = default;
            ConcurrentQueue<T> queue;

            SortedSet<int> snapshot = new SortedSet<int>(sortedPrioritySet);
            IEnumerable<int> reversed = snapshot.Reverse();
            foreach (int priority in reversed)
            {
                if (queueDic.TryGetValue(priority, out queue))
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
