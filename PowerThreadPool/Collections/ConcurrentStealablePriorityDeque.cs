using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool.Helpers;

// Copyright and License
// ChaseLevDeque adapts ideas/code from tejacques/Deque (MIT License):
//   https://github.com/tejacques/Deque
//
// Acknowledgment
// We gratefully acknowledge tejacques/Deque as an excellent foundation
// for ChaseLevDeque.
//
// About ChaseLevDeque:
//
// A Chase–Lev (ABP) work-stealing deque for single-owner (producer/consumer at bottom)
// and multiple thieves (steal from top).
//
// Derived from https://github.com/tejacques/Deque and adapted as follows:
// - Replaced list semantics with Chase–Lev protocol (Top/Bottom indices).
// - Added atomic operations: volatile reads/writes and CAS on Top.
// - Implemented owner-only PushBottom/TryPopBottom and concurrent TrySteal.
// - Used power-of-two circular buffer with on-demand growth.
// - Exposed advisory ApproximateCount/IsEmpty (non-linearizable).
namespace PowerThreadPool.Collections
{
    internal class ConcurrentStealablePriorityDeque<T> : IStealablePriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, ChaseLevDeque<T>> _queueDic
            = new ConcurrentDictionary<int, ChaseLevDeque<T>>();

        internal volatile List<int> _sortedPriorityList = new List<int>();

        // Dedicated queue for zero-priority items to optimize access without dictionary lookup.
        private readonly ChaseLevDeque<T> _zeroQueue = new ChaseLevDeque<T>();

        public ConcurrentStealablePriorityDeque()
        {
            _sortedPriorityList.Add(0);
        }

        private bool TryGetQueue(int priority, out ChaseLevDeque<T> queue)
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
                _zeroQueue.PushBottom(item);
                return;
            }

            ChaseLevDeque<T> queue = _queueDic.GetOrAdd(priority, _ => new ChaseLevDeque<T>());

            while (true)
            {
                List<int> oldList = _sortedPriorityList;

                if (oldList.Contains(priority))
                {
                    break;
                }

                List<int> newList = InsertPriorityDescending(oldList, priority);

                List<int> orig = Interlocked.CompareExchange(ref _sortedPriorityList, newList, oldList);

                if (ReferenceEquals(orig, oldList))
                {
                    break;
                }
            }

            queue.PushBottom(item);
        }

        public T Get()
        {
            T item = default;

            List<int> priorities = _sortedPriorityList;

            if (priorities.Count == 1)
            {
                _zeroQueue.TryPopBottom(out item);
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

        public T Steal()
        {
            T item = default;

            List<int> priorities = _sortedPriorityList;

            if (priorities.Count == 1)
            {
                _zeroQueue.TrySteal(out item);
                return item;
            }

            for (int i = 0; i < priorities.Count; ++i)
            {
                int pr = priorities[i];
                if (TryStealItem(pr, out item))
                {
                    break;
                }
            }
            return item;
        }

        public T Discard()
        {
            T item = default;

            List<int> priorities = _sortedPriorityList;

            if (priorities.Count == 1)
            {
                _zeroQueue.TryPopBottom(out item);
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
            return TryGetQueue(priority, out ChaseLevDeque<T> q) && q.TryPopBottom(out item);
        }

        private bool TryStealItem(int priority, out T item)
        {
            item = default;
            return TryGetQueue(priority, out ChaseLevDeque<T> q) && q.TrySteal(out item);
        }

        private List<int> InsertPriorityDescending(List<int> oldList, int priority)
        {
            var newList = new List<int>(oldList.Count + 1);
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
            return newList;
        }
    }
}
