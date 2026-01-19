using System.Diagnostics;
using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using Xunit.Abstractions;

namespace UnitTest
{
    public class StressTest
    {
        private readonly ITestOutputHelper _output;

        public StressTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Timeout = 15 * 60 * 1000)]
        public async Task StressTest1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            await Task.Run(async () =>
            {
                PowerPool powerPool = new PowerPool();

                int totalTasks = 1000000;

                for (int i = 0; i < 100; ++i)
                {
                    int doneCount = 0;
                    int failedCount = 0;

                    powerPool.EnablePoolIdleCheck = false;

                    Task[] tasks = Enumerable.Range(0, totalTasks).Select(i =>
                        Task.Run(() =>
                        {
                            WorkID workId = powerPool.QueueWorkItem(() =>
                            {
                            }, (res) =>
                            {
                                if (res.Status == Status.Failed)
                                {
                                    Interlocked.Increment(ref failedCount);
                                }
                                Interlocked.Increment(ref doneCount);
                            });
                            Assert.False(workId == null);
                        })
                    ).ToArray();

                    await Task.WhenAll(tasks);

                    powerPool.EnablePoolIdleCheck = true;

                    powerPool.Wait();

                    string errLog = "";
                    errLog = "doneCount: " + doneCount + "/" + totalTasks + " | failedCount: " + failedCount + " | powerPool.RunningWorkerCount: " + powerPool.RunningWorkerCount + " | powerPool.WaitingWorkCount: " + powerPool.WaitingWorkCount + " | powerPool.IdleWorkerCount: " + powerPool.IdleWorkerCount + " | powerPool.AliveWorkerCount: " + powerPool.AliveWorkerCount + " | powerPool.MaxThreads: " + powerPool.PowerPoolOption.MaxThreads;
                    if (totalTasks != doneCount || 0 != failedCount || 0 != powerPool.RunningWorkerCount || 0 != powerPool.WaitingWorkCount || powerPool.IdleWorkerCount == 0)
                    {
                        Assert.Fail(errLog + " | PoolRunning: " + powerPool.PoolRunning);
                    }
                }

                powerPool.Dispose();
            });
        }

        [Fact(Timeout = 15 * 60 * 1000)]
        public async Task StressTest2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            await Task.Run(async () =>
            {
                Random random = new Random();
                bool run = false;
                int doneCount = 0;

                PowerPool powerPool = new PowerPool();
                powerPool.PowerPoolOption = new PowerPoolOption()
                {
                    MaxThreads = 8,
                    DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 1000 },
                    StartSuspended = true,
                    DefaultCallback = (res) =>
                    {
                        Interlocked.Increment(ref doneCount);
                    },
                };

                long start = GetNowSs();
                run = true;
                while (run)
                {
                    if (GetNowSs() - start >= 300000)
                    {
                        break;
                    }

                    int runCount = random.Next(10, 200);
                    doneCount = 0;
                    for (int i = 0; i < runCount; ++i)
                    {
                        int r = random.Next(0, 101);
                        if (r == 100)
                        {
                            WorkID id = powerPool.QueueWorkItem(() => { throw new Exception(); });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else if (r >= 95 && r <= 99)
                        {
                            WorkID id = powerPool.QueueWorkItem(() =>
                            {
                                Sleep(10000, powerPool);
                                int r1 = random.Next(0, 101);
                                if (r1 >= 100 && r1 <= 100)
                                {
                                    Thread.Sleep(1);
                                }
                            });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else if (r >= 94 && r <= 94)
                        {
                            WorkID id = powerPool.QueueWorkItem(() =>
                            {
                                Sleep(30000, powerPool);
                                int r1 = random.Next(0, 101);
                                if (r1 >= 100 && r1 <= 100)
                                {
                                    Thread.Sleep(1);
                                }
                            });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else if (r >= 70 && r <= 93)
                        {
                            WorkID id = powerPool.QueueWorkItem(async () =>
                            {
                                await Task.Delay(random.Next(200, 600));
                                int r1 = random.Next(0, 101);
                                if (r1 >= 100 && r1 <= 100)
                                {
                                    await Task.Delay(1);
                                }
                                await Task.Delay(random.Next(200, 600));
                                await Task.Delay(random.Next(200, 600));
                            });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else
                        {
                            WorkID id = powerPool.QueueWorkItem(() =>
                            {
                                Sleep(random.Next(500, 1000), powerPool);
                                int r1 = random.Next(0, 101);
                                if (r1 >= 100 && r1 <= 100)
                                {
                                    Thread.Sleep(1);
                                }
                            });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                    }

                    Thread.Yield();

                    if (runCount != powerPool.WaitingWorkCount)
                    {
                        Assert.Fail();
                    }

                    powerPool.Start();

                    int r1 = random.Next(0, 101);
                    if (r1 >= 81 && r1 <= 100)
                    {
                        powerPool.Stop();
                        await powerPool.WaitAsync();
                        if (powerPool.RunningWorkerCount > 0 || powerPool.WaitingWorkCount > 0)
                        {
                            Assert.Fail();
                        }
                    }
                    else
                    {
                        await powerPool.WaitAsync();
                        if (powerPool.RunningWorkerCount > 0 || powerPool.WaitingWorkCount > 0 || runCount != doneCount)
                        {
                            Assert.Fail();
                        }
                    }
                    if (r1 >= 81 && r1 <= 100)
                    {
                    }
                    else
                    {
                        Sleep(random.Next(0, 1500), powerPool);
                    }
                }

                powerPool.Dispose();
            });
        }

        [Fact(Timeout = 15 * 60 * 1000)]
        public async Task StressTest3()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            await Task.Run(async () =>
            {
                PowerPool powerPool = new PowerPool();

                int totalTasks = 100;
                int doneCount = 0;
                for (int i = 0; i < 1000000; ++i)
                {
                    powerPool.EnablePoolIdleCheck = false;

                    Task[] tasks = Enumerable.Range(0, totalTasks).Select(i =>
                        Task.Run(() =>
                        {
                            powerPool.QueueWorkItem(() =>
                            {
                                Interlocked.Increment(ref doneCount);
                            });
                        })
                    ).ToArray();

                    await Task.WhenAll(tasks);

                    powerPool.EnablePoolIdleCheck = true;

                    powerPool.Wait();
                }

                string errLog = "";
                errLog = "doneCount: " + doneCount + "/" + 100 * 1000000 + " | powerPool.RunningWorkerCount: " + powerPool.RunningWorkerCount + " | powerPool.WaitingWorkCount: " + powerPool.WaitingWorkCount + " | powerPool.IdleWorkerCount: " + powerPool.IdleWorkerCount + " | powerPool.AliveWorkerCount: " + powerPool.AliveWorkerCount + " | powerPool.MaxThreads: " + powerPool.PowerPoolOption.MaxThreads;
                if (100 * 1000000 != doneCount || 0 != powerPool.RunningWorkerCount || 0 != powerPool.WaitingWorkCount || powerPool.IdleWorkerCount == 0)
                {
                    Assert.Fail(errLog + " | PoolRunning: " + powerPool.PoolRunning);
                }

                powerPool.Dispose();
            });
        }

        [Fact(Timeout = 15 * 60 * 1000)]
        public async Task StressTest4()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            await Task.Run(async () =>
            {
                Random random = new Random();
                bool run = false;
                int doneCount = 0;

                PowerPool powerPool = new PowerPool();
                powerPool.PowerPoolOption = new PowerPoolOption()
                {
                    MaxThreads = 8,
                    DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 1000 },
                    StartSuspended = true,
                    DefaultCallback = (res) =>
                    {
                        Interlocked.Increment(ref doneCount);
                    },
                    QueueType = QueueType.LIFO
                };

                long start = GetNowSs();
                run = true;
                while (run)
                {
                    if (GetNowSs() - start >= 300000)
                    {
                        break;
                    }

                    int runCount = random.Next(10, 200);
                    doneCount = 0;
                    for (int i = 0; i < runCount; ++i)
                    {
                        int r = random.Next(0, 101);
                        if (r == 100)
                        {
                            WorkID id = powerPool.QueueWorkItem(() => { throw new Exception(); });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else if (r >= 95 && r <= 99)
                        {
                            WorkID id = powerPool.QueueWorkItem(() =>
                            {
                                Sleep(10000, powerPool);
                                int r1 = random.Next(0, 101);
                                if (r1 >= 100 && r1 <= 100)
                                {
                                    Thread.Sleep(1);
                                }
                            });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else if (r >= 94 && r <= 94)
                        {
                            WorkID id = powerPool.QueueWorkItem(() =>
                            {
                                Sleep(30000, powerPool);
                                int r1 = random.Next(0, 101);
                                if (r1 >= 100 && r1 <= 100)
                                {
                                    Thread.Sleep(1);
                                }
                            });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else if (r >= 70 && r <= 93)
                        {
                            WorkID id = powerPool.QueueWorkItem(async () =>
                            {
                                await Task.Delay(random.Next(200, 600));
                                int r1 = random.Next(0, 101);
                                if (r1 >= 100 && r1 <= 100)
                                {
                                    await Task.Delay(1);
                                }
                                await Task.Delay(random.Next(200, 600));
                                await Task.Delay(random.Next(200, 600));
                            });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else
                        {
                            WorkID id = powerPool.QueueWorkItem(() =>
                            {
                                Sleep(random.Next(500, 1000), powerPool);
                                int r1 = random.Next(0, 101);
                                if (r1 >= 100 && r1 <= 100)
                                {
                                    Thread.Sleep(1);
                                }
                            });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                    }

                    Thread.Yield();

                    if (runCount != powerPool.WaitingWorkCount)
                    {
                        Assert.Fail();
                    }

                    powerPool.Start();

                    int r1 = random.Next(0, 101);
                    if (r1 >= 81 && r1 <= 100)
                    {
                        powerPool.Stop();
                        await powerPool.WaitAsync();
                        if (powerPool.RunningWorkerCount > 0 || powerPool.WaitingWorkCount > 0)
                        {
                            Assert.Fail();
                        }
                    }
                    else
                    {
                        await powerPool.WaitAsync();
                        if (powerPool.RunningWorkerCount > 0 || powerPool.WaitingWorkCount > 0 || runCount != doneCount)
                        {
                            Assert.Fail();
                        }
                    }
                    if (r1 >= 81 && r1 <= 100)
                    {
                    }
                    else
                    {
                        Sleep(random.Next(0, 1500), powerPool);
                    }
                }

                await powerPool.WaitAsync();

                powerPool.Dispose();
            });
        }

        private void Sleep(int ms, PowerPool powerPool)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < ms)
            {
                powerPool.StopIfRequested();
            }

            stopwatch.Stop();
        }

        private long GetNowSs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
