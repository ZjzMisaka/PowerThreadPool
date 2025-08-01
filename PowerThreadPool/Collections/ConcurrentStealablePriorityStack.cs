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
        private readonly ConcurrentDictionary<int, ConcurrentDeque<T>> _queueDic;
        private List<int> _sortedPriorityList;
        private InterlockedFlag<CanInsertPriority> _canInsertPriority = CanInsertPriority.Allowed;

        internal ConcurrentStealablePriorityStack()
        {
            _queueDic = new ConcurrentDictionary<int, ConcurrentDeque<T>>();
            _sortedPriorityList = new List<int>();
        }

        public void Set(T item, int priority)
        {
            ConcurrentDeque<T> deque = _queueDic.GetOrAdd(priority, _ =>
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
                return new ConcurrentDeque<T>();
            });

            deque.PushRight(item);
        }

        public T Get()
        {
            T item = default;

            for (int i = 0; i < _sortedPriorityList.Count; ++i)
            {
                int priority = _sortedPriorityList[i];
                if (_queueDic.TryGetValue(priority, out ConcurrentDeque<T> deque))
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
                if (_queueDic.TryGetValue(priority, out ConcurrentDeque<T> deque))
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
                if (_queueDic.TryGetValue(priority, out ConcurrentDeque<T> deque))
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

    /// <summary>
    /// 分治任务的工作窃取算法中, 入队与出队这两个高频操作使用的是同一端, 所以虽然这个实现将自旋的粒度缩小到了节点级别, 但是实际上依旧会高频争用最右侧的同一个节点.
    /// 而窃取者线程虽然不会争用_tail节点, 可以实现左右两侧并发执行, 但窃取行为本身并不会高频发生.
    /// 所以这种设计意义不大, 不过作为通用双端队列 (FIFO) 可能有可取之处 (待验证).
    ///
    /// 根据Benchmark结果, 分散的节点导致内存占用以及GC压力变大, 应该改用循环数组的实现
    /// 并且减少使用自旋, 而是使用数组两端节点下标变量的Interlocked.Increment与Interlocked.Decrement来进行出队以及窃取.
    /// 由于这个双端队列存在通用用途, 因此不可默认只会有一个线程操作尾部进行入队与出队, 所以在操作尾部节点时可能不可避免使用自旋, 难以做到对分治任务的极限优化. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ConcurrentDeque<T>
    {
        private volatile Node _head;
        private volatile Node _tail;

        public ConcurrentDeque()
        {
            var sentinel = new Node(default(T));
            _head = _tail = sentinel;
        }

        public void PushRight(T item)
        {
            var newNode = new Node(item);
        retry:
            using (Operator op = new Operator(_tail, newNode, null))
            {
                if (!op.Check(_tail, newNode, null))
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

            if (_tail == _head)
            {
                return false;
            }

        retry:
            using (Operator op = new Operator(_tail, _tail.Prev, null))
            {
                if (!op.Check(_tail, _tail.Prev, null))
                {
                    goto retry;
                }
                var currentTail = _tail;
                item = currentTail.Value;

                _tail = currentTail.Prev;

                _tail.Next = null;

                return true;
            }
        }

        public bool TryPopLeft(out T item)
        {
            item = default;
        retry:
            using (Operator op = new Operator(_head, _head.Next, _head.Next?.Next))
            {
                if (!op.Check(_head, _head.Next, _head.Next?.Next))
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
            private static long s_id = 0;

            public long _id;
            public T Value;
            public volatile Node Next;
            public volatile Node Prev;

            public InterlockedFlag<DequeState> _state = DequeState.Normal;

            public Node(T value)
            {
                Value = value;
                _id = Interlocked.Increment(ref s_id);
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
            internal Operator(Node node1, Node node2, Node node3)
            {
                _node1 = node1;
                _node2 = node2;
                _node3 = node3;

                var nodesToLock = new List<Node>();
                if (node1 != null)
                {
                    nodesToLock.Add(node1);
                }
                if (node2 != null)
                {
                    nodesToLock.Add(node2);
                }
                if (node3 != null)
                {
                    nodesToLock.Add(node3);
                }

                nodesToLock.Sort((a, b) => a._id.CompareTo(b._id));

                foreach (var node in nodesToLock)
                {
                    while (!node._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                    }
                }
            }

            public bool Check(Node node1, Node node2, Node node3)
            {
                if (node1 != null && (node1 != _node1 || (node1._id != _node1._id)))
                {
                    return false;
                }
                if (node2 != null && (node2 != _node2 || (node2._id != _node2._id)))
                {
                    return false;
                }
                if (node3 != null && (node3 != _node3 || (node3._id != _node3._id)))
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
