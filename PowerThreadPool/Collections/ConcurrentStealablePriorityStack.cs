using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool.Helpers;
namespace PowerThreadPool.Collections
{
    internal class ConcurrentStealablePriorityStack<T> : IStealablePriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentStack<T>> _queueDic
            = new ConcurrentDictionary<int, ConcurrentStack<T>>();

        internal volatile List<int> _sortedPriorityList = new List<int>();

        // Dedicated queue for zero-priority items to optimize access without dictionary lookup.
        private readonly ConcurrentStack<T> _zeroStack = new ConcurrentStack<T>();

        public ConcurrentStealablePriorityStack()
        {
            _sortedPriorityList.Add(0);
        }

        private bool TryGetStack(int priority, out ConcurrentStack<T> stack)
        {
            if (priority == 0)
            {
                stack = _zeroStack;
                return true;
            }
            else
            {
                return _queueDic.TryGetValue(priority, out stack);
            }
        }

        public void Set(T item, int priority)
        {
            if (priority == 0)
            {
                _zeroStack.Push(item);
                return;
            }

            ConcurrentStack<T> stack = _queueDic.GetOrAdd(priority, _ => new ConcurrentStack<T>());

            while (true)
            {
                List<int> oldList = _sortedPriorityList;

                if (oldList.Contains(priority))
                {
                    break;
                }

                List<int> newList = null;

                lock (this)
                {
                    newList = ConcurrentStealablePriorityCollectionHelper.InsertPriorityDescending(oldList, priority);
                }

                List<int> orig = Interlocked.CompareExchange(ref _sortedPriorityList, newList, oldList);

                if (ReferenceEquals(orig, oldList))
                {
                    break;
                }
            }

            stack.Push(item);
        }

        public T Get()
        {
            T item = default;

            List<int> priorities = _sortedPriorityList;

            if (priorities.Count == 1)
            {
                _zeroStack.TryPop(out item);
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
                _zeroStack.TryPop(out item);
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
            return TryGetStack(priority, out ConcurrentStack<T> s) && s.TryPop(out item);
        }
    }
}
