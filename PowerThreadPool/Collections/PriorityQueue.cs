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
        private int highestPriority;

        public PriorityQueue()
        {
            queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
            sortedPrioritySet = new SortedSet<int>();
            highestPriority = int.MinValue;
        }

        public void Enqueue(T item, int priority)
        {
            var queue = queueDic.GetOrAdd(priority, _ => new ConcurrentQueue<T>());
            queue.Enqueue(item);
            sortedPrioritySet.Add(priority);
            if (priority > highestPriority)
            {
                UpdateHighestPriority(priority);
            }
        }

        public T Dequeue()
        {
            T item = default;
            ConcurrentQueue<T> queue;

            SortedSet<int> snapshot = new SortedSet<int>(sortedPrioritySet);
            IEnumerable<int> reversed = sortedPrioritySet.Reverse();
            foreach (int priority in reversed)
            {
                if (priority <= highestPriority && queueDic.TryGetValue(priority, out queue))
                {
                    if (queue.TryDequeue(out item))
                    {
                        if (priority > highestPriority)
                        {
                            UpdateHighestPriority(priority);
                        }
                        break;
                    }
                }
            }

            return item;
        }

        private void UpdateHighestPriority(int priority)
        {
            bool retry = true;
            while (retry)
            {
                int highestPriorityTemp = highestPriority;
                if (priority > highestPriorityTemp)
                {
                    if (Interlocked.CompareExchange(ref highestPriority, priority, highestPriorityTemp) == highestPriorityTemp)
                    {
                        retry = false;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
