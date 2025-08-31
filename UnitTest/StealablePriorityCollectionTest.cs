using PowerThreadPool.Collections;

namespace UnitTest
{
    public class StealablePriorityCollectionTest
    {
        [Fact]
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

        [Fact]
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

        [Fact]
        public void TestConcurrentStealablePriorityQueueDiscard()
        {
            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>();
            queue.Set(1, 0);
            Assert.Equal(1, queue.Discard());
        }

        [Fact]
        public void TestConcurrentStealablePriorityStackDiscard()
        {
            ConcurrentStealablePriorityStack<int> queue = new ConcurrentStealablePriorityStack<int>();
            queue.Set(1, 0);
            Assert.Equal(1, queue.Discard());
        }

        [Fact]
        public void TestConcurrentStealablePriorityQueueNotInserted()
        {
            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>();
            queue.Set(1, -1);
            Assert.Equal(1, queue.Discard());
        }

        [Fact]
        public void TestConcurrentStealablePriorityStackNotInserted()
        {
            ConcurrentStealablePriorityStack<int> queue = new ConcurrentStealablePriorityStack<int>();
            queue.Set(1, -1);
            Assert.Equal(1, queue.Discard());
        }
    }
}
