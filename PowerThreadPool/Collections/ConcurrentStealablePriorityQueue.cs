using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers;

namespace PowerThreadPool.Collections
{
    internal class ConcurrentStealablePriorityQueue<T> : IStealablePriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<T>> _queueDic;
        private readonly ConcurrentSet<int> _prioritySet;
        private List<int> _reversed;
        private InterlockedFlag<CanInsertPriority> _canInsertPriority = CanInsertPriority.Allowed;

        internal ConcurrentStealablePriorityQueue()
        {
            _queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
            _prioritySet = new ConcurrentSet<int>();
            _reversed = new List<int>();
        }

        public void Set(T item, int priority)
        {
            ConcurrentQueue<T> queue = _queueDic.GetOrAdd(priority, _ =>
            {
                _prioritySet.Add(priority);
                SpinWait.SpinUntil(() => _canInsertPriority.TrySet(CanInsertPriority.NotAllowed, CanInsertPriority.Allowed));
                bool inserted = false;
                for (int i = 0; i < _reversed.Count; ++i)
                {
                    int p = _reversed[i];
                    if (priority >= p)
                    {
                        _reversed.Insert(i, priority);
                        inserted = true;
                        break;
                    }
                }
                if (!inserted)
                {
                    _reversed.Add(priority);
                }
                _canInsertPriority = CanInsertPriority.Allowed;
                return new ConcurrentQueue<T>();
            });

            queue.Enqueue(item);
        }

        public T Get()
        {
            T item = default;

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
