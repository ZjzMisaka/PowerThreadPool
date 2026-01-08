using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Collections;
using Xunit.Abstractions;

namespace UnitTest
{
    public class StealablePriorityCollectionTest
    {
        private readonly ITestOutputHelper _output;

        public StealablePriorityCollectionTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestConcurrentStealablePriorityQueue()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>(false);
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
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityStack<int> queue = new ConcurrentStealablePriorityStack<int>(false);
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
        public void TestConcurrentStealablePriorityDequeGet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var deque = new ConcurrentStealablePriorityDeque<int>(true);
            deque.Set(1, 2);
            deque.Set(2, 2);
            deque.Set(3, 4);
            deque.Set(4, 4);
            deque.Set(5, 6);
            deque.Set(6, 6);
            deque.Set(7, 5);
            deque.Set(8, 5);
            deque.Set(9, 3);
            deque.Set(10, 3);
            deque.Set(11, 1);
            deque.Set(12, 1);

            Assert.Equal(6, deque.Get());
            Assert.Equal(5, deque.Get());
            Assert.Equal(8, deque.Get());
            Assert.Equal(7, deque.Get());
            Assert.Equal(4, deque.Get());
            Assert.Equal(3, deque.Get());
            Assert.Equal(10, deque.Get());
            Assert.Equal(9, deque.Get());
            Assert.Equal(2, deque.Get());
            Assert.Equal(1, deque.Get());
            Assert.Equal(12, deque.Get());
            Assert.Equal(11, deque.Get());
        }

        [Fact]
        public void TestConcurrentStealablePriorityDequeStealOnlyZeroPriority()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var deque = new ConcurrentStealablePriorityDeque<int>(true);
            deque.Set(1, 0);
            deque.Set(2, 0);

            Assert.Equal(1, deque.Steal());
            Assert.Equal(2, deque.Steal());
        }

        [Fact]
        public void TestConcurrentStealablePriorityDequeStealWithMultiplePriorities()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var deque = new ConcurrentStealablePriorityDeque<int>(true);
            deque.Set(1, 0);
            deque.Set(2, 0);
            deque.Set(999, 5);

            Assert.Equal(999, deque.Get());

            Assert.Equal(1, deque.Steal());
            Assert.Equal(2, deque.Steal());
        }

        [Fact]
        public void TestConcurrentStealablePriorityQueueDiscard()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>(false);
            queue.Set(1, 0);
            Assert.Equal(1, queue.Discard());
        }

        [Fact]
        public void TestConcurrentStealablePriorityStackDiscard()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityStack<int> queue = new ConcurrentStealablePriorityStack<int>(false);
            queue.Set(1, 0);
            Assert.Equal(1, queue.Discard());
        }

        [Fact]
        public void TestConcurrentStealablePriorityDequeDiscard()
        {
            var deque = new ConcurrentStealablePriorityDeque<int>(true);
            deque.Set(1, 0);
            Assert.Equal(1, deque.Discard());
        }

        [Fact]
        public void TestConcurrentStealablePriorityQueueNotInserted()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>(false);
            queue.Set(1, -1);
            Assert.Equal(1, queue.Discard());
        }

        [Fact]
        public void TestConcurrentStealablePriorityStackNotInserted()
        {
            ConcurrentStealablePriorityStack<int> queue = new ConcurrentStealablePriorityStack<int>(false);
            queue.Set(1, -1);
            Assert.Equal(1, queue.Discard());
        }

        [Fact]
        public void TestConcurrentStealablePriorityDequeNotInserted()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var deque = new ConcurrentStealablePriorityDeque<int>(true);
            deque.Set(1, -1);
            Assert.Equal(1, deque.Discard());
        }

        [Fact]
        public void TestGetQueueReturnsFalseWhenPriorityNotZeroAndNotInDictionary()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var q = new ConcurrentStealablePriorityQueue<int>(false);

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

        [Fact]
        public void TestGetStackReturnsFalseWhenPriorityNotZeroAndNotInDictionary()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityQueue<int> q = new ConcurrentStealablePriorityQueue<int>(false);

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

        [Fact]
        public void TryGetStackReturnsFalseWhenPriorityNotZeroAndNotInDictionary()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityStack<int> s = new ConcurrentStealablePriorityStack<int>(false);

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

        [Fact]
        public void TestGetDequeReturnsFalseWhenPriorityNotZeroAndNotInDictionary()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var dq = new ConcurrentStealablePriorityDeque<int>(true);

            var type = typeof(ConcurrentStealablePriorityDeque<int>);
            var sortedField = type.GetField("_sortedPriorityList", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(sortedField);

            var newList = new List<int> { 0, 1 };
            sortedField!.SetValue(dq, newList);

            var result = dq.Get();
            Assert.Equal(default, result);

            dq.Set(42, 2);
            var got = dq.Get();
            Assert.Equal(42, got);
        }

        [Fact]
        public void TestStealDequeReturnsFalseWhenPriorityNotZeroAndNotInDictionary()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var dq = new ConcurrentStealablePriorityDeque<int>(true);

            var type = typeof(ConcurrentStealablePriorityDeque<int>);
            var sortedField = type.GetField("_sortedPriorityList", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(sortedField);

            sortedField!.SetValue(dq, new List<int> { 0, 1 });

            var result = dq.Steal();
            Assert.Equal(default, result);

            dq.Set(42, 2);
            var got = dq.Steal();
            Assert.Equal(42, got);
        }

        [Fact]
        public void TestDiscardDequeReturnsFalseWhenPriorityNotZeroAndNotInDictionary()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var dq = new ConcurrentStealablePriorityDeque<int>(true);

            var type = typeof(ConcurrentStealablePriorityDeque<int>);
            var sortedField = type.GetField("_sortedPriorityList", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(sortedField);

            sortedField!.SetValue(dq, new List<int> { 0, 1 });

            var result = dq.Discard();
            Assert.Equal(default, result);

            dq.Set(7, 2);

            var got = dq.Discard();
            Assert.Equal(7, got);
        }

        [Fact]
        public void ConcurrentStealablePriorityQueueDiscardShouldIterateAllPrioritiesAndReturnDefaultWhenAllQueuesEmpty()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityQueue<object> q = new ConcurrentStealablePriorityQueue<object>(false);

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

        [Fact]
        public void ConcurrentStealablePriorityStackDiscardShouldIterateAllPrioritiesAndReturnDefaultWhenAllQueuesEmpty()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityStack<object> q = new ConcurrentStealablePriorityStack<object>(false);

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

        [Fact]
        public void ConcurrentStealablePriorityDequeDiscardShouldIterateAllPrioritiesAndReturnDefaultWhenAllQueuesEmpty()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var dq = new ConcurrentStealablePriorityDeque<object>(true);

            var marker = new object();
            dq.Set(marker, priority: 10);

            var got = dq.Get();
            Assert.Same(marker, got);

            var zero = dq.Discard();
            for (int i = 0; i < 3; i++)
            {
                _ = dq.Discard();
            }

            var result = dq.Discard();

            Assert.Null(result);
        }

        [Fact]
        public void TestConcurrentStealablePriorityDequeGetFallsBackFromEmptyHigherPriorityToZero()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var d = new ConcurrentStealablePriorityDeque<int>(true);
            d.Set(100, 5);
            d.Set(1, 0);
            d.Set(2, 0);

            Assert.Equal(100, d.Get());
            Assert.Equal(2, d.Get());
            Assert.Equal(1, d.Get());
        }

        [Fact]
        public void TestGetOnEmptyDequeReturnsDefault()
        {
            var d = new ConcurrentStealablePriorityDeque<int>(true);
            var result = d.Get();
            Assert.Equal(default, result);
        }

        [Fact]
        public void TestDequeGetOnlyZeroPrioritySucceed()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var d = new ConcurrentStealablePriorityDeque<int>(true);
            d.Set(1, 0);
            d.Set(2, 0);

            Assert.Equal(2, d.Get());
            Assert.Equal(1, d.Get());
            Assert.Equal(default, d.Get());
        }

        [Fact]
        public void TestDequeStealPrefersHigherPriorityOverZero()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var d = new ConcurrentStealablePriorityDeque<int>(true);
            d.Set(1, 0);
            d.Set(100, 2);
            d.Set(200, 1);

            Assert.Equal(100, d.Steal());
            Assert.Equal(200, d.Steal());
            Assert.Equal(1, d.Steal());
            Assert.Equal(default, d.Steal());
        }

        [Fact]
        public void TestDequeDiscardPrefersLowestPriorityFirst()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var d = new ConcurrentStealablePriorityDeque<int>(true);
            d.Set(300, 3);
            d.Set(200, 2);
            d.Set(10, 0);
            d.Set(-5, -1);

            Assert.Equal(-5, d.Discard());
            Assert.Equal(10, d.Discard());
            Assert.Equal(200, d.Discard());
            Assert.Equal(300, d.Discard());
            Assert.Equal(default, d.Discard());
        }

        [Fact]
        public void TestDequeDiscardIsLifoWithinPriority()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var d = new ConcurrentStealablePriorityDeque<int>(true);
            d.Set(1, -1);
            d.Set(2, -1);

            Assert.Equal(2, d.Discard());
            Assert.Equal(1, d.Discard());
        }

        [Fact]
        public void TestDequeStealIsFifoWithinPriorityForNonZero()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var d = new ConcurrentStealablePriorityDeque<int>(true);
            d.Set(1, 2);
            d.Set(2, 2);

            Assert.Equal(1, d.Steal());
            Assert.Equal(2, d.Steal());
        }

        [Fact]
        public void TestInsertPriorityRaceQueue()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var d = new ConcurrentStealablePriorityQueue<int>(false);
            PowerPool powerPool = new PowerPool(new PowerThreadPool.Options.PowerPoolOption
            {
                MaxThreads = 100,
                StartSuspended = true,
            });
            for (int i = 0; i < 10000; ++i)
            {
                int localI = i;
                powerPool.QueueWorkItem(() => { d.Set(localI, localI); });
            }
            powerPool.Start();
            powerPool.Wait();

            Assert.Equal(10000, d._sortedPriorityList.Count);
        }

        [Fact]
        public void TestInsertPriorityRaceStack()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var d = new ConcurrentStealablePriorityStack<int>(false);
            PowerPool powerPool = new PowerPool(new PowerThreadPool.Options.PowerPoolOption
            {
                MaxThreads = 100,
                StartSuspended = true,
            });
            for (int i = 0; i < 10000; ++i)
            {
                int localI = i;
                powerPool.QueueWorkItem(() => { d.Set(localI, localI); });
            }
            powerPool.Start();
            powerPool.Wait();

            Assert.Equal(10000, d._sortedPriorityList.Count);
        }

        [Fact]
        public void TestInsertPriorityRaceDeque()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var d = new ConcurrentStealablePriorityDeque<int>(true);
            PowerPool powerPool = new PowerPool(new PowerThreadPool.Options.PowerPoolOption
            {
                MaxThreads = 100,
                StartSuspended = true,
            });
            for (int i = 0; i < 10000; ++i)
            {
                int localI = i;
                powerPool.QueueWorkItem(() => { d.Set(localI, localI); });
            }
            powerPool.Start();
            powerPool.Wait();

            Assert.Equal(10000, d._sortedPriorityList.Count);
        }
    }
}
