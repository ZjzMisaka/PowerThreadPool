using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using PowerThreadPool.Works;
using Xunit.Abstractions;

namespace UnitTest
{
    /// <summary>
    /// Tests that writes to a work's option never leak into sibling works that
    /// share the same option instance (WorkOption.DefaultInstance, Parallel
    /// batch options): writes go through a clone-on-write instead.
    /// </summary>
    public class DefaultWorkOptionIsolationTest
    {
        private readonly ITestOutputHelper _output;

        public DefaultWorkOptionIsolationTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestAddWorkToGroupDoesNotLeakIntoOtherDefaultOptionWorks()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id1 = powerPool.QueueWorkItem(() => Thread.Sleep(1000));
            WorkID id2 = powerPool.QueueWorkItem(() => Thread.Sleep(1000));

            // Both works share WorkOption.DefaultInstance; AddWorkToGroup writes
            // WorkOption.Group on work1, which must not re-group work2.
            bool res = powerPool.AddWorkToGroup("AAA", id1);
            Assert.True(res);

            Assert.Contains(id1, powerPool.GetGroupMemberSet("AAA"));
            Assert.DoesNotContain(id2, powerPool.GetGroupMemberSet("AAA"));

            powerPool.Stop();
        }

        [Fact]
        public void TestExternalMutationOnDefaultInstanceDoesNotAffectQueuedWork()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id1 = powerPool.QueueWorkItem(() => Thread.Sleep(1000));

            // Simulate an internal/friendly mutation of the shared default
            // instance after work1 was queued: work1 must not observe it.
            WorkOption.DefaultInstance.Group = "BBB";

            Assert.DoesNotContain(id1, powerPool.GetGroupMemberSet("BBB"));

            powerPool.Stop();
        }

        [Fact]
        public void TestAddWorkToGroupKeepsOtherOptionFields()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(
                () => Thread.Sleep(1000),
                new WorkOption { CustomWorkID = "custom-id-1", LongRunning = true });

            bool res = powerPool.AddWorkToGroup("AAA", id);
            Assert.True(res);

            // Regression: the Group write used to replace the whole option with
            // a bare WorkOption(), silently dropping CustomWorkID and other fields.
            // (Non-shared options are written in place, this also guards the clone path.)
            Assert.True(powerPool._aliveWorkDic.TryGetValue(id, out WorkBase work));
            Assert.True(work.LongRunning);

            powerPool.Stop();
        }

        [Fact]
        public void TestAddWorkToGroupOnParallelBatchWorkDoesNotLeakIntoBatchSiblings()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.For(0, 8, i => Thread.Sleep(5000));

            // All works of the batch share one flagged option; re-grouping one
            // member must clone instead of rewriting the shared option, which
            // would change every sibling's Group as well.
            string batchGroupName = powerPool._workGroupDic.Keys.FirstOrDefault();
            Assert.NotNull(batchGroupName);
            ConcurrentSet<WorkID> batchMembers = powerPool.GetGroupMemberSet(batchGroupName);
            WorkID memberId = batchMembers.FirstOrDefault();
            WorkID siblingId = batchMembers.FirstOrDefault(id => id != memberId);

            bool res = powerPool.AddWorkToGroup("AAA", memberId);
            Assert.True(res);

            Assert.True(powerPool._aliveWorkDic.TryGetValue(memberId, out WorkBase moved));
            Assert.Equal("AAA", moved.Group);

            // The sibling must still report the original batch group.
            Assert.True(powerPool._aliveWorkDic.TryGetValue(siblingId, out WorkBase sibling));
            Assert.Equal(batchGroupName, sibling.Group);

            // Only the moved work was registered under the new group.
            Assert.Equal(1, powerPool.GetGroupMemberSet("AAA").Count);

            powerPool.Stop();
        }

        [Fact]
        public void TestCloneResetsDefaultInstanceFlagAndKeepsValues()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            WorkOption source = new WorkOption { Group = "G", WorkPriority = 3 };
            source.IsDefaultInstance = true;

            WorkOption clone = source.Clone();

            Assert.False(clone.IsDefaultInstance);
            Assert.True(source.IsDefaultInstance);
            Assert.Equal("G", clone.Group);
            Assert.Equal(3, clone.WorkPriority);
            Assert.NotSame(source, clone);
        }
    }
}
