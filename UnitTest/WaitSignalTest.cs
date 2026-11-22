using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using Xunit;
using Xunit.Abstractions;

namespace UnitTest
{
    /// <summary>
    /// Regression tests for the lazy creation of the per-work wait / pause signals.
    ///
    /// A signal is published by the first caller that needs it. Publishing must be atomic:
    /// every concurrent caller has to receive that one instance, no caller may observe null, and no
    /// caller may overwrite (and orphan) an instance another caller is already waiting on.
    /// </summary>
    public class WaitSignalTest
    {
        private static readonly TimeSpan JoinTimeout = TimeSpan.FromSeconds(10);

        private readonly ITestOutputHelper _output;

        public WaitSignalTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void WaitAndPauseSignalsAreNotCreatedBeforeUse()
        {
            _output.WriteLine($"Testing {GetType().Name}.{nameof(WaitAndPauseSignalsAreNotCreatedBeforeUse)}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 2 });
            try
            {
                Work<int> work = NewWork(powerPool);

                // Laziness is deliberate: a work that is never waited on / paused allocates nothing.
                Assert.Null(work.WaitSignal);
                Assert.Null(work.PauseSignal);
                Assert.Null(work.PauseAsyncSignal);
            }
            finally
            {
                powerPool.Dispose();
            }
        }

        [Fact]
        public void EnsureWaitSignalExistsConcurrentCallersShareOneInstance()
        {
            _output.WriteLine($"Testing {GetType().Name}.{nameof(EnsureWaitSignalExistsConcurrentCallersShareOneInstance)}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 2 });
            try
            {
                MethodInfo ensureWaitSignal = EnsureMethod("EnsureWaitSignalExists");

                for (int round = 0; round < 50; ++round)
                {
                    Work<int> work = NewWork(powerPool);
                    object[] signals = InvokeConcurrently(ensureWaitSignal, work, 4);

                    // A loser must never get its own (orphaned) instance and must never get null.
                    Assert.All(signals, signal => Assert.NotNull(signal));
                    Assert.All(signals, signal => Assert.Same(signals[0], signal));
                    Assert.Same(signals[0], work.WaitSignal);
                }
            }
            finally
            {
                powerPool.Dispose();
            }
        }

        [Fact]
        public void EnsurePauseSignalExistsConcurrentCallersShareOneInstance()
        {
            _output.WriteLine($"Testing {GetType().Name}.{nameof(EnsurePauseSignalExistsConcurrentCallersShareOneInstance)}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 2 });
            try
            {
                MethodInfo ensurePauseSignal = EnsureMethod("EnsurePauseSignalExists");
                MethodInfo ensurePauseAsyncSignal = EnsureMethod("EnsurePauseAsyncSignalExists");

                for (int round = 0; round < 50; ++round)
                {
                    Work<int> work = NewWork(powerPool);
                    object[] signals = InvokeConcurrently(ensurePauseSignal, work, 4);

                    Assert.All(signals, signal => Assert.NotNull(signal));
                    Assert.All(signals, signal => Assert.Same(signals[0], signal));
                    Assert.Same(signals[0], work.PauseSignal);

                    // The instance a PauseIfRequested waiter captures must be the one Resume signals.
                    Assert.True(work.Pause());
                    Assert.True(work.IsPausing);

                    ManualResetEvent pauseSignal = (ManualResetEvent)signals[0];
                    bool released = false;
                    Thread waiter = new Thread(() =>
                    {
                        released = pauseSignal.WaitOne(JoinTimeout);
                    })
                    { IsBackground = true };
                    waiter.Start();

                    Assert.True(work.Resume());
                    Assert.True(waiter.Join(JoinTimeout), "the waiter was not released by Resume");
                    Assert.True(released);
                    Assert.False(work.IsPausing);

                    Work<int> asyncWork = NewWork(powerPool);
                    object[] asyncSignals = InvokeConcurrently(ensurePauseAsyncSignal, asyncWork, 4);

                    Assert.All(asyncSignals, signal => Assert.NotNull(signal));
                    Assert.All(asyncSignals, signal => Assert.Same(asyncSignals[0], signal));
                    Assert.Same(asyncSignals[0], asyncWork.PauseAsyncSignal);
                }
            }
            finally
            {
                powerPool.Dispose();
            }
        }

        [Fact]
        public void ConcurrentWaitersOnRunningWorkAllReturnTheResult()
        {
            _output.WriteLine($"Testing {GetType().Name}.{nameof(ConcurrentWaitersOnRunningWorkAllReturnTheResult)}");

            const int waiterCount = 3;
            const int rounds = 100;

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 4 });
            bool completed = false;
            try
            {
                for (int round = 0; round < rounds; ++round)
                {
                    ManualResetEventSlim workGate = new ManualResetEventSlim(false);
                    ManualResetEventSlim startGate = new ManualResetEventSlim(false);
                    try
                    {
                        int expected = round;
                        WorkID id = powerPool.QueueWorkItem(
                            () =>
                            {
                                workGate.Wait();
                                return expected;
                            },
                            new WorkOption<int> { IsBackground = true });

                        ExecuteResult<int>[] results = new ExecuteResult<int>[waiterCount];
                        Exception[] errors = new Exception[waiterCount];
                        int ready = 0;
                        Thread[] waiters = new Thread[waiterCount];

                        for (int i = 0; i < waiterCount; ++i)
                        {
                            int index = i;
                            waiters[i] = new Thread(() =>
                            {
                                try
                                {
                                    startGate.Wait();
                                    Interlocked.Increment(ref ready);
                                    results[index] = powerPool.Fetch<int>(id);
                                }
                                catch (Exception ex)
                                {
                                    errors[index] = ex;
                                }
                            })
                            { IsBackground = true };
                            waiters[i].Start();
                        }

                        startGate.Set();

                        // Let every waiter get past the door and enter Fetch before the work completes.
                        // That is the interleaving that used to let two waiters each publish an event,
                        // one of which was then overwritten and never signaled.
                        Assert.True(
                            SpinWait.SpinUntil(() => Volatile.Read(ref ready) == waiterCount, 5000),
                            $"round {round}: not all waiters started");

                        Thread.Sleep(2);
                        workGate.Set();

                        bool joined = true;
                        foreach (Thread waiter in waiters)
                        {
                            joined &= waiter.Join(JoinTimeout);
                        }

                        // Deliberately fail instead of waiting forever; the pool is left undisposed on
                        // this path because disposing it would also wait for the stuck waiter.
                        Assert.True(joined, $"round {round}: a waiter never completed (lost wake-up)");
                        Assert.All(errors, error => Assert.Null(error));
                        Assert.All(results, result =>
                        {
                            Assert.NotNull(result);
                            Assert.True(result.IsFound, $"round {round}: the work was already untracked");
                            Assert.Equal(expected, result.Result);
                        });
                    }
                    finally
                    {
                        workGate.Set();
                        startGate.Dispose();
                        workGate.Dispose();
                    }
                }

                completed = true;
            }
            finally
            {
                if (completed)
                {
                    powerPool.Dispose();
                }
            }
        }

        private static Work<int> NewWork(PowerPool powerPool)
        {
            return new WorkFunc<int>(powerPool, null, () => 0, new WorkOption<int>(), null);
        }

        private static MethodInfo EnsureMethod(string name)
        {
            MethodInfo method = typeof(Work<int>).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return method;
        }

        private static object[] InvokeConcurrently(MethodInfo method, object target, int callerCount)
        {
            object[] results = new object[callerCount];
            Exception[] errors = new Exception[callerCount];
            Thread[] callers = new Thread[callerCount];

            using (Barrier barrier = new Barrier(callerCount))
            {
                for (int i = 0; i < callerCount; ++i)
                {
                    int index = i;
                    callers[i] = new Thread(() =>
                    {
                        try
                        {
                            barrier.SignalAndWait();
                            results[index] = method.Invoke(target, null);
                        }
                        catch (Exception ex)
                        {
                            errors[index] = ex;
                        }
                    })
                    { IsBackground = true };
                    callers[i].Start();
                }

                foreach (Thread caller in callers)
                {
                    Assert.True(caller.Join(JoinTimeout), $"{method.Name}: a caller did not return in time");
                }
            }

            Assert.All(errors, error => Assert.Null(error));
            return results;
        }
    }
}
