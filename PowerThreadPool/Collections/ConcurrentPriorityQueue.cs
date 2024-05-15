using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PowerThreadPool.Constants;

namespace PowerThreadPool.Collections
{
    internal class ConcurrentPriorityQueue<T> : IConcurrentPriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<T>> _queueDic;
        private readonly ConcurrentSet<int> _prioritySet;
        private List<int> _reversed;
        private volatile bool _updated;

        internal ConcurrentPriorityQueue()
        {
            _queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
            _prioritySet = new ConcurrentSet<int>();
            _updated = false;
        }

        public void Set(T item, int priority)
        {
            ConcurrentQueue<T> queue = _queueDic.GetOrAdd(priority, _ =>
            {
                _prioritySet.Add(priority);
                //Interlocked.Exchange(ref _updated, UpdatedFlags.Updated);
                _updated = true;
                return new ConcurrentQueue<T>();
            });

            queue.Enqueue(item);
        }

        public T Get()
        {
            T item = default;

            //if (Interlocked.CompareExchange(ref _updated, UpdatedFlags.NotUpdated, UpdatedFlags.Updated) == UpdatedFlags.Updated)
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
    }
}
