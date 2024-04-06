using PowerThreadPool;
using PowerThreadPool.Option;
using System.Diagnostics;

namespace UnitTest
{
    public class StressTest
    {
        PowerPool powerPool;

        [Fact]
        public async Task StressTest1()
        {
            powerPool = new PowerPool(new PowerPoolOption() { DestroyThreadOption = new DestroyThreadOption() });

            int totalTasks = 500000;

            for (int i = 0; i < 100; ++i)
            {
                int doneCount = 0;
                int failedCount = 0;

                Task[] tasks = Enumerable.Range(0, totalTasks).Select(i =>
                    Task.Run(() =>
                    {
                        string workId = powerPool.QueueWorkItem(() =>
                        {
                        }, (res) =>
                        {
                            if (res.Status == Status.Failed)
                            {
                                Interlocked.Increment(ref failedCount);
                            }
                            Interlocked.Increment(ref doneCount);
                        });
                        Assert.NotNull(workId);
                    })
                ).ToArray();

                await Task.WhenAll(tasks);

                await powerPool.WaitAsync();

                string errLog = "";
                errLog = "doneCount: " + doneCount + "/" + totalTasks + " | failedCount: " + failedCount + " | powerPool.RunningWorkerCount: " + powerPool.RunningWorkerCount + " | powerPool.WaitingWorkCount: " + powerPool.WaitingWorkCount + " | powerPool.IdleWorkerCount: " + powerPool.IdleWorkerCount;
                if (totalTasks != doneCount || 0 != failedCount || 0 != powerPool.RunningWorkerCount || 0 != powerPool.WaitingWorkCount || powerPool.IdleWorkerCount == 0)
                {
                    Assert.Fail(errLog);
                }
            }
        }

        [Fact]
        public async Task StressTest2()
        {
            Random random = new Random();
            bool run = false;
            int doneCount = 0;

            powerPool = new PowerPool();
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
                if (GetNowSs() -start >= 300000)
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
                        string id = powerPool.QueueWorkItem(() => { throw new Exception(); });
                        if (id == null)
                        {
                            Assert.Fail("PoolStopping");
                        }
                    }
                    else if (r >= 95 && r <= 99)
                    {
                        string id = powerPool.QueueWorkItem(() =>
                        {
                            Sleep(10000);
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
                        string id = powerPool.QueueWorkItem(() =>
                        {
                            Sleep(30000);
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
                    else
                    {
                        string id = powerPool.QueueWorkItem(() =>
                        {
                            Sleep(random.Next(500, 1000));
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
                    await powerPool.StopAsync();
                    await powerPool.WaitAsync();
                    if (powerPool.RunningWorkerCount > 0 || powerPool.WaitingWorkCount > 0)
                    {
                        Assert.Fail();
                    }
                }
                else if (r1 >= 61 && r1 <= 80)
                {
                    await powerPool.StopAsync(true);
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
                    Sleep(random.Next(0, 1500));
                }
            }
        }

        [Fact]
        public async Task StressTest3()
        {
            powerPool = new PowerPool(new PowerPoolOption() { DestroyThreadOption = new DestroyThreadOption() });

            int totalTasks = 100;
            int doneCount = 0;
            for (int i = 0; i < 300000; ++i)
            {
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

                await powerPool.WaitAsync();
            }

            string errLog = "";
            errLog = "doneCount: " + doneCount + "/" + 100 * 300000 + " | powerPool.RunningWorkerCount: " + powerPool.RunningWorkerCount + " | powerPool.WaitingWorkCount: " + powerPool.WaitingWorkCount + " | powerPool.IdleWorkerCount: " + powerPool.IdleWorkerCount;
            if (100 * 300000 != doneCount || 0 != powerPool.RunningWorkerCount || 0!= powerPool.WaitingWorkCount || powerPool.IdleWorkerCount == 0)
            {
                Assert.Fail(errLog);
            }
        }

        private void Sleep(int ms)
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
