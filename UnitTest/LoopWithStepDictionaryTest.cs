using System.Collections.Concurrent;
using System.Reflection;
using PowerThreadPool.Collections;

namespace UnitTest
{
    public class LoopWithStepDictionaryTest
    {
        [Fact]
        public void TestRebuildSnapshotUnderConcurrentAddDoesNotThrow()
        {
            LoopWithStepDictionary<int, object> dic = new LoopWithStepDictionary<int, object>();

            const int initialCount = 8;
            for (int i = 0; i < initialCount; i++)
            {
                dic.TryAdd(i, new object());
            }

            Task adder = Task.Run(() =>
            {
                int key = 1_000_000;
                while (true)
                {
                    dic.TryAdd(key++, new object());
                }
            });

            try
            {
                for (int i = 0; i < 300_000; i++)
                {
                    if (i % 100_000 == 0 && i > 0)
                    {
                        Assert.False(adder.IsCompleted, $"adder ended unexpectedly: status={adder.Status}, error={adder.Exception?.GetBaseException()}");
                    }
                    int key = 2_000_000 + (i % 3);
                    dic.TryAdd(key, new object());
                    dic.TryRemove(key, out _);
                }
            }
            finally
            {
                Assert.False(adder.IsFaulted, $"adder faulted: {adder.Exception?.GetBaseException()}");
            }
        }

        [Fact]
        public void TestSnapshotMatchesDictionaryContent()
        {
            LoopWithStepDictionary<int, string> dic = new LoopWithStepDictionary<int, string>();

            Assert.Empty(dic.GetSnapshot());

            for (int i = 0; i < 100; i++)
            {
                dic.TryAdd(i, $"v{i}");
            }
            string[] snapshot = dic.GetSnapshot();
            Assert.Equal(100, snapshot.Length);
            Assert.Equal(100, snapshot.Distinct().Count());
            Assert.All(snapshot, v => Assert.StartsWith("v", v));

            dic.TryRemove(42, out _);
            snapshot = dic.GetSnapshot();
            Assert.Equal(99, snapshot.Length);
            Assert.DoesNotContain("v42", snapshot);

            dic.Clear();
            Assert.Empty(dic.GetSnapshot());
        }

        [Fact]
        public void TestTryAddTryRemoveBehavior()
        {
            LoopWithStepDictionary<int, string> dic = new LoopWithStepDictionary<int, string>();

            Assert.True(dic.TryAdd(1, "a"));
            Assert.True(dic.TryAdd(2, "b"));
            Assert.False(dic.TryRemove(999, out string removed));
            Assert.Null(removed);
            Assert.True(dic.TryRemove(1, out removed));
            Assert.Equal("a", removed);
            Assert.Single(dic.GetSnapshot());
        }
    }
}
