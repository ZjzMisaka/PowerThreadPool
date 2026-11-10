using System;
using System.Threading;
using System.Threading.Tasks;
using PowerThreadPool;
using PowerThreadPool.Collections;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;

namespace UnitTest
{
    /// <summary>
    /// Regression tests for async works (TaskCompletionSource != null) that end without
    /// being scheduled to a worker: the early-exit paths must complete the out Task
    /// (TCS) and decrement _asyncWorkCount exactly once, so that the pool can return
    /// to NotRunning and Wait()/WaitAsync() do not hang forever.
    /// </summary>
    public class AsyncWorkEarlyExitTest
    {
        private const int TimeoutMs = 3000;

        /// <summary>
        /// Waits for completion regardless of the final state (canceled/faulted tasks make
        /// Task.Wait throw instead of returning true).
        /// </summary>
        private static bool WaitCompleted(Task task, int millisecondsTimeout = TimeoutMs)
        {
            try
            {
                return task.Wait(millisecondsTimeout);
            }
            catch (AggregateException)
            {
                return true;
            }
        }

        private static WorkID QueueFailedWork(PowerPool pool, ManualResetEventSlim started = null)
        {
            return pool.QueueWorkItem(new Func<int>(() =>
            {
                started?.Set();
                throw new Exception("boom");
            }));
        }

        // --- Path 1: dependency already failed when registering (OnPrecedingWorkNotSuccessfullyCompletedWhenRegister) ---

        [Fact]
        public void TestAsyncWorkRegisteredAfterFailedDependency_CompletesTaskAndDrainsPool()
        {
            PowerPool pool = new PowerPool(new PowerPoolOption { ShouldStoreResult = true });

            WorkID idA = pool.QueueWorkItem(new Func<int>(() => throw new Exception("boom")));
            pool.Wait(idA);

            Task taskB = null;
            pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskB, new WorkOption
            {
                Dependencies = new ConcurrentSet<WorkID> { idA },
            });

            Assert.True(WaitCompleted(taskB), "out Task never completed after dependency already failed");
            Assert.True(taskB.IsFaulted, $"expected faulted, got {taskB.Status}");

            pool.Wait();
            Assert.Equal(0, pool.AsyncWorkCount);
        }

        // --- Path 2: dependency fails while the async work is waiting (CauseAcquiredWorksToFail) ---

        [Fact]
        public void TestAsyncWorkWaitingOnFailingDependency_CompletesTaskAndDrainsPool()
        {
            PowerPool pool = new PowerPool(new PowerPoolOption { ShouldStoreResult = true });

            ManualResetEventSlim aStarted = new ManualResetEventSlim(false);
            WorkID idA = QueueFailedWork(pool, aStarted);
            Assert.True(aStarted.Wait(TimeoutMs));
            Thread.Sleep(50); // let B register while A is in flight

            Task taskB = null;
            pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskB, new WorkOption
            {
                Dependencies = new ConcurrentSet<WorkID> { idA },
            });

            Assert.True(WaitCompleted(taskB), "out Task never completed after dependency failed");
            Assert.True(taskB.IsFaulted, $"expected faulted, got {taskB.Status}");

            pool.Wait();
            Assert.Equal(0, pool.AsyncWorkCount);
        }

        // --- Path 3: Stop() -> dependency controller Cancel() ---

        [Fact]
        public void TestStopWithPendingAsyncDependencyWork_CompletesTaskAndDrainsPool()
        {
            PowerPool pool = new PowerPool(new PowerPoolOption { ShouldStoreResult = true });

            ManualResetEventSlim aStarted = new ManualResetEventSlim(false);
            WorkID idA = pool.QueueWorkItem(() =>
            {
                aStarted.Set();
                Thread.Sleep(400);
            });
            Assert.True(aStarted.Wait(TimeoutMs));

            Task taskB = null;
            pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskB, new WorkOption
            {
                Dependencies = new ConcurrentSet<WorkID> { idA },
            });

            pool.Stop();

            Assert.True(WaitCompleted(taskB), "out Task never completed after Stop()");
            Assert.True(taskB.IsCanceled, $"expected canceled, got {taskB.Status}");

            pool.Wait();
            Assert.Equal(0, pool.AsyncWorkCount);
        }

        // --- Path 4: Cancel(id) taking the dependency branch ---

        [Fact]
        public void TestCancelByIdAsyncDependencyWork_CompletesTaskAndDrainsPool()
        {
            PowerPool pool = new PowerPool(new PowerPoolOption { ShouldStoreResult = true });

            ManualResetEventSlim aStarted = new ManualResetEventSlim(false);
            WorkID idA = pool.QueueWorkItem(() =>
            {
                aStarted.Set();
                Thread.Sleep(400);
            });
            Assert.True(aStarted.Wait(TimeoutMs));

            Task taskB = null;
            WorkID idB = pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskB, new WorkOption
            {
                Dependencies = new ConcurrentSet<WorkID> { idA },
            });

            Assert.True(pool.Cancel(idB));

            Assert.True(WaitCompleted(taskB), "out Task never completed after Cancel(id) on dependency-registered work");
            Assert.True(taskB.IsCanceled, $"expected canceled, got {taskB.Status}");

            pool.Wait();
            Assert.Equal(0, pool.AsyncWorkCount);
        }

        // --- Side finding 1: Cancel(id) taking the suspended branch (StartSuspended) ---

        [Fact]
        public void TestCancelByIdSuspendedAsyncWork_CompletesTaskAndDrainsPool()
        {
            PowerPool pool = new PowerPool(new PowerPoolOption
            {
                ShouldStoreResult = true,
                StartSuspended = true,
            });

            Task taskB = null;
            WorkID idB = pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskB);

            Assert.True(pool.Cancel(idB));

            Assert.True(WaitCompleted(taskB), "out Task never completed after Cancel(id) on suspended work");
            Assert.True(taskB.IsCanceled, $"expected canceled, got {taskB.Status}");
        }

        // --- Side finding 2: Stop() -> Cancel() -> _stopSuspendedWork cleared ---

        [Fact]
        public void TestStopThenCancelStopSuspendedAsyncWork_CompletesTask()
        {
            PowerPool pool = new PowerPool(new PowerPoolOption { ShouldStoreResult = true });

            ManualResetEventSlim aStarted = new ManualResetEventSlim(false);
            WorkID idA = pool.QueueWorkItem(() =>
            {
                aStarted.Set();
                Thread.Sleep(300);
            });
            Assert.True(aStarted.Wait(TimeoutMs));

            pool.Stop(); // pool is now stopping

            Task taskB = null;
            pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskB); // queued while stopping -> goes into _stopSuspendedWork

            pool.Cancel(); // clears _stopSuspendedWork

            Assert.True(WaitCompleted(taskB), "out Task never completed after Cancel() on stop-suspended work");
            Assert.True(taskB.IsCanceled, $"expected canceled, got {taskB.Status}");
        }

        // --- Side finding 3: rejection Discard path (OnWorkDiscarded) ---
        // ThreadQueueLimit = 0: while the single thread is busy, any newly submitted work
        // is rejected and (with RejectType.Discard) the submitted work itself is discarded.

        [Fact]
        public void TestDiscardedAsyncWork_CompletesTaskAndDrainsPool()
        {
            PowerPool pool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = 1,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.Discard,
                    ThreadQueueLimit = 0,
                }
            });

            Task taskKept = null;
            pool.QueueWorkItem(async () =>
            {
                Thread.Sleep(300);
            }, out taskKept); // running: occupies the only thread

            WorkID discardedID = null;
            pool.WorkDiscarded += (s, e) => discardedID = e.ID;

            Task taskDiscarded = null;
            WorkID idDiscarded = pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskDiscarded); // rejected while the thread is busy

            Assert.True(ThreadIsBusy(pool), "precondition: the pool must be busy so the second submission is rejected");
            Assert.Equal(idDiscarded, discardedID);

            Assert.True(WaitCompleted(taskDiscarded), "out Task never completed after being discarded");
            Assert.True(taskDiscarded.IsCanceled, $"expected canceled, got {taskDiscarded.Status}");

            Assert.True(WaitCompleted(taskKept));

            pool.Wait();
            Assert.Equal(0, pool.AsyncWorkCount);
        }

        private static bool ThreadIsBusy(PowerPool pool)
        {
            // The first work sleeps 300ms; waiting a bit guarantees the worker picked it up.
            Thread.Sleep(100);
            return pool.RunningWorkerCount >= 1;
        }

        // --- Side finding 4: rejection Abort path (OnRejected) ---

        [Fact]
        public void TestAbortedAsyncWork_CompletesTaskAndDrainsPool()
        {
            PowerPool pool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = 1,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.Abort,
                    ThreadQueueLimit = 1,
                }
            });

            Task taskKept = null;
            pool.QueueWorkItem(async () =>
            {
                Thread.Sleep(300);
            }, out taskKept);

            Task taskQueued = null;
            pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskQueued);

            Task taskAborted = null;
            Assert.Throws<WorkRejectedException>(() => pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskAborted));

            Assert.True(WaitCompleted(taskAborted), "out Task never completed after Abort rejection");
            Assert.True(taskAborted.IsCanceled, $"expected canceled, got {taskAborted.Status}");

            Assert.True(WaitCompleted(taskKept));
            Assert.True(WaitCompleted(taskQueued));

            pool.Wait();
            Assert.Equal(0, pool.AsyncWorkCount);
        }

        // --- Sanity: normal dependency flow still works (no double-finalize regression) ---

        [Fact]
        public void TestAsyncWorkDependencyNormalFlow_StillSucceeds()
        {
            PowerPool pool = new PowerPool(new PowerPoolOption { ShouldStoreResult = true });

            Task taskA = null;
            WorkID idA = pool.QueueWorkItem(async () =>
            {
                await Task.Delay(100);
            }, out taskA);

            Task taskB = null;
            pool.QueueWorkItem(async () =>
            {
                await Task.Delay(50);
            }, out taskB, new WorkOption
            {
                Dependencies = new ConcurrentSet<WorkID> { idA },
            });

            Assert.True(WaitCompleted(taskA));
            Assert.True(WaitCompleted(taskB));
            Assert.Equal(TaskStatus.RanToCompletion, taskB.Status);

            pool.Wait();
            Assert.Equal(0, pool.AsyncWorkCount);
        }
    }
}
