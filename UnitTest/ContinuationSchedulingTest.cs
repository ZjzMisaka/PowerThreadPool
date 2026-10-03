using System.Reflection;
using System.Threading;
using PowerThreadPool;
using PowerThreadPool.Constants;
using PowerThreadPool.Helpers.Asynchronous;
using PowerThreadPool.Options;
using PowerThreadPool.Works;
using Xunit.Abstractions;

namespace UnitTest
{
    /// <summary>
    /// Coverage for the continuation scheduling pipeline of PowerPoolSynchronizationContext:
    /// how Post() schedules a drain onto a worker, and how posts are enqueued into the
    /// slot/overflow queues. Includes the drain-scheduling fast path introduced by
    /// "perf: Set Continuation work into local worker id possible" (9a7b980) and
    /// "fix: Guard drain scheduling for running workers in fast path" (2f669b4).
    ///
    /// PowerPoolSynchronizationContext.Post() picks among these branches when scheduling a drain:
    ///   A. worker != null + reserve OK + worker Running     -> worker.SetWork(...)        (fast path, guarded)
    ///   B. worker != null + reserve OK + worker NOT Running -> release + pool.SetWork(...) (2f669b4 guard fallback)
    ///   C. worker != null + reserve FAIL (flag not Allowed) -> pool.SetWork(...)           (contended / idle-transition)
    ///   D. worker == null (work not bound to a worker)      -> pool.SetWork(...)           (pre-9a7b980 path)
    ///
    /// Branch A is the common case and is covered end-to-end by keeping the local worker busy
    /// with a second work while the first work's continuation posts, and deterministically by
    /// parking a blocker work on a real Running worker.
    /// Branches B/C/D only occur inside narrow race windows of the real pipeline, so they are
    /// exercised deterministically against real PowerPoolSynchronizationContext instances
    /// (InternalsVisibleTo) whose worker-state input is prepared explicitly.
    ///
    /// Note on InterlockedFlag&lt;T&gt;.TrySet(value, comparand): it succeeds only when the CURRENT
    /// flag value equals the comparand. The fast-path "reservation" in Post is
    /// TrySet(NotAllowed, Allowed), i.e. it consumes an Allowed flag; Worker.SetWork(..., true)
    /// later writes Allowed back, releasing the reservation.
    /// </summary>
    public class ContinuationSchedulingTest
    {
        private readonly ITestOutputHelper _output;

        public ContinuationSchedulingTest(ITestOutputHelper output)
        {
            _output = output;
        }

        // ------------------------------------------------------------------
        // End-to-end: branch A (fast path) through the public API.
        // A single worker is kept busy by a trailing sync work while the async
        // continuations post, so Post observes a Running worker it can reserve.
        // ------------------------------------------------------------------

        [Fact]
        public void TestFastPathBusyWorkerAction()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            const int workCount = 8;
            int doneCount = 0;

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            for (int i = 0; i < workCount; ++i)
            {
                powerPool.QueueWorkItem(async () =>
                {
                    await Task.Yield();
                    await Task.Yield();
                    await Task.Yield();
                });
            }

            // Keeps the only worker busy while the continuations above post,
            // so their drain scheduling goes through the guarded fast path.
            powerPool.QueueWorkItem(() => Interlocked.Increment(ref doneCount));

            powerPool.Wait();

            Assert.Equal(1, doneCount);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.WaitingWorkCount);
            Assert.Equal(0, powerPool.AsyncWorkCount);
        }

        [Fact]
        public void TestFastPathBusyWorkerFunc()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            const int workCount = 8;
            int doneCount = 0;

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            for (int i = 0; i < workCount; ++i)
            {
                powerPool.QueueWorkItem<string>(async () =>
                {
                    await Task.Yield();
                    await Task.Yield();
                    await Task.Yield();
                    return "R";
                });
            }

            powerPool.QueueWorkItem(() => Interlocked.Increment(ref doneCount));

            powerPool.Wait();

            Assert.Equal(1, doneCount);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.WaitingWorkCount);
            Assert.Equal(0, powerPool.AsyncWorkCount);
        }

        // ------------------------------------------------------------------
        // End-to-end: the guard's survival scenarios driven through the public API.
        // When an awaited task completes on a non-pool thread (here: a timer thread),
        // the continuation posts while its only worker has gone idle (branch B input)
        // or, with KeepAliveTime == 0, has been destroyed (worst case of B). Which
        // branch Post actually observes is a race, so completion is asserted, not the branch.
        // ------------------------------------------------------------------

        [Fact]
        public void TestContinuationPostsWhenWorkerIdleAction()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            object p = null;
            object c = null;

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                c = "2";
            });

            powerPool.Wait();

            Assert.Equal("1", p);
            Assert.Equal("2", c);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.WaitingWorkCount);
            Assert.Equal(0, powerPool.AsyncWorkCount);
        }

        [Fact]
        public void TestContinuationPostsWhenWorkerIdleFunc()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            object p = null;
            object c = null;
            string r = null;

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });
            powerPool.QueueWorkItem<string>(async () =>
            {
                p = "1";
                await Task.Delay(100);
                c = "2";
                return "R";
            }, out _, (res) =>
            {
                r = res.Result;
            });

            powerPool.Wait();

            Assert.Equal("1", p);
            Assert.Equal("2", c);
            Assert.Equal("R", r);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.WaitingWorkCount);
            Assert.Equal(0, powerPool.AsyncWorkCount);
        }

        [Fact]
        public void TestContinuationPostsWhenWorkerDestroyedAction()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            object p = null;
            object c = null;

            // With KeepAliveTime == 0 the idle worker is destroyed immediately - the
            // exact state the 2f669b4 guard has to survive without losing the work.
            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption { MinThreads = 0, KeepAliveTime = 0 },
            });
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                c = "2";
            });

            powerPool.Wait();

            Assert.Equal("1", p);
            Assert.Equal("2", c);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.WaitingWorkCount);
            Assert.Equal(0, powerPool.AsyncWorkCount);
        }

        [Fact]
        public void TestContinuationPostsWhenWorkerDestroyedFunc()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            object p = null;
            object c = null;
            string r = null;

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption { MinThreads = 0, KeepAliveTime = 0 },
            });
            powerPool.QueueWorkItem<string>(async () =>
            {
                p = "1";
                await Task.Delay(100);
                c = "2";
                return "R";
            }, out _, (res) =>
            {
                r = res.Result;
            });

            powerPool.Wait();

            Assert.Equal("1", p);
            Assert.Equal("2", c);
            Assert.Equal("R", r);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.WaitingWorkCount);
            Assert.Equal(0, powerPool.AsyncWorkCount);
        }

        // Repeated cycles maximize the chance that every branch is hit at least once
        // across the whole test; completion and zero leftover state are asserted per round.
        [Fact]
        public void TestContinuationSchedulingStressAllBranches()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            const int rounds = 300;

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption { MinThreads = 1, KeepAliveTime = 0 },
            });

            for (int i = 0; i < rounds; ++i)
            {
                int done = 0;
                powerPool.QueueWorkItem(async () =>
                {
                    await Task.Delay(1);
                    await Task.Delay(1);
                    Interlocked.Increment(ref done);
                }, out _);
                powerPool.QueueWorkItem<string>(async () =>
                {
                    await Task.Delay(1);
                    await Task.Delay(1);
                    return "R";
                }, out _);

                powerPool.Wait();

                Assert.Equal(1, done);
                Assert.Equal(0, powerPool.RunningWorkerCount);
                Assert.Equal(0, powerPool.WaitingWorkCount);
                Assert.Equal(0, powerPool.AsyncWorkCount);
            }
        }

        // ------------------------------------------------------------------
        // EnqueueOverflow lazy-init race coverage:
        //     if (Interlocked.CompareExchange(ref _overflow, overflow, null) != null)
        //     {
        //         overflow = _overflow;   // <- the CAS loser reads the winner's queue
        //     }
        // That branch only runs when two threads enter EnqueueOverflow while _overflow is
        // still null and the CAS loses - a nanosecond window with no seam to force it
        // deterministically. So EnqueuePost (the enqueue path of Post, internal for
        // testability) is driven by a barrier-synchronized thread storm on a FRESH context
        // per round (fresh => _overflow null again), maximizing collision probability.
        // The assertion verifies the property that branch exists for: when the race hits,
        // the loser must adopt the winner's queue, so no post is dropped into an orphaned
        // queue. Only the non-generic context is exercised: both context classes carry a
        // line-for-line identical copy of EnqueuePost/EnqueueOverflow.
        // Pool scheduling is not involved: nothing is queued onto real workers.
        // ------------------------------------------------------------------

        [Fact]
        public void TestEnqueueOverflowLazyInitRaceStorm()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            const int rounds = 500;
            const int threads = 4;
            const int postsPerThread = 4;

            using PowerPool pool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            for (int round = 0; round < rounds; ++round)
            {
                PowerPoolSynchronizationContext ctx = (PowerPoolSynchronizationContext)CreateNonGenericCase(pool).Ctx;

                using Barrier barrier = new Barrier(threads);

                Task[] tasks = new Task[threads];
                for (int t = 0; t < threads; ++t)
                {
                    tasks[t] = Task.Run(() =>
                    {
                        barrier.SignalAndWait(20_000);
                        for (int i = 0; i < postsPerThread; ++i)
                        {
                            // The first post per thread claims the single _slot; every other
                            // one goes to EnqueueOverflow, where concurrent first-callers
                            // race the lazy queue initialization.
                            ctx.EnqueuePost(_ => { }, null);
                        }
                    });
                }

                Assert.True(Task.WaitAll(tasks, 20_000), "enqueue threads did not finish");

                // Nothing drains the queues (that is Post's job), so verify no post was lost
                // by counting the enqueued items directly: slot (0 or 1) + overflow must hold
                // every post.
                int inSlot = ctx.GetType()
                    .GetField("_slot", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(ctx) != null ? 1 : 0;
                int inOverflow = ((System.Collections.ICollection)ctx.GetType()
                    .GetField("_overflow", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(ctx)).Count;
                Assert.Equal(threads * postsPerThread, inSlot + inOverflow);
            }
        }

        // ------------------------------------------------------------------
        // Deterministic branch coverage: a real PowerPoolSynchronizationContext
        // bound to a real WorkAction instance on a real pool. Post itself installs
        // the drain action into the work, so driving Post is exactly what the
        // production pipeline does; every worker-state input is prepared explicitly.
        // ------------------------------------------------------------------

        [Fact]
        public void TestPostBranchesDirectlyNonGenericContext()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            RunPostBranchCases(
                CreateNonGenericCase,
                (ctx, continuation) => ctx.Post(_ => continuation(), null));
        }

        [Fact]
        public void TestPostBranchesDirectlyGenericContext()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            RunPostBranchCases(
                CreateGenericCase,
                (ctx, continuation) => ctx.Post(_ => continuation(), null));
        }

        private static (SynchronizationContext Ctx, WorkBase BoundWork) CreateNonGenericCase(PowerPool pool)
        {
            WorkBase work = new WorkAction<object>();
            work.Init(pool, new WorkIDLong(946), new WorkOption(), null);
            PowerPoolSynchronizationContext ctx = new PowerPoolSynchronizationContext(pool, work);
            ctx.SetTask(Task.CompletedTask);
            return (ctx, work);
        }

        private static (SynchronizationContext Ctx, WorkBase BoundWork) CreateGenericCase(PowerPool pool)
        {
            WorkFunc<string> work = new WorkFunc<string>();
            work.Init(pool, new WorkIDLong(946), new WorkOption(), null);
            PowerPoolSynchronizationContext<string> ctx = new PowerPoolSynchronizationContext<string>(pool, work);
            ctx.SetTask(Task.FromResult("R"));
            return (ctx, (WorkBase)work);
        }

        private static void RunPostBranchCases(
            Func<PowerPool, (SynchronizationContext Ctx, WorkBase BoundWork)> createCase,
            Action<SynchronizationContext, Action> post)
        {
            // --- Branch A: reservation OK and worker Running -> worker.SetWork fast path.
            // The blocker work parks a real worker in the Running state, exactly like a
            // worker busy with another work when a continuation posts.
            RunPostBranchCase(
                maxThreads: 1,
                useBlocker: true,
                prepareWorker: (pool, boundWork, parkedWorker) => boundWork.Worker = parkedWorker,
                cleanup: () => { },
                createCase: createCase,
                post: post);

            // --- Branch B: reservation OK but worker NOT Running -> guard falls back to pool.SetWork.
            // The worker sits in the idle queue (state Idle, flag Allowed), which is what Post
            // sees when the awaited task completed on a non-pool thread. The fallback must take
            // it out of the idle queue with the correct bookkeeping and run the drain on it.
            RunPostBranchCase(
                maxThreads: 1,
                useBlocker: false,
                prepareWorker: (pool, boundWork, parkedWorker) => boundWork.Worker = FindIdleWorker(pool),
                cleanup: () => { },
                createCase: createCase,
                post: post);

            // --- Branch C: reservation fails (_canGetWork not Allowed) -> pool.SetWork fallback.
            // ToBeDisabled blocks the Allowed->NotAllowed reservation CAS, mirroring a worker
            // in its idle transition. A second worker (MaxThreads = 2, MinThreads = 2) serves
            // the fallback, otherwise the single worker would stay reserved-and-spinning.
            Worker branchCWorker = null;
            RunPostBranchCase(
                maxThreads: 2,
                useBlocker: false,
                prepareWorker: (pool, boundWork, parkedWorker) =>
                {
                    branchCWorker = FindIdleWorker(pool);
                    boundWork.Worker = branchCWorker;
                    branchCWorker._canGetWork.InterlockedValue = CanGetWork.ToBeDisabled;
                },
                cleanup: () =>
                {
                    // Give the contorted worker back to the pool so disposal can settle it.
                    if (branchCWorker != null)
                    {
                        branchCWorker._canGetWork.InterlockedValue = CanGetWork.Allowed;
                    }
                },
                createCase: createCase,
                post: post);

            // --- Branch D: work not bound to any worker -> pool.SetWork.
            RunPostBranchCase(
                maxThreads: 1,
                useBlocker: false,
                prepareWorker: (pool, boundWork, parkedWorker) => { },
                cleanup: () => { },
                createCase: createCase,
                post: post);
        }

        /// <summary>
        /// One deterministic Post() call against a fresh pool + fresh work:
        /// 1. a real worker is either parked by a blocker work (Running) or idle,
        ///    producing the exact input state of the target branch;
        /// 2. Post is called once - the worker reservation outcome is fully determined;
        /// 3. the posted callback must run (the drain completes through whichever branch
        ///    was taken) and the pool must settle back to zero.
        /// </summary>
        private static void RunPostBranchCase(
            int maxThreads,
            bool useBlocker,
            Action<PowerPool, WorkBase, Worker> prepareWorker,
            Action cleanup,
            Func<PowerPool, (SynchronizationContext Ctx, WorkBase BoundWork)> createCase,
            Action<SynchronizationContext, Action> post)
        {
            PowerPool pool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = maxThreads,
                DestroyThreadOption = new DestroyThreadOption { MinThreads = maxThreads, KeepAliveTime = 0 },
            });
            bool disposed = false;
            try
            {
                Worker parkedWorker = null;
                ManualResetEventSlim releaseBlocker = null;
                if (useBlocker)
                {
                    releaseBlocker = ParkWorkerWithBlocker(pool);
                    parkedWorker = FindWorker(pool, WorkerStates.Running);
                }

                (SynchronizationContext ctx, WorkBase boundWork) = createCase(pool);

                prepareWorker(pool, boundWork, parkedWorker);

                int ran = 0;
                post(ctx, () => Volatile.Write(ref ran, 1));

                // In the blocker scenario the drain is queued behind the blocker work,
                // so the blocker must be released for the fast path to deliver it.
                releaseBlocker?.Set();

                Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref ran) == 1, 20_000),
                    "the posted continuation never ran: the drain was lost in branch scheduling");

                Assert.True(SpinWait.SpinUntil(
                    () => pool.RunningWorkerCount == 0
                        && pool.WaitingWorkCount == 0
                        && pool.IdleWorkerCount == pool.AliveWorkerCount,
                    20_000),
                    "pool did not settle: worker/waiting counters are corrupted");

                cleanup();

                // Every alive worker must be disposable: a worker hijacked while sitting in
                // the idle queue (the exact corruption the 2f669b4 guard prevents) never
                // becomes disposable and makes Dispose spin forever. Bounded so a failure is
                // an assertion, not a hung test run.
                Task<bool> disposeTask = Task.Run(() => { pool.Dispose(); return true; });
                Assert.True(disposeTask.Wait(20_000),
                    "pool.Dispose hung: a worker was left in a non-disposable state (idle-queue corruption)");
                disposed = true;
            }
            finally
            {
                if (!disposed)
                {
                    pool.Dispose();
                }
            }
        }

        /// <summary>
        /// Queues a blocker sync work that parks its worker in the Running state (the
        /// fast-path input) until the returned release event is set.
        /// </summary>
        private static ManualResetEventSlim ParkWorkerWithBlocker(PowerPool pool)
        {
            ManualResetEventSlim started = new ManualResetEventSlim(false);
            ManualResetEventSlim release = new ManualResetEventSlim(false);

            pool.QueueWorkItem(() =>
            {
                started.Set();
                release.Wait();
            });

            Assert.True(SpinWait.SpinUntil(() => started.IsSet, 20_000),
                "the blocker work never started");

            started.Dispose();
            return release;
        }

        private static Worker FindWorker(PowerPool pool, WorkerStates state)
        {
            Worker worker = null;
            Assert.True(SpinWait.SpinUntil(() =>
            {
                foreach (var kv in pool._aliveWorkerDic)
                {
                    Worker w = kv.Value;
                    if (w._workerState == state && (state != WorkerStates.Idle || w._canGetWork == CanGetWork.Allowed))
                    {
                        worker = w;
                        return true;
                    }
                }
                return false;
            }, 20_000), $"no {state} worker found");
            return worker;
        }

        /// <summary>
        /// Finds a worker sitting in the idle state WITHOUT taking it out of the idle queue
        /// (state Idle, can-get-work flag Allowed): the exact branch-B input Post must fall back from.
        /// </summary>
        private static Worker FindIdleWorker(PowerPool pool)
            => FindWorker(pool, WorkerStates.Idle);
    }
}
