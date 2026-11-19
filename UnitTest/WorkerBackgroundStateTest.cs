using System.Collections.Generic;
using System.Threading;
using PowerThreadPool;
using PowerThreadPool.Options;
using Xunit;
using Xunit.Abstractions;

namespace UnitTest
{
    public class WorkerBackgroundStateTest
    {
        private readonly ITestOutputHelper _output;

        public WorkerBackgroundStateTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestIdleWorkerThreadTurnsBackground()
        {
            _output.WriteLine($"Testing {GetType().Name}.{nameof(TestIdleWorkerThreadTurnsBackground)}");

            using PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 1 });

            Assert.False(RunWork(powerPool, isBackground: false),
                "a foreground work was executed on a background thread");

            Worker worker = GetSingleWorker(powerPool);
            Assert.True(SpinWait.SpinUntil(() => worker._thread.IsBackground, 10_000),
                "the idle worker thread stayed in the foreground state");

            Assert.True(RunWork(powerPool, isBackground: true),
                "a background work was executed on a foreground thread");
        }

        [Fact]
        public void TestForegroundBackgroundSwitchAcrossIdleRounds()
        {
            _output.WriteLine($"Testing {GetType().Name}.{nameof(TestForegroundBackgroundSwitchAcrossIdleRounds)}");

            using PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 1 });

            foreach (bool isBackground in new[] { false, true, false, true, false })
            {
                Assert.Equal(isBackground, RunWork(powerPool, isBackground));

                Worker worker = GetSingleWorker(powerPool);
                Assert.True(SpinWait.SpinUntil(() => worker._thread.IsBackground, 10_000),
                    "the worker thread did not return to the background state before the next work");
            }
        }

        private static bool RunWork(PowerPool powerPool, bool isBackground)
        {
            bool observed = !isBackground;
            ManualResetEventSlim done = new ManualResetEventSlim(false);

            powerPool.QueueWorkItem(
                () =>
                {
                    observed = Thread.CurrentThread.IsBackground;
                    done.Set();
                },
                new WorkOption() { IsBackground = isBackground });

            Assert.True(done.Wait(10_000), "the work never started");
            powerPool.Wait();

            return observed;
        }

        private static Worker GetSingleWorker(PowerPool powerPool)
        {
            Worker worker = null;

            Assert.True(SpinWait.SpinUntil(() =>
            {
                worker = null;
                foreach (KeyValuePair<int, Worker> kv in powerPool._aliveWorkerDic)
                {
                    worker = kv.Value;
                }
                return worker != null;
            }, 10_000), "no alive worker was found");

            return worker;
        }
    }
}
