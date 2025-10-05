using System.Reflection;
using PowerThreadPool.Collections;

namespace UnitTest
{
    public class StealablePriorityCollectionTest
    {
        [Fact(Timeout = 5 * 60 * 1000)]
        public void TestConcurrentStealablePriorityQueue()
        {
            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>();
            queue.Set(1, 2);
            queue.Set(2, 2);
            queue.Set(3, 4);
            queue.Set(4, 4);
            queue.Set(5, 6);
            queue.Set(6, 6);
            queue.Set(7, 5);
            queue.Set(8, 5);
            queue.Set(9, 3);
            queue.Set(10, 3);
            queue.Set(11, 1);
            queue.Set(12, 1);

            Assert.Equal(5, queue.Get());
            Assert.Equal(6, queue.Get());
            Assert.Equal(7, queue.Get());
            Assert.Equal(8, queue.Get());
            Assert.Equal(3, queue.Get());
            Assert.Equal(4, queue.Get());
            Assert.Equal(9, queue.Get());
            Assert.Equal(10, queue.Get());
            Assert.Equal(1, queue.Get());
            Assert.Equal(2, queue.Get());
            Assert.Equal(11, queue.Get());
            Assert.Equal(12, queue.Get());
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void TestConcurrentStealablePriorityStack()
        {
            ConcurrentStealablePriorityStack<int> queue = new ConcurrentStealablePriorityStack<int>();
            queue.Set(1, 2);
            queue.Set(2, 2);
            queue.Set(3, 4);
            queue.Set(4, 4);
            queue.Set(5, 6);
            queue.Set(6, 6);
            queue.Set(7, 5);
            queue.Set(8, 5);
            queue.Set(9, 3);
            queue.Set(10, 3);
            queue.Set(11, 1);
            queue.Set(12, 1);

            Assert.Equal(6, queue.Get());
            Assert.Equal(5, queue.Get());
            Assert.Equal(8, queue.Get());
            Assert.Equal(7, queue.Get());
            Assert.Equal(4, queue.Get());
            Assert.Equal(3, queue.Get());
            Assert.Equal(10, queue.Get());
            Assert.Equal(9, queue.Get());
            Assert.Equal(2, queue.Get());
            Assert.Equal(1, queue.Get());
            Assert.Equal(12, queue.Get());
            Assert.Equal(11, queue.Get());
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void TestConcurrentStealablePriorityQueueDiscard()
        {
            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>();
            queue.Set(1, 0);
            Assert.Equal(1, queue.Discard());
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void TestConcurrentStealablePriorityStackDiscard()
        {
            ConcurrentStealablePriorityStack<int> queue = new ConcurrentStealablePriorityStack<int>();
            queue.Set(1, 0);
            Assert.Equal(1, queue.Discard());
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void TestConcurrentStealablePriorityQueueNotInserted()
        {
            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>();
            queue.Set(1, -1);
            Assert.Equal(1, queue.Discard());
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void TestConcurrentStealablePriorityStackNotInserted()
        {
            ConcurrentStealablePriorityStack<int> queue = new ConcurrentStealablePriorityStack<int>();
            queue.Set(1, -1);
            Assert.Equal(1, queue.Discard());
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void TestGetQueueReturnsFalseWhenPriorityNotZeroAndNotInDictionary()
        {
            var q = new ConcurrentStealablePriorityQueue<int>();

            var type = typeof(ConcurrentStealablePriorityQueue<int>);
            var sortedField = type.GetField("_sortedPriorityList", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(sortedField);

            var newList = new List<int> { 0, 1 };
            sortedField!.SetValue(q, newList);

            var result = q.Get();

            Assert.Equal(default, result);

            q.Set(42, 2);
            var got = q.Get();
            Assert.Equal(42, got);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void TestGetStackReturnsFalseWhenPriorityNotZeroAndNotInDictionary()
        {
            ConcurrentStealablePriorityQueue<int> q = new ConcurrentStealablePriorityQueue<int>();

            Type type = typeof(ConcurrentStealablePriorityQueue<int>);
            FieldInfo sortedField = type.GetField("_sortedPriorityList", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(sortedField);

            List<int> newList = new List<int> { 0, 1 };
            sortedField!.SetValue(q, newList);

            int result = q.Get();

            Assert.Equal(default, result);

            q.Set(42, 2);
            int got = q.Get();
            Assert.Equal(42, got);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void TryGetStackReturnsFalseWhenPriorityNotZeroAndNotInDictionary()
        {
            ConcurrentStealablePriorityStack<int> s = new ConcurrentStealablePriorityStack<int>();

            Type type = typeof(ConcurrentStealablePriorityStack<int>);
            FieldInfo sortedField = type.GetField("_sortedPriorityList", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(sortedField);

            List<int> newList = new List<int> { 0, 1 };
            sortedField!.SetValue(s, newList);

            int result = s.Get();

            Assert.Equal(default, result);

            s.Set(99, 2);
            int got = s.Get();
            Assert.Equal(99, got);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void ConcurrentStealablePriorityQueueDiscardShouldIterateAllPrioritiesAndReturnDefaultWhenAllQueuesEmpty()
        {
            ConcurrentStealablePriorityQueue<object> q = new ConcurrentStealablePriorityQueue<object>();

            object marker = new object();
            q.Set(marker, priority: 10);

            object got = q.Get();
            Assert.Same(marker, got);

            object zero = q.Discard();
            for (int i = 0; i < 3; i++)
            {
                _ = q.Discard();
            }

            object result = q.Discard();

            Assert.Null(result);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public void ConcurrentStealablePriorityStackDiscardShouldIterateAllPrioritiesAndReturnDefaultWhenAllQueuesEmpty()
        {
            ConcurrentStealablePriorityStack<object> q = new ConcurrentStealablePriorityStack<object>();

            object marker = new object();
            q.Set(marker, priority: 10);

            object got = q.Get();
            Assert.Same(marker, got);

            object zero = q.Discard();
            for (int i = 0; i < 3; i++)
            {
                _ = q.Discard();
            }

            object result = q.Discard();

            Assert.Null(result);
        }
    }
}
