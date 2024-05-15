using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PowerThreadPool.Collections
{
    internal class ConcurrentPriorityStack<T> : IConcurrentPriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentStack<T>> _queueDic;
        private readonly ConcurrentSet<int> _prioritySet;
        private List<int> _reversed;
        private volatile bool _updated;

        internal ConcurrentPriorityStack()
        {
            _queueDic = new ConcurrentDictionary<int, ConcurrentStack<T>>();
            _prioritySet = new ConcurrentSet<int>();
            _updated = false;
        }

        public void Set(T item, int priority)
        {
            ConcurrentStack<T> queue = _queueDic.GetOrAdd(priority, _ =>
            {
                _prioritySet.Add(priority);
                _updated = true;
                return new ConcurrentStack<T>();
            });

            queue.Push(item);
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
                if (_queueDic.TryGetValue(priority, out ConcurrentStack<T> queue))
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
