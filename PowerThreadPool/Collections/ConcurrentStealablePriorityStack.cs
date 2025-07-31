using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.LockFree;

namespace PowerThreadPool.Collections
{
    internal class ConcurrentStealablePriorityStack<T> : IStealablePriorityCollection<T>
    {
        private readonly ConcurrentDictionary<int, LockFreeDeque<T>> _queueDic;
        private List<int> _sortedPriorityList;
        private InterlockedFlag<CanInsertPriority> _canInsertPriority = CanInsertPriority.Allowed;

        internal ConcurrentStealablePriorityStack()
        {
            _queueDic = new ConcurrentDictionary<int, LockFreeDeque<T>>();
            _sortedPriorityList = new List<int>();
        }

        public void Set(T item, int priority)
        {
            LockFreeDeque<T> deque = _queueDic.GetOrAdd(priority, _ =>
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
                return new LockFreeDeque<T>();
            });

            deque.PushRight(item);
        }

        public T Get()
        {
            T item = default;

            for (int i = 0; i < _sortedPriorityList.Count; ++i)
            {
                int priority = _sortedPriorityList[i];
                if (_queueDic.TryGetValue(priority, out LockFreeDeque<T> deque))
                {
                    if (deque.TryPopRight(out item))
                    {
                        break;
                    }
                }
            }

            return item;
        }

        public T Steal()
        {
            T item = default;

            // 窃取时从最高优先级队列的左端获取元素
            for (int i = 0; i < _sortedPriorityList.Count; ++i)
            {
                int priority = _sortedPriorityList[i];
                if (_queueDic.TryGetValue(priority, out LockFreeDeque<T> deque))
                {
                    if (deque.TryPopLeft(out item))
                    {
                        break;
                    }
                }
            }

            return item;
        }

        public T Discard()
        {
            T item = default;

            for (int i = _sortedPriorityList.Count - 1; i >= 0; --i)
            {
                int priority = _sortedPriorityList[i];
                if (_queueDic.TryGetValue(priority, out LockFreeDeque<T> deque))
                {
                    if (deque.TryPopRight(out item))
                    {
                        break;
                    }
                }
            }
            return item;
        }
    }

    internal class LockFreeDeque<T>
    {
        private volatile Node _head;
        private volatile Node _tail;

        public LockFreeDeque()
        {
            var sentinel = new Node(default(T));
            _head = _tail = sentinel;
        }

        public void PushRight(T item)
        {
            var newNode = new Node(item);
        retry:
            using (Operator op = new Operator(_tail, newNode, null, out bool res))
            {
                if (!res || !op.Check(_tail, newNode, null))
                {
                    goto retry;
                }
                var currentTail = _tail;
                currentTail.Next = newNode;
                newNode.Prev = currentTail;
                _tail = newNode;
            }
        }

        public bool TryPopRight(out T item)
        {
            item = default;
        retry:
            using (Operator op = new Operator(_tail, _tail.Prev, null, out bool res))
            {
                if (!res || !op.Check(_tail, _tail.Prev, null))
                {
                    goto retry;
                }
                var currentTail = _tail;
                item = currentTail.Value;

                _tail = currentTail.Prev;

                if (_tail == null) // 只剩哨兵节点
                {
                    _tail = currentTail;
                    return false;
                }

                _tail.Next = null;

                return true;
            }
        }

        public bool TryPopLeft(out T item)
        {
            item = default;
        retry:
            using (Operator op = new Operator(_head, _head.Next, _head.Next?.Next, out bool res))
            {
                if (!res || !op.Check(_head, _head.Next, _head.Next?.Next))
                {
                    goto retry;
                }
                var currentHead = _head;
                var nextNode = currentHead.Next;

                if (nextNode == null)
                {
                    return false;
                }

                item = nextNode.Value;

                if (nextNode.Next != null)
                {
                    nextNode.Next.Prev = currentHead;
                    currentHead.Next = nextNode.Next;
                }
                else
                {
                    currentHead.Next = null;
                    _tail = currentHead;
                }

                return true;
            }
        }

        internal class Node
        {
            public T Value;
            public volatile Node Next;
            public volatile Node Prev;

            public InterlockedFlag<DequeState> _state = DequeState.Normal;

            public Node(T value)
            {
                Value = value;
            }
        }

        public enum DequeState
        {
            Normal,
            Updating
        }

        internal class Operator : IDisposable
        {
            private Node _node1;
            private Node _node2;
            private Node _node3;
            internal Operator(Node node1, Node node2, Node node3, out bool res)
            {
                res = true;

                int loopCount = 0;

                while (true)
                {
                    if (node1 == null || node1._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                        break;
                    }
                    ++loopCount;
                    if (loopCount == 3)
                    {
                        res = false;
                        Dispose();
                        return;
                    }
                }
                _node1 = node1;

                loopCount = 0;
                while (true)
                {
                    if (node2 == null || node2._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                        break;
                    }
                    ++loopCount;
                    if (loopCount == 3)
                    {
                        res = false;
                        Dispose();
                        return;
                    }
                }
                _node2 = node2;

                loopCount = 0;
                while (true)
                {
                    if (node3 == null || node3._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                        break;
                    }
                    ++loopCount;
                    if (loopCount == 3)
                    {
                        res = false;
                        Dispose();
                        return;
                    }
                }
                _node3 = node3;
            }

            public bool Check(Node node1, Node node2, Node node3)
            {
                if (node1 != null && (node1 != _node1 || (node1.Value != null && !node1.Value.Equals(_node1.Value))))
                {
                    return false;
                }
                if (node2 != null && (node2 != _node2 || (node2.Value != null && !node2.Value.Equals(_node2.Value))))
                {
                    return false;
                }
                if (node3 != null && (node3 != _node3 || (node3.Value != null && !node3.Value.Equals(_node3.Value))))
                {
                    return false;
                }
                return true;
            }

            public void Dispose()
            {
                if(_node1 != null)
                {
                    _node1._state.InterlockedValue = DequeState.Normal;
                }
                if (_node2 != null)
                {
                    _node2._state.InterlockedValue = DequeState.Normal;
                }
                if (_node3 != null)
                {
                    _node3._state.InterlockedValue = DequeState.Normal;
                }
            }
        }
    }
}
