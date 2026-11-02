using System.Reflection;
using System.Threading;
using PowerThreadPool;
using PowerThreadPool.Helpers.Asynchronous;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using Xunit.Abstractions;

namespace UnitTest
{
    // Regression tests for SynchronizationContext save/restore around async work
    // segments. The PowerPoolSynchronizationContext must only be current while
    // an async work segment (first segment or a continuation) is executing on a
    // worker thread; it must never leak onto a worker thread between segments,
    // otherwise unrelated works (typically sync-over-async blocks) capture a
    // stale context whose Posts are routed back into the pool and can deadlock.
    public class SyncContextRestoreTest
    {
        private readonly ITestOutputHelper _output;

        public SyncContextRestoreTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestSyncOverAsyncDoesNotDeadlockAfterAsyncWork()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            // The async work's first segment leaves the worker after an await;
            // its PTP context must not stay installed on the worker thread.
            powerPool.QueueWorkItem(async () =>
            {
                await Task.Delay(300);
            });

            // Let the first segment run and suspend before queuing the victim.
            Thread.Sleep(150);

            // Sync-over-async on the same single worker: before the fix, the
            // continuation of Inner() was posted to the leaked context, which
            // enqueued it behind this blocked work on the only worker.
            powerPool.QueueWorkItem(() =>
            {
                Inner().GetAwaiter().GetResult();
            });

            // Fails (and would otherwise hang) if the deadlock regresses.
            powerPool.Wait(new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token);

            Assert.Equal(0, powerPool.AsyncWorkCount);
        }

        [Fact]
        public void TestWorkerThreadHasNoStaleContextAfterAsyncWorks()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            powerPool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
                await Task.Delay(50);
            });
            powerPool.QueueWorkItem(new Func<Task<int>>(async () =>
            {
                await Task.Yield();
                return 1;
            }));
            powerPool.Wait();

            string leaked = null;
            ManualResetEventSlim done = new ManualResetEventSlim(false);
            powerPool.QueueWorkItem(new Func<string>(() =>
                SynchronizationContext.Current?.GetType().Name ?? "null"),
                (res) =>
                {
                    leaked = (string)res.Result;
                    done.Set();
                });

            Assert.True(done.Wait(TimeSpan.FromSeconds(30)));
            Assert.Equal("null", leaked);
        }

        [Fact]
        public void TestContinuationStillSeesPoolContextInsideAsyncWork()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            bool firstSegmentOnCtx = false;
            bool continuationOnCtx = false;
            bool afterAwaitStillOnCtx = false;

            powerPool.QueueWorkItem(async () =>
            {
                firstSegmentOnCtx =
                    SynchronizationContext.Current is PowerPoolSynchronizationContext;
                await Task.Yield();
                // After an asynchronous continuation segment resumed, the context
                // must be re-installed for the rest of the segment.
                afterAwaitStillOnCtx =
                    SynchronizationContext.Current is PowerPoolSynchronizationContext;
                await Task.Delay(50);
                continuationOnCtx =
                    SynchronizationContext.Current is PowerPoolSynchronizationContext;
            });

            powerPool.Wait(new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token);

            Assert.True(firstSegmentOnCtx);
            Assert.True(afterAwaitStillOnCtx);
            Assert.True(continuationOnCtx);
        }

        [Fact]
        public void TestPostAfterWorkFinalizedIsNotDropped()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            // A finalized work: execute result present, no worker attached.
            WorkOption workOption = new WorkOption();
            WorkAction<object> work = new WorkAction<object>();
            work.Init(powerPool, WorkID.FromString("post-after-finalized-test"), workOption, null);
            work.AllowEventsAndCallback = true;
            work.SetExecuteResult(null, null, WorkStatus.Stopped);

            PowerPoolSynchronizationContext ctx = new PowerPoolSynchronizationContext(powerPool, work);

            ManualResetEventSlim posted = new ManualResetEventSlim(false);
            ctx.Post(_ => posted.Set(), null);

            // Before the fix, Post returned silently and this wait timed out.
            Assert.True(posted.Wait(TimeSpan.FromSeconds(30)));
        }

        private static async Task Inner() => await Task.Delay(50);
    }
}
