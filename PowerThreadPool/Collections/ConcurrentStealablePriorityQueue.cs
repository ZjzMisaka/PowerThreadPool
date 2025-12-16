using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool.Helpers;
namespace PowerThreadPool.Collections
{
    internal class ConcurrentStealablePriorityQueue<T> : IStealablePriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<T>> _queueDic
            = new ConcurrentDictionary<int, ConcurrentQueue<T>>();

        internal volatile List<int> _sortedPriorityList = new List<int>();

        // Dedicated queue for zero-priority items to optimize access without dictionary lookup.
        private readonly ConcurrentQueue<T> _zeroQueue = new ConcurrentQueue<T>();

        public bool EnforceDequeOwnership { get; }

        public ConcurrentStealablePriorityQueue(bool enforceDequeOwnership)
        {
            _sortedPriorityList.Add(0);
            EnforceDequeOwnership = enforceDequeOwnership;
        }

        private bool TryGetQueue(int priority, out ConcurrentQueue<T> queue)
        {
            if (priority == 0)
            {
                queue = _zeroQueue;
                return true;
            }
            else
            {
                return _queueDic.TryGetValue(priority, out queue);
            }
        }

        public void Set(T item, int priority)
        {
            if (priority == 0)
            {
                _zeroQueue.Enqueue(item);
                return;
            }

            ConcurrentQueue<T> queue = _queueDic.GetOrAdd(priority, _ => new ConcurrentQueue<T>());

            while (true)
            {
                List<int> oldList = _sortedPriorityList;

                if (oldList.BinarySearch(priority) >= 0)
                {
                    break;
                }

                List<int> newList = ConcurrentStealablePriorityCollectionHelper.InsertPriorityDescending(oldList, priority);

                List<int> orig = Interlocked.CompareExchange(ref _sortedPriorityList, newList, oldList);

                if (ReferenceEquals(orig, oldList))
                {
                    break;
                }
            }

            queue.Enqueue(item);
        }

        public T Get()
        {
            T item = default;

            List<int> priorities = _sortedPriorityList;

            if (priorities.Count == 1)
            {
                _zeroQueue.TryDequeue(out item);
                return item;
            }

            for (int i = 0; i < priorities.Count; ++i)
            {
                int pr = priorities[i];
                if (TryGetItem(pr, out item))
                {
                    break;
                }
            }
            return item;
        }

        public T Steal() => Get();

        public T Discard()
        {
            T item = default;

            List<int> priorities = _sortedPriorityList;

            if (priorities.Count == 1)
            {
                _zeroQueue.TryDequeue(out item);
                return item;
            }

            for (int i = priorities.Count - 1; i >= 0; --i)
            {
                int pr = priorities[i];
                if (TryGetItem(pr, out item))
                {
                    break;
                }
            }
            return item;
        }

        private bool TryGetItem(int priority, out T item)
        {
            item = default;
            return TryGetQueue(priority, out ConcurrentQueue<T> q) && q.TryDequeue(out item);
        }
    }
}
