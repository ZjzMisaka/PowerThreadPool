using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.LockFree;

namespace PowerThreadPool.Collections
{
    internal class ConcurrentStealablePriorityQueue<T> : IStealablePriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<T>> _queueDic
            = new ConcurrentDictionary<int, ConcurrentQueue<T>>();

        private volatile List<int> _sortedPriorityList = new List<int>();

        private InterlockedFlag<CanInsertPriority> _canInsertPriority = CanInsertPriority.Allowed;

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
                    Thread.Yield();
                }
#endif
                try
                {
                    List<int> oldList = _sortedPriorityList;
                    List<int> newList = new List<int>(oldList.Count + 1);

                    bool inserted = false;
                    for (int i = 0; i < oldList.Count; ++i)
                    {
                        int p = oldList[i];
                        if (!inserted && priority > p)
                        {
                            newList.Add(priority);
                            inserted = true;
                        }
                        newList.Add(p);
                    }
                    if (!inserted)
                    {
                        newList.Add(priority);
                    }

                    Interlocked.Exchange(ref _sortedPriorityList, newList);

                    return new ConcurrentQueue<T>();
                }
                finally
                {
                    _canInsertPriority.InterlockedValue = CanInsertPriority.Allowed;
                }
            });

            queue.Enqueue(item);
        }

        public T Get()
        {
            T item = default;

            List<int> priorities = _sortedPriorityList;
            for (int i = 0; i < priorities.Count; ++i)
            {
                int pr = priorities[i];
                if (_queueDic.TryGetValue(pr, out ConcurrentQueue<T> q))
                {
                    if (q.TryDequeue(out item))
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

            List<int> priorities = _sortedPriorityList;
            for (int i = priorities.Count - 1; i >= 0; --i)
            {
                int pr = priorities[i];
                if (_queueDic.TryGetValue(pr, out ConcurrentQueue<T> q))
                {
                    if (q.TryDequeue(out item))
                    {
                        break;
                    }
                }
            }
            return item;
        }
    }
}
