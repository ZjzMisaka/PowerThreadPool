using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using PowerThreadPool;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using Xunit.Abstractions;

namespace UnitTest
{
    public class ObsoleteFeaturesTest
    {
        private readonly ITestOutputHelper _output;

        public ObsoleteFeaturesTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestObsoleteAttributeEnforceDequeOwnership1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>();
            ConcurrentStealablePriorityStack<int> stack = new ConcurrentStealablePriorityStack<int>();
            ConcurrentStealablePriorityDeque<int> deque = new ConcurrentStealablePriorityDeque<int>();
            PowerPool powerPool = new PowerPool(new PowerThreadPool.Options.PowerPoolOption { QueueType = PowerThreadPool.Options.QueueType.Deque, EnforceDequeOwnership = true });
            powerPool.QueueWorkItem(() => { });
            powerPool.Wait();
        }

        [Fact]
        public void TestObsoleteAttributeEnforceDequeOwnership2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            ConcurrentStealablePriorityQueue<int> queue = new ConcurrentStealablePriorityQueue<int>();
            ConcurrentStealablePriorityStack<int> stack = new ConcurrentStealablePriorityStack<int>();
            ConcurrentStealablePriorityDeque<int> deque = new ConcurrentStealablePriorityDeque<int>();
            PowerPool powerPool = new PowerPool(new PowerThreadPool.Options.PowerPoolOption { QueueType = PowerThreadPool.Options.QueueType.FIFO, EnforceDequeOwnership = true });
            powerPool.QueueWorkItem(() => { });
            powerPool.Wait();
        }

        [Fact]
        public void TestRejectDiscardQueuedPolicy()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardQueuedPolicy,
                    ThreadQueueLimit = 1,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            _ = powerPool
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                });

            bool done = false;
            powerPool.QueueWorkItem(() =>
            {
                done = true;
            });
            Assert.False(done);
            Assert.Equal(4, powerPool.WaitingWorkCount);

            powerPool.Wait();

            Assert.True(done);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }
    }
}
