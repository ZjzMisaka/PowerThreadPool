using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.LockFree;

namespace PowerThreadPool.Collections
{
    internal class ConcurrentStealablePriorityStack<T> : IStealablePriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, ConcurrentStack<T>> _queueDic
            = new ConcurrentDictionary<int, ConcurrentStack<T>>();

        private volatile List<int> _sortedPriorityList = new List<int>();

        private InterlockedFlag<CanInsertPriority> _canInsertPriority = CanInsertPriority.Allowed;

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

            ConcurrentStack<T> stack = _queueDic.GetOrAdd(priority, _ =>
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

                    return new ConcurrentStack<T>();
                }
                finally
                {
                    _canInsertPriority.InterlockedValue = CanInsertPriority.Allowed;
                }
            });

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
