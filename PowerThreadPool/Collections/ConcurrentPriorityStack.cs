using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerThreadPool.Collections
{
    internal class ConcurrentPriorityStack<T> : IConcurrentPriorityCollection<T>
    {
        private ConcurrentDictionary<int, ConcurrentStack<T>> queueDic;
        private ConcurrentSet<int> prioritySet;
        private List<int> reversed;
        private int updated;

        internal ConcurrentPriorityStack()
        {
            queueDic = new ConcurrentDictionary<int, ConcurrentStack<T>>();
            prioritySet = new ConcurrentSet<int>();
            updated = 0;
        }

        public void Set(T item, int priority)
        {
            ConcurrentStack<T> queue = queueDic.GetOrAdd(priority, _ =>
            {
                prioritySet.Add(priority);
                Interlocked.Exchange(ref updated, 1);
                return new ConcurrentStack<T>();
            });

            queue.Push(item);
        }

        public T Get()
        {
            T item = default;

            if (Interlocked.CompareExchange(ref updated, 0, 1) == 1)
            {
                reversed = prioritySet.ToList();
                reversed.Sort();
                reversed.Reverse();
            }

            for (int i = 0; i < reversed.Count; ++i)
            {
                int priority = reversed[i];
                if (queueDic.TryGetValue(priority, out ConcurrentStack<T> queue))
                {
                    if (queue.TryPop(out item))
                    {
                        break;
                    }
                }
            }

            return item;
        }
    }
}
