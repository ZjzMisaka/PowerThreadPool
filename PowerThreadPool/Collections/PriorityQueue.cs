using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerThreadPool.Collections
{
    public class PriorityQueue<T>
    {
        private ConcurrentDictionary<int, ConcurrentQueue<T>> queueDic;
        private int highestPriority;

        public PriorityQueue()
        {
            queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
            highestPriority = int.MinValue;
        }

        public void Enqueue(T item, int priority)
        {
            var queue = queueDic.GetOrAdd(priority, _ => new ConcurrentQueue<T>());
            queue.Enqueue(item);

            UpdateHighestPriority(priority);
        }

        public T Dequeue()
        {
            T item = default;
            ConcurrentQueue<T> queue;
            if (queueDic.TryGetValue(highestPriority, out queue))
            {
                if (queue.TryDequeue(out item))
                {
                    return item;
                }
            }

            List<int> sortedPriorities = queueDic.Keys.OrderByDescending(x => x).ToList();

            foreach (int priority in sortedPriorities)
            {
                if (queueDic.TryGetValue(priority, out queue))
                {
                    if (queue.TryDequeue(out item))
                    {
                        UpdateHighestPriority(priority);
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
