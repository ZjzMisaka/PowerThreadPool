using System.Diagnostics;
using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
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

        PowerPool _powerPool;

        [Fact(Timeout = 15 * 60 * 1000)]
        public async Task StressTest1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            await Task.Run(async () =>
            {
                _powerPool = new PowerPool(new PowerPoolOption() { DestroyThreadOption = new DestroyThreadOption() });

                int totalTasks = 1000000;

                for (int i = 0; i < 100; ++i)
                {
                    int doneCount = 0;
                    int failedCount = 0;

                    _powerPool.EnablePoolIdleCheck = false;

                    Task[] tasks = Enumerable.Range(0, totalTasks).Select(i =>
                        Task.Run(() =>
                        {
                            string workId = _powerPool.QueueWorkItem(() =>
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

                    _powerPool.EnablePoolIdleCheck = true;

                    await _powerPool.WaitAsync();

                    string errLog = "";
                    errLog = "doneCount: " + doneCount + "/" + totalTasks + " | failedCount: " + failedCount + " | powerPool.RunningWorkerCount: " + _powerPool.RunningWorkerCount + " | powerPool.WaitingWorkCount: " + _powerPool.WaitingWorkCount + " | powerPool.IdleWorkerCount: " + _powerPool.IdleWorkerCount + " | powerPool.MaxThreads: " + _powerPool.PowerPoolOption.MaxThreads;
                    if (totalTasks != doneCount || 0 != failedCount || 0 != _powerPool.RunningWorkerCount || 0 != _powerPool.WaitingWorkCount || _powerPool.IdleWorkerCount == 0)
                    {
                        Assert.Fail(errLog);
                    }
                }
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

                _powerPool = new PowerPool();
                _powerPool.PowerPoolOption = new PowerPoolOption()
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
                            string id = _powerPool.QueueWorkItem(() => { throw new Exception(); });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else if (r >= 95 && r <= 99)
                        {
                            string id = _powerPool.QueueWorkItem(() =>
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
                            string id = _powerPool.QueueWorkItem(() =>
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
                            string id = _powerPool.QueueWorkItem(() =>
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

                    if (runCount != _powerPool.WaitingWorkCount)
                    {
                        Assert.Fail();
                    }

                    _powerPool.Start();

                    int r1 = random.Next(0, 101);
                    if (r1 >= 81 && r1 <= 100)
                    {
                        _powerPool.Stop();
                        await _powerPool.WaitAsync();
                        if (_powerPool.RunningWorkerCount > 0 || _powerPool.WaitingWorkCount > 0)
                        {
                            Assert.Fail();
                        }
                    }
                    else if (r1 >= 61 && r1 <= 80)
                    {
                        _powerPool.Stop(true);
                        await _powerPool.WaitAsync();
                        if (_powerPool.RunningWorkerCount > 0 || _powerPool.WaitingWorkCount > 0)
                        {
                            Assert.Fail();
                        }
                    }
                    else
                    {
                        await _powerPool.WaitAsync();
                        if (_powerPool.RunningWorkerCount > 0 || _powerPool.WaitingWorkCount > 0 || runCount != doneCount)
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
            });
        }

        [Fact(Timeout = 15 * 60 * 1000)]
        public async Task StressTest3()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            await Task.Run(async () =>
            {
                _powerPool = new PowerPool(new PowerPoolOption() { DestroyThreadOption = new DestroyThreadOption() });

                int totalTasks = 100;
                int doneCount = 0;
                for (int i = 0; i < 1000000; ++i)
                {
                    _powerPool.EnablePoolIdleCheck = false;

                    Task[] tasks = Enumerable.Range(0, totalTasks).Select(i =>
                        Task.Run(() =>
                        {
                            _powerPool.QueueWorkItem(() =>
                            {
                                Interlocked.Increment(ref doneCount);
                            });
                        })
                    ).ToArray();

                    await Task.WhenAll(tasks);

                    _powerPool.EnablePoolIdleCheck = true;

                    await _powerPool.WaitAsync();
                }

                string errLog = "";
                errLog = "doneCount: " + doneCount + "/" + 100 * 1000000 + " | powerPool.RunningWorkerCount: " + _powerPool.RunningWorkerCount + " | powerPool.WaitingWorkCount: " + _powerPool.WaitingWorkCount + " | powerPool.IdleWorkerCount: " + _powerPool.IdleWorkerCount + " | powerPool.MaxThreads: " + _powerPool.PowerPoolOption.MaxThreads;
                if (100 * 1000000 != doneCount || 0 != _powerPool.RunningWorkerCount || 0 != _powerPool.WaitingWorkCount || _powerPool.IdleWorkerCount == 0)
                {
                    Assert.Fail(errLog);
                }
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

                _powerPool = new PowerPool();
                _powerPool.PowerPoolOption = new PowerPoolOption()
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
                            string id = _powerPool.QueueWorkItem(() => { throw new Exception(); });
                            if (id == null)
                            {
                                Assert.Fail("PoolStopping");
                            }
                        }
                        else if (r >= 95 && r <= 99)
                        {
                            string id = _powerPool.QueueWorkItem(() =>
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
                            string id = _powerPool.QueueWorkItem(() =>
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
                            string id = _powerPool.QueueWorkItem(() =>
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

                    if (runCount != _powerPool.WaitingWorkCount)
                    {
                        Assert.Fail();
                    }

                    _powerPool.Start();

                    int r1 = random.Next(0, 101);
                    if (r1 >= 81 && r1 <= 100)
                    {
                        _powerPool.Stop();
                        await _powerPool.WaitAsync();
                        if (_powerPool.RunningWorkerCount > 0 || _powerPool.WaitingWorkCount > 0)
                        {
                            Assert.Fail();
                        }
                    }
                    else if (r1 >= 61 && r1 <= 80)
                    {
                        _powerPool.Stop(true);
                        await _powerPool.WaitAsync();
                        if (_powerPool.RunningWorkerCount > 0 || _powerPool.WaitingWorkCount > 0)
                        {
                            Assert.Fail();
                        }
                    }
                    else
                    {
                        await _powerPool.WaitAsync();
                        if (_powerPool.RunningWorkerCount > 0 || _powerPool.WaitingWorkCount > 0 || runCount != doneCount)
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
            });
        }

        private void Sleep(int ms)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < ms)
            {
                _powerPool.StopIfRequested();
            }

            stopwatch.Stop();
        }

        private long GetNowSs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
