using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PowerThreadPool.Collections
{
    public class PriorityQueue<T>
    {
        private ConcurrentDictionary<int, ConcurrentQueue<T>> queueDic;
        private ConcurrentBag<int> priorities;

        public PriorityQueue()
        {
            queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
            priorities = new ConcurrentBag<int>();
        }

        public void Enqueue(T item, int priority)
        {
            var queue = queueDic.GetOrAdd(priority, _ => new ConcurrentQueue<T>());
            queue.Enqueue(item);

            // Add priority to the bag if it's a new one
            if (queue.Count == 1)
            {
                priorities.Add(priority);
            }
        }

        public T Dequeue()
        {
            T item = default;
            var sortedPriorities = priorities.Distinct().OrderByDescending(x => x).ToList();

            foreach (var priority in sortedPriorities)
            {
                if (queueDic.TryGetValue(priority, out var queue))
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
