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

    internal class LockFreeDeque4<T>
    {
        private volatile Node _head;
        private volatile Node _tail;

        public LockFreeDeque4()
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
                // 在持有状态锁的情况下不断尝试找到有效的tail
                Node currentTail;
                while (true)
                {
                    currentTail = _tail;

                    // 如果当前tail没有被标记删除，可以使用
                    if (!currentTail.IsMarkedForDeletion)
                    {
                        break;
                    }

                    // 如果被标记删除了，帮助完成删除操作
                    // 向前移动tail指针到前一个节点
                    var prevNode = currentTail.Prev;
                    if (prevNode != null)
                    {
                        // 尝试更新_tail，如果失败说明其他线程已经更新了
                        Interlocked.CompareExchange(ref _tail, prevNode, currentTail);
                    }

                    // 小延迟，让其他线程有机会完成操作
                    Thread.Yield();
                }

                // 现在currentTail是一个有效的未删除节点
                currentTail.Next = newNode;
                newNode.Prev = currentTail;
                _tail = newNode;
            }
        }

        public bool TryPopRight(out T item)
        {
            item = default;

            while (true)
            {
            retry:
                using (Operator op = new Operator(_tail, _tail.Prev, null))
                {
                    if (!op.Check(_tail, _tail.Prev, null))
                    {
                        goto retry;
                    }
                    var currentTail = _tail;

                    // 跳过哨兵节点
                    if (currentTail == _head)
                    {
                        return false;
                    }

                    // 跳过已经被标记删除的节点
                    if (currentTail.IsMarkedForDeletion)
                    {
                        Thread.Yield();
                        continue;
                    }

                    // 尝试标记节点为已删除
                    if (!currentTail.TryMarkForDeletion())
                    {
                        // 节点已被其他线程标记删除
                        Thread.Yield();
                        continue;
                    }

                    // 节点已成功标记，可以安全读取值
                    item = currentTail.Value;

                    // 物理删除节点 - 先更新_tail，再断开链接
                    var prevNode = currentTail.Prev;
                    if (prevNode != null)
                    {
                        _tail = prevNode;
                        prevNode.Next = null;
                        currentTail.Prev = null; // 帮助GC
                    }

                    return true;
                }

                Thread.Yield();
            }
        }

        public bool TryPopLeft(out T item)
        {
            item = default;

            while (true)
            {
            retry:
                using (Operator op = new Operator(_head, _head.Next, _head.Next?.Next))
                {
                    if (!op.Check(_head, _head.Next, _head.Next?.Next))
                    {
                        goto retry;
                    }
                    Node nextNode;
                    Node currentHead;

                    // 在持有状态锁的情况下找到第一个有效节点
                    while (true)
                    {
                        currentHead = _head;
                        nextNode = currentHead.Next;

                        // 队列为空
                        if (nextNode == null)
                        {
                            return false;
                        }

                        // 如果节点未被标记删除，可以使用
                        if (!nextNode.IsMarkedForDeletion)
                        {
                            break;
                        }

                        // 如果被标记删除，帮助完成删除
                        if (nextNode.Next != null)
                        {
                            currentHead.Next = nextNode.Next;
                            nextNode.Next.Prev = currentHead;
                        }
                        else
                        {
                            currentHead.Next = null;
                            if (_tail == nextNode)
                            {
                                Interlocked.CompareExchange(ref _tail, currentHead, nextNode);
                            }
                        }

                        Thread.Yield();
                    }

                    // 尝试标记节点为已删除
                    if (!nextNode.TryMarkForDeletion())
                    {
                        // 罕见情况：在检查和标记之间，节点被其他线程标记了
                        Thread.Yield();
                        continue;
                    }

                    // 节点已成功标记，可以安全读取值
                    item = nextNode.Value;

                    // 物理删除节点
                    if (nextNode.Next != null)
                    {
                        nextNode.Next.Prev = currentHead;
                        currentHead.Next = nextNode.Next;
                    }
                    else
                    {
                        // 这是最后一个元素
                        currentHead.Next = null;
                        if (_tail == nextNode)
                        {
                            _tail = currentHead;
                        }
                    }

                    return true;
                }
            }
        }

        internal class Node
        {
            public T Value;
            public volatile Node Next;
            public volatile Node Prev;
            private int _markedForDeletion = 0;
            public InterlockedFlag<DequeState> _state = DequeState.Normal;

            public Node(T value)
            {
                Value = value;
            }

            public bool IsMarkedForDeletion => _markedForDeletion == 1;

            public bool TryMarkForDeletion()
            {
                return Interlocked.CompareExchange(ref _markedForDeletion, 1, 0) == 0;
            }
        }

        internal enum DequeState
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
                while (true)
                {
                    if (_node1 == null || _node1._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                        break;
                    }
                }
                while (true)
                {
                    if (_node2 == null || _node2._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                        break;
                    }
                }
                while (true)
                {
                    if (_node3 == null || _node3._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                        break;
                    }
                }
            }

            public bool Check(Node node1, Node node2, Node node3)
            {
                if (node1 != null && node1 != _node1)
                {
                    return false;
                }
                if (node2 != null && node2 != _node2)
                {
                    return false;
                }
                if (node3 != null && node3 != _node3)
                {
                    return false;
                }
                return true;
            }

            public void Dispose()
            {
                if (_node1 != null)
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
            using (Operator op = new Operator(_head, _head.Next, _head.Next?.Next))
            {
                if (!op.Check(_head, _head.Next, _head.Next?.Next))
                {
                    goto retry;
                }
                var currentHead = _head;
                var nextNode = currentHead.Next;

                if (nextNode == null) // 队列为空
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
                    // 这是最后一个元素
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
            internal Operator(Node node1, Node node2, Node node3)
            {
                _node1 = node1;
                _node2 = node2;
                _node3 = node3;
                while (true)
                {
                    if (_node1 == null || _node1._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                        break;
                    }
                }
                while (true)
                {
                    if (_node2 == null || _node2._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                        break;
                    }
                }
                while (true)
                {
                    if (_node3 == null || _node3._state.TrySet(DequeState.Updating, DequeState.Normal))
                    {
                        break;
                    }
                }
            }

            public bool Check(Node node1, Node node2, Node node3)
            {
                if (node1 != null && node1 != _node1)
                {
                    return false;
                }
                if (node2 != null && node2 != _node2)
                {
                    return false;
                }
                if (node3 != null && node3 != _node3)
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


    internal class LockFreeDeque2<T>
    {
        private volatile Node _head;
        private volatile Node _tail;
        private InterlockedFlag<DequeState> _leftState = DequeState.Normal;
        private InterlockedFlag<DequeState> _rightState = DequeState.Normal;

        public LockFreeDeque2()
        {
            var sentinel = new Node(default(T));
            _head = _tail = sentinel;
        }

        public void PushRight(T item)
        {
            var newNode = new Node(item);

            while (true)
            {
                if (_rightState.TrySet(DequeState.Updating, DequeState.Normal))
                {
                    // 在持有状态锁的情况下不断尝试找到有效的tail
                    Node currentTail;
                    while (true)
                    {
                        currentTail = _tail;

                        // 如果当前tail没有被标记删除，可以使用
                        if (!currentTail.IsMarkedForDeletion)
                        {
                            break;
                        }

                        // 如果被标记删除了，帮助完成删除操作
                        // 向前移动tail指针到前一个节点
                        var prevNode = currentTail.Prev;
                        if (prevNode != null)
                        {
                            // 尝试更新_tail，如果失败说明其他线程已经更新了
                            Interlocked.CompareExchange(ref _tail, prevNode, currentTail);
                        }

                        // 小延迟，让其他线程有机会完成操作
                        Thread.Yield();
                    }

                    // 现在currentTail是一个有效的未删除节点
                    currentTail.Next = newNode;
                    newNode.Prev = currentTail;
                    _tail = newNode;
                    _rightState = DequeState.Normal;
                    break;
                }
                Thread.Yield();
            }
        }

        public bool TryPopRight(out T item)
        {
            item = default;

            while (true)
            {
                if (_rightState.TrySet(DequeState.Updating, DequeState.Normal))
                {
                    var currentTail = _tail;

                    // 跳过哨兵节点
                    if (currentTail == _head)
                    {
                        _rightState = DequeState.Normal;
                        return false;
                    }

                    // 跳过已经被标记删除的节点
                    if (currentTail.IsMarkedForDeletion)
                    {
                        _rightState = DequeState.Normal;
                        Thread.Yield();
                        continue;
                    }

                    // 尝试标记节点为已删除
                    if (!currentTail.TryMarkForDeletion())
                    {
                        // 节点已被其他线程标记删除
                        _rightState = DequeState.Normal;
                        Thread.Yield();
                        continue;
                    }

                    // 节点已成功标记，可以安全读取值
                    item = currentTail.Value;

                    // 物理删除节点 - 先更新_tail，再断开链接
                    var prevNode = currentTail.Prev;
                    if (prevNode != null)
                    {
                        _tail = prevNode;
                        prevNode.Next = null;
                        currentTail.Prev = null; // 帮助GC
                    }

                    _rightState = DequeState.Normal;
                    return true;
                }

                Thread.Yield();
            }
        }

        public bool TryPopLeft(out T item)
        {
            item = default;

            while (true)
            {
                if (_leftState.TrySet(DequeState.Updating, DequeState.Normal))
                {
                    Node nextNode;
                    Node currentHead;

                    // 在持有状态锁的情况下找到第一个有效节点
                    while (true)
                    {
                        currentHead = _head;
                        nextNode = currentHead.Next;

                        // 队列为空
                        if (nextNode == null)
                        {
                            _leftState = DequeState.Normal;
                            return false;
                        }

                        // 如果节点未被标记删除，可以使用
                        if (!nextNode.IsMarkedForDeletion)
                        {
                            break;
                        }

                        // 如果被标记删除，帮助完成删除
                        if (nextNode.Next != null)
                        {
                            currentHead.Next = nextNode.Next;
                            nextNode.Next.Prev = currentHead;
                        }
                        else
                        {
                            currentHead.Next = null;
                            if (_tail == nextNode)
                            {
                                Interlocked.CompareExchange(ref _tail, currentHead, nextNode);
                            }
                        }

                        Thread.Yield();
                    }

                    // 尝试标记节点为已删除
                    if (!nextNode.TryMarkForDeletion())
                    {
                        // 罕见情况：在检查和标记之间，节点被其他线程标记了
                        _leftState = DequeState.Normal;
                        Thread.Yield();
                        continue;
                    }

                    // 节点已成功标记，可以安全读取值
                    item = nextNode.Value;

                    // 物理删除节点
                    if (nextNode.Next != null)
                    {
                        nextNode.Next.Prev = currentHead;
                        currentHead.Next = nextNode.Next;
                    }
                    else
                    {
                        // 这是最后一个元素
                        currentHead.Next = null;
                        if (_tail == nextNode)
                        {
                            _tail = currentHead;
                        }
                    }

                    _leftState = DequeState.Normal;
                    return true;
                }

                Thread.Yield();
            }
        }

        private class Node
        {
            public T Value;
            public volatile Node Next;
            public volatile Node Prev;
            private int _markedForDeletion = 0;

            public Node(T value)
            {
                Value = value;
            }

            public bool IsMarkedForDeletion => _markedForDeletion == 1;

            public bool TryMarkForDeletion()
            {
                return Interlocked.CompareExchange(ref _markedForDeletion, 1, 0) == 0;
            }
        }

        private enum DequeState
        {
            Normal,
            Updating
        }
    }


    /// <summary>
    /// 基于InterlockedFlag技术的无锁双端队列实现
    /// </summary>
    internal class LockFreeDeque1<T>
    {
        private volatile Node _head;
        private volatile Node _tail;
        private InterlockedFlag<DequeState> _leftState = DequeState.Normal;
        private InterlockedFlag<DequeState> _rightState = DequeState.Normal;

        public LockFreeDeque1()
        {
            var sentinel = new Node(default(T));
            _head = _tail = sentinel;
        }

        public void PushRight(T item)
        {
            var newNode = new Node(item);

            while (true)
            {
                if (_rightState.TrySet(DequeState.Updating, DequeState.Normal))
                {
                    var currentTail = _tail;
                    currentTail.Next = newNode;
                    newNode.Prev = currentTail;
                    _tail = newNode;
                    _rightState = DequeState.Normal;
                    break;
                }
                Thread.Yield();
            }
        }

        public bool TryPopRight(out T item)
        {
            item = default;

            while (true)
            {
                //var currentTail = _tail;
                //if (currentTail.Prev == null) // 只剩哨兵节点
                //{
                //    return false;
                //}

                if (_rightState.TrySet(DequeState.Updating, DequeState.Normal))
                {
                    var currentTail = _tail;
                    item = currentTail.Value;



                    _tail = currentTail.Prev;

                    if (_tail == null) // 只剩哨兵节点
                    {
                        _rightState = DequeState.Normal;
                        _tail = currentTail;
                        return false;
                    }

                    _tail.Next = null;

                    _rightState = DequeState.Normal;

                    return true;
                }

                if (_tail.Prev == null) // 快速检查是否为空
                {
                    return false;
                }

                Thread.Yield();
            }
        }

        public bool TryPopLeft(out T item)
        {
            item = default;

            while (true)
            {
                //var currentHead = _head;
                //if (currentHead.Next == null) // 队列为空
                //{
                //    return false;
                //}

                if (_leftState.TrySet(DequeState.Updating, DequeState.Normal))
                {
                    var currentHead = _head;
                    var nextNode = currentHead.Next;

                    if (nextNode == null) // 队列为空
                    {
                        _leftState.InterlockedValue = DequeState.Normal;
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
                        // 这是最后一个元素
                        currentHead.Next = null;
                        _tail = currentHead;
                    }

                    _leftState.InterlockedValue = DequeState.Normal;

                    return true;
                }

                if (_head.Next == null) // 快速检查是否为空
                {
                    return false;
                }

                Thread.Yield();
            }
        }

        private class Node
        {
            public T Value;
            public volatile Node Next;
            public volatile Node Prev;

            public Node(T value)
            {
                Value = value;
            }
        }

        private enum DequeState
        {
            Normal,
            Updating
        }
    }
}
