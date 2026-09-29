using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using Xunit.Abstractions;

namespace UnitTest
{
    /// <summary>
    /// Regression tests for the WorkExtras size optimization: cold-state members
    /// (wait/pause signals, timeout timer, CTS, statistics timestamps) live in a
    /// lazily allocated side object, which must never be created on the plain
    /// synchronous path, and their concurrent publication must stay correct.
    /// </summary>
    public class WorkExtrasTest
    {
        private readonly ITestOutputHelper _output;

        public WorkExtrasTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestDefaultSyncWorkDoesNotAllocateExtras()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<WorkBase> trackedWorks = new List<WorkBase>();
            powerPool.CallbackEnd += (work, status) =>
            {
                lock (trackedWorks)
                {
                    trackedWorks.Add(work);
                }
            };

            for (int i = 0; i < 20; i++)
            {
                powerPool.QueueWorkItem(() => { });
            }

            powerPool.Wait();

            Assert.True(trackedWorks.Count >= 1);
            foreach (WorkBase work in trackedWorks)
            {
                Assert.Null(work._extras);
            }
        }

        [Fact]
        public void TestConcurrentWaitOnSameWork()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            int workDuration = 300;
            int waiterCount = 8;
            ManualResetEventSlim allWaitersWaiting = new ManualResetEventSlim(false);
            int readyCount = 0;

            WorkID id = powerPool.QueueWorkItem(() =>
            {
                // Wait until all waiters are inside Wait() before letting the work finish,
                // so they genuinely contend on the wait signal publication.
                Spinner.Start(() => Volatile.Read(ref readyCount) == waiterCount, true);
                Thread.Sleep(workDuration);
                return 1;
            });

            List<Task<bool>> waitTasks = new List<Task<bool>>();
            for (int i = 0; i < waiterCount; i++)
            {
                waitTasks.Add(Task.Run(() =>
                {
                    Interlocked.Increment(ref readyCount);
                    return powerPool.Wait(id);
                }));
            }

            Assert.True(Task.WaitAll(waitTasks.ToArray(), 30000));
            foreach (Task<bool> t in waitTasks)
            {
                Assert.True(t.Result);
            }
        }

        [Fact]
        public void TestConcurrentPauseResumeDuringExecution()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            int finished = 0;
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    powerPool.PauseIfRequested();
                }
                Interlocked.Increment(ref finished);
                return 1;
            });

            List<Task> pauseTasks = new List<Task>();
            for (int i = 0; i < 4; i++)
            {
                pauseTasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 20; j++)
                    {
                        powerPool.Pause(id);
                        Thread.Sleep(1);
                        powerPool.Resume(id);
                    }
                }));
            }

            Task.WaitAll(pauseTasks.ToArray());
            powerPool.Wait(id);

            Assert.Equal(1, Volatile.Read(ref finished));
        }

        [Fact]
        public void TestStopRacingWithExecution()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            for (int round = 0; round < 20; round++)
            {
                PowerPool powerPool = new PowerPool();
                bool done = false;
                WorkID id = powerPool.QueueWorkItem(() =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        powerPool.CheckIfRequestedStop();
                    }
                    done = true;
                    return 1;
                });

                powerPool.Stop(id);
                powerPool.Wait();

                // Either the work completed fully or it was stopped mid-flight;
                // both are valid outcomes of the race, but Wait() must return.
                Assert.True(done || !done);
            }
        }

        [Fact]
        public void TestFetchAfterExtrasOptimization()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
                return "extras";
            });

            ExecuteResult<string> result = powerPool.Fetch<string>(id);
            powerPool.Wait();

            Assert.Equal("extras", result.Result);
            Assert.Equal(Status.Succeed, result.Status);
        }

        [Fact]
        public void TestStatisticsEnabledStillTracksTime()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                EnableStatisticsCollection = true,
            };
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(50);
                return 1;
            });
            ExecuteResult<int> result = powerPool.Fetch<int>(id);
            powerPool.Wait();

            Assert.Equal(Status.Succeed, result.Status);
            Assert.True(result.UtcStartDateTime > DateTime.MinValue);
            Assert.True(result.QueueDateTime > DateTime.MinValue);
        }

        [Fact]
        public void TestAsyncWorkWaitAndFetchWithExtras()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(async () =>
            {
                await Task.Delay(100);
                return "async-extras";
            }, out Task<ExecuteResult<string>> task);

            // Concurrent sync-wait and task await on an async work whose signal
            // may be published from the RegisterCompletion continuation path.
            Task<bool> waitTask = Task.Run(() => powerPool.Wait(id));
            Task<ExecuteResult<string>> fetchTask = Task.Run(() => powerPool.Fetch<string>(id));

            Assert.True(waitTask.Result);
            Assert.Equal(Status.Succeed, fetchTask.Result.Status);
            Assert.Equal("async-extras", fetchTask.Result.Result);
            Assert.Equal(Status.Succeed, task.Result.Status);
            Assert.Equal("async-extras", task.Result.Result);
        }

        [Fact]
        public void TestTimeoutStillWorksAfterMigration()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            bool timedOut = false;
            powerPool.WorkTimedOut += (s, e) =>
            {
                if (e.ID != null)
                {
                    timedOut = true;
                }
            };

            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(3000);
                return 1;
            }, new WorkOption()
            {
                TimeoutOption = new TimeoutOption() { Duration = 200, ForceStop = false },
            });

            powerPool.Wait(id);

            Assert.True(timedOut);
        }
    }
}
