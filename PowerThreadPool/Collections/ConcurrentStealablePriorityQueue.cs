﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.LockFree;

namespace PowerThreadPool.Collections
{
    internal class ConcurrentStealablePriorityQueue<T> : IStealablePriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<T>> _queueDic;
        private List<int> _sortedPriorityList;
        private InterlockedFlag<CanInsertPriority> _canInsertPriority = CanInsertPriority.Allowed;

        internal ConcurrentStealablePriorityQueue()
        {
            _queueDic = new ConcurrentDictionary<int, ConcurrentQueue<T>>();
            _sortedPriorityList = new List<int>();
        }

        public void Set(T item, int priority)
        {
            ConcurrentQueue<T> queue = _queueDic.GetOrAdd(priority, _ =>
            {
#if DEBUG
                Spinner.Start(() => _canInsertPriority.TrySet(CanInsertPriority.NotAllowed, CanInsertPriority.Allowed));
#else
                while (true)
                {
                    if (_canInsertPriority.TrySet(CanInsertPriority.NotAllowed, CanInsertPriority.Allowed))
                    {
                        break;
                    }
                }
#endif
                bool inserted = false;
                for (int i = 0; i < _sortedPriorityList.Count; ++i)
                {
                    int p = _sortedPriorityList[i];
                    if (priority > p)
                    {
                        _sortedPriorityList.Insert(i, priority);
                        inserted = true;
                        break;
                    }
                }
                if (!inserted)
                {
                    _sortedPriorityList.Add(priority);
                }
                _canInsertPriority = CanInsertPriority.Allowed;
                return new ConcurrentQueue<T>();
            });

            queue.Enqueue(item);
        }

        public T Get()
        {
            T item = default;

            for (int i = 0; i < _sortedPriorityList.Count; ++i)
            {
                int priority = _sortedPriorityList[i];
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

        public T Discard()
        {
            T item = default;

            for (int i = _sortedPriorityList.Count - 1; i >= 0; --i)
            {
                int priority = _sortedPriorityList[i];
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
