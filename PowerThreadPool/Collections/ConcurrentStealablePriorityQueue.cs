using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
#if NET45_OR_GREATER
using ConcurrentCollection = NonBlocking;
#else
using ConcurrentCollection = System.Collections.Concurrent;
#endif

namespace PowerThreadPool.Collections
{
    internal class ConcurrentStealablePriorityQueue<T> : IStealablePriorityCollection<T>
    {
        private readonly ConcurrentCollection.ConcurrentDictionary<int, ConcurrentQueue<T>> _queueDic;
        private readonly ConcurrentSet<int> _prioritySet;
        private List<int> _reversed;
        private volatile bool _updated;

        internal ConcurrentStealablePriorityQueue()
        {
            _queueDic = new ConcurrentCollection.ConcurrentDictionary<int, ConcurrentQueue<T>>();
            _prioritySet = new ConcurrentSet<int>();
            _updated = false;
        }

        public void Set(T item, int priority)
        {
            ConcurrentQueue<T> queue = _queueDic.GetOrAdd(priority, _ =>
            {
                _prioritySet.Add(priority);
                _updated = true;
                return new ConcurrentQueue<T>();
            });

            queue.Enqueue(item);
        }

        public T Get()
        {
            T item = default;

            if (_updated)
            {
                _updated = false;
                _reversed = _prioritySet.OrderByDescending(x => x).ToList();
            }

            for (int i = 0; i < _reversed.Count; ++i)
            {
                int priority = _reversed[i];
                if (_queueDic.TryGetValue(priority, out ConcurrentQueue<T> queue))
                {
                    if (queue.TryDequeue(out item))
                    {
                        break;
                    }
                }
            }

            return item;
        }

        public T Steal() => Get();
    }
}
