using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PowerThreadPool.Collections
{
    public class PriorityQueue<T>
    {
        private ConcurrentDictionary<int, ConcurrentQueue<T>> queueDic;

        public PriorityQueue()
        {
            queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
        }

        public void Enqueue(T item, int priority)
        {
            queueDic.AddOrUpdate(priority, (key) => { ConcurrentQueue<T> queue = new ConcurrentQueue<T>(); queue.Enqueue(item); return queue; }, (key, oldValue) => { oldValue.Enqueue(item); return oldValue; });
        }

        public T Dequeue()
        {
            if (queueDic.Count <= 0)
            {
                return default;
            }

            int highestPriority = queueDic.Keys.Max();
            ConcurrentQueue<T> queue = queueDic[highestPriority];
            queue.TryDequeue(out T item);

            if (!queue.Any())
            {
                queueDic.TryRemove(highestPriority, out _);
            }

            return item;
        }
    }
}
