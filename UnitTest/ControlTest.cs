using System.Reflection;
using PowerThreadPool;
#if DEBUG
using PowerThreadPool.Helpers.LockFree;
#endif
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using Xunit.Abstractions;

namespace UnitTest
{
    public class ControlTest
    {
        private readonly ITestOutputHelper _output;

        public ControlTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private long GetNowSs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        [Fact]
        public void TestPauseAll()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();
            object lockObj = new object();
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                Thread.Sleep(1000);
                powerPool.PauseIfRequested();
                return GetNowSs() - start;
            }, (res) =>
            {
                lock (lockObj)
                {
                    logList.Add(res.Result);
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                Thread.Sleep(1000);
                powerPool.PauseIfRequested();
                return GetNowSs() - start;
            }, (res) =>
            {
                lock (lockObj)
                {
                    logList.Add(res.Result);
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                Thread.Sleep(1000);
                powerPool.PauseIfRequested();
                return GetNowSs() - start;
            }, (res) =>
            {
                lock (lockObj)
                {
                    logList.Add(res.Result);
                }
            });
            Thread.Sleep(500);
            powerPool.Pause();
            Thread.Sleep(1000);
            powerPool.Resume();
            powerPool.Wait();

            Assert.Collection<long>(logList,
                item => Assert.True(item >= 1490),
                item => Assert.True(item >= 1490),
                item => Assert.True(item >= 1490)
            );
        }

        [Fact]
        public void TestPauseByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work0 END");
            });
            Thread.Sleep(200);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work1 END");
            });
            Thread.Sleep(200);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work2 END");
            });
            Thread.Sleep(50);
            bool pauseRes = powerPool.Pause(id);
            Assert.True(pauseRes);
            Thread.Sleep(1000);
            bool resumeRes = powerPool.Resume(id);
            Assert.True(resumeRes);
            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("Work0 END", item),
                item => Assert.Equal("Work2 END", item),
                item => Assert.Equal("Work1 END", item)
            );
        }

        [Fact]
        public void TestPauseByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work0 END");
            });
            Thread.Sleep(200);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work1 END");
            });
            Thread.Sleep(200);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work2 END");
            });
            Thread.Sleep(50);
            List<WorkID> pauseRes = powerPool.Pause(new List<WorkID>() { id });
            Assert.Empty(pauseRes);
            Thread.Sleep(1000);
            List<WorkID> resumeRes = powerPool.Resume(new List<WorkID>() { id });
            Assert.Empty(resumeRes);
            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("Work0 END", item),
                item => Assert.Equal("Work2 END", item),
                item => Assert.Equal("Work1 END", item)
            );
        }

        [Fact]
        public void TestPauseByGroup()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, new WorkOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work0 END");
                },
                Group = "B"
            });
            Thread.Sleep(200);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, new WorkOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work1 END");
                },
                Group = "A"
            });
            Thread.Sleep(200);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, new WorkOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work2 END");
                }
            });
            Thread.Sleep(50);
            List<WorkID> pauseRes = powerPool.Pause(powerPool.GetGroupMemberList("A"));
            Assert.Empty(pauseRes);
            Thread.Sleep(1000);
            List<WorkID> resumeRes = powerPool.Resume(powerPool.GetGroupMemberList("A"));
            Assert.Empty(resumeRes);
            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("Work0 END", item),
                item => Assert.Equal("Work2 END", item),
                item => Assert.Equal("Work1 END", item)
            );
        }

        [Fact]
        public void TestPauseByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, new WorkOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work0 END");
                },
                Group = "B"
            });
            Thread.Sleep(200);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, new WorkOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work1 END");
                },
                Group = "A"
            });
            Thread.Sleep(200);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, new WorkOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work2 END");
                }
            });
            Thread.Sleep(50);
            List<WorkID> pauseRes = powerPool.GetGroup("A").Pause();
            Assert.Empty(pauseRes);
            Thread.Sleep(1000);
            List<WorkID> resumeRes = powerPool.GetGroup("A").Resume();
            Assert.Empty(resumeRes);
            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("Work0 END", item),
                item => Assert.Equal("Work2 END", item),
                item => Assert.Equal("Work1 END", item)
            );
        }

        [Fact]
        public void TestPauseByIDAndResumeAll()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work0 END");
            });
            Thread.Sleep(100);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work1 END");
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work2 END");
            });
            Thread.Sleep(50);
            bool pauseRes = powerPool.Pause(id);
            Assert.True(pauseRes);
            Thread.Sleep(500);
            powerPool.Resume(true);
            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("Work0 END", item),
                item => Assert.Equal("Work2 END", item),
                item => Assert.Equal("Work1 END", item)
            );
        }

        [Fact]
        public void TestPauseByIDAndResumeAllWhenItStealWaiting()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 1 });
            List<string> logList = new List<string>();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work0 END");
            });
            Thread.Sleep(100);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work1 END");
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work2 END");
            });
            Thread.Sleep(50);
            bool pauseRes = powerPool.Pause(id);
            Assert.True(pauseRes);
            Thread.Sleep(500);
            powerPool.Resume(true);
            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("Work0 END", item),
                item => Assert.Equal("Work1 END", item),
                item => Assert.Equal("Work2 END", item)
            );
        }

        [Fact]
        public void TestForceStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            object res0 = null;
            object res1 = null;
            object res2 = null;
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                res0 = res.Exception;
            });
            Thread.Sleep(100);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                res1 = res.Exception;
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                res2 = res.Exception;
            });
            long start = GetNowSs();

            bool res = powerPool.ForceStop();
            powerPool.Wait();
            long end = GetNowSs() - start;

            if (res)
            {
                Assert.IsType<ThreadInterruptedException>(res0);
                Assert.IsType<ThreadInterruptedException>(res1);
                Assert.IsType<ThreadInterruptedException>(res2);
            }
        }

        [Fact]
        public void TestForceStopBeforeRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });

            int doneCount = 0;
            int cancelCount = 0;

            powerPool.WorkCanceled += (s, e) =>
            {
                Interlocked.Increment(ref cancelCount);
            };

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            powerPool.ForceStop(id);
            powerPool.Wait();

            Assert.Equal(3, doneCount);
            Assert.Equal(1, cancelCount);
        }

        [Fact]
        public void TestStopBeforeRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });
            int doneCount = 0;
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            powerPool.Stop(id);
            powerPool.Wait();

            Assert.Equal(3, doneCount);
        }

        [Fact]
        public void TestForceStopAfterExecuteEnd()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            WorkID resId = default;
            PowerPool powerPool = new PowerPool();
            WorkID id = default;
            powerPool.WorkEnded += (s, e) =>
            {
                if (e.Succeed)
                {
                    powerPool.ForceStop();
                }
            };
            powerPool.WorkStopped += (s, e) =>
            {
                if (e.ForceStop)
                {
                    resId = e.ID;
                }
            };
            id = powerPool.QueueWorkItem(() =>
            {
            }, (res) =>
            {
                if (res.Status != Status.ForceStopped)
                {
                    while (true)
                    {
                        Thread.Sleep(10);
                    }
                }
            });
            powerPool.Wait();

            Assert.Equal(resId, id);
        }

        [Fact]
        public void TestForceStopWhenCallback()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool()
            {
                PowerPoolOption = new PowerPoolOption()
                {
                    MaxThreads = 1,
                    EnableStatisticsCollection = true,
                }
            };
            bool forceStopped = false;
            powerPool.WorkStopped += (s, e) =>
            {
                forceStopped = e.ForceStop;
            };
            powerPool.QueueWorkItem(() =>
            {
            }, (res) =>
            {
                if (res.Status == Status.ForceStopped)
                {
                    return;
                }
                while (true)
                {
                    Thread.Sleep(10);
                }
            });
            Thread.Sleep(10);

            powerPool.ForceStop();
            powerPool.Wait();

            Assert.True(forceStopped);
        }

        [Fact]
        public void TestForceStopWhenInvoke()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool() { PowerPoolOption = new PowerPoolOption() { MaxThreads = 1 } };
            bool forceStopped = false;
            powerPool.WorkStopped += (s, e) =>
            {
                forceStopped = e.ForceStop;
            };
            powerPool.WorkEnded += (s, e) =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            };
            powerPool.QueueWorkItem(() =>
            {
            });
            Thread.Sleep(10);

            powerPool.ForceStop();
            powerPool.Wait();

            Assert.True(forceStopped);
        }

        [Fact]
        public void TestStopAll()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 1000; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work0 END");
            });
            Thread.Sleep(100);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 1000; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work1 END");
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 1000; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work2 END");
            });
            long start = GetNowSs();
            powerPool.Stop();
            long end = GetNowSs() - start;

            Assert.True(end >= 0 && end <= 300);
        }

        [Fact]
        public async void TestStopByIDMultiWorks()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 8 });
            WorkID id = default;
            WorkID resID = default;

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            Assert.Equal(8, powerPool.RunningWorkerCount);

            powerPool.Stop(id);

            Thread.Sleep(100);

            Assert.Equal(7, powerPool.RunningWorkerCount);
            Assert.Equal(resID, id);

            powerPool.ForceStop();
            await powerPool.WaitAsync();
            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public async void TestStopByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop(e.ID);
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            await powerPool.WaitAsync(id);
            await powerPool.WaitAsync();

            Assert.Equal(id, resID);
        }

        [Fact]
        public void TestStopByIDAfterWorkStart()
        {
#if DEBUG
            Spinner.s_enableTimeoutLog = false;
#endif
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool() { PowerPoolOption = new PowerPoolOption() { StartSuspended = true } };
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop(e.ID);
            };
            for (int i = 0; i < 100000; ++i)
            {
                powerPool.QueueWorkItem(() =>
                {
                });
            }

            powerPool.Start();
            powerPool.Wait();
#if DEBUG
            Spinner.s_enableTimeoutLog = true;
#endif
        }

        [Fact]
        public async void TestStopByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop(new List<WorkID>() { e.ID });
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            await powerPool.WaitAsync(new List<WorkID>() { id });
            await powerPool.WaitAsync();

            Assert.Equal(id, resID);
        }

        [Fact]
        public async void TestForceStopByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.ForceStop(new List<WorkID>() { e.ID });
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (true)
                {
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            await powerPool.WaitAsync(new List<WorkID>() { id });
            await powerPool.WaitAsync();

            Assert.Equal(id, resID);
        }

        [Fact]
        public async void TestStopByGroup()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop(powerPool.GetGroupMemberList("A"));
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, new WorkOption<object>()
            {
                Callback = (res) =>
                {
                    resID = res.ID;
                }
                ,
                Group = "A"
            });

            await powerPool.WaitAsync(powerPool.GetGroupMemberList("A"));
            await powerPool.WaitAsync();

            Assert.Equal(id, resID);
        }

        [Fact]
        public async void TestStopByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.GetGroup("A").Stop();
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(1);
                }
            }, new WorkOption<object>()
            {
                Callback = (res) =>
                {
                    resID = res.ID;
                }
                ,
                Group = "A"
            });

            await powerPool.GetGroup("A").WaitAsync();
            await powerPool.WaitAsync();

            Assert.Equal(id, resID);
        }

        [Fact]
        public async void TestForceStopByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.GetGroup("A").ForceStop();
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (true)
                {
                    Thread.Sleep(1);
                }
            }, new WorkOption<object>()
            {
                Callback = (res) =>
                {
                    resID = res.ID;
                }
                ,
                Group = "A"
            });

            await powerPool.GetGroup("A").WaitAsync();
            await powerPool.WaitAsync();

            Assert.Equal(id, resID);
        }

        [Fact]
        public async void TestStopByIDUseCheckIfRequestedStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop(e.ID);
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (true)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.ID;
            });

            await powerPool.WaitAsync(id);
            await powerPool.WaitAsync();

            Assert.Equal(id, resID);
        }

        [Fact]
        public async void TestStopByIDDoActionBeforeStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop(e.ID);
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (GetNowSs() - start <= 1000)
                {
                    powerPool.StopIfRequested(() => { });
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.Status == Status.Stopped ? WorkID.FromString("Stopped" + res.ID) : WorkID.FromString("Ended" + res.ID);
            });

            await powerPool.WaitAsync(id);
            await powerPool.WaitAsync();

            Assert.Equal(WorkID.FromString("Stopped" + id), resID);
        }

        [Fact]
        public async void TestStopByIDDoBeforeStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop(e.ID);
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (GetNowSs() - start <= 1000)
                {
                    powerPool.StopIfRequested(() => true);
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.Status == Status.Stopped ? WorkID.FromString("Stopped" + res.ID) : WorkID.FromString("Ended" + res.ID);
            });

            await powerPool.WaitAsync(id);
            await powerPool.WaitAsync();

            Assert.Equal(WorkID.FromString("Stopped" + id), resID);
        }

        [Fact]
        public void TestStopAllDoBeforeStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop();
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (GetNowSs() - start <= 1000)
                {
                    powerPool.StopIfRequested(() => true);
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.Status == Status.Stopped ? WorkID.FromString("Stopped" + res.ID) : WorkID.FromString("Ended" + res.ID);
            });

            powerPool.Wait(id);
            powerPool.Wait();

            Assert.Equal(WorkID.FromString("Stopped" + id), resID);
        }

        [Fact]
        public async void TestStopByIDDoBeforeStopReturnFalse()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop(e.ID);
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (GetNowSs() - start <= 1000)
                {
                    powerPool.StopIfRequested(() => false);
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.Status == Status.Stopped ? WorkID.FromString("Stopped" + res.ID) : WorkID.FromString("Ended" + res.ID);
            });

            await powerPool.WaitAsync(id);
            await powerPool.WaitAsync();

            Assert.Equal(WorkID.FromString("Ended" + id), resID);
        }

        [Fact]
        public void TestStopAllDoBeforeStopReturnFalse()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            WorkID id = default;
            WorkID resID = default;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop();
            };

            id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                while (GetNowSs() - start <= 1000)
                {
                    powerPool.StopIfRequested(() => false);
                    Thread.Sleep(1);
                }
            }, (res) =>
            {
                resID = res.Status == Status.Stopped ? WorkID.FromString("Stopped" + res.ID) : WorkID.FromString("Ended" + res.ID);
            });

            powerPool.Wait(id);
            powerPool.Wait();

            Assert.Equal(WorkID.FromString("Ended" + id), resID);
        }

        [Fact]
        public void TestCancelByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption()
            {
                MaxThreads = 2,
                EnableStatisticsCollection = true,
            });
            List<long> logList = new List<long>();
            WorkID cid = default;
            WorkID eid = default;
            DateTime queueTime = DateTime.MaxValue;
            DateTime startTime = DateTime.MaxValue;
            DateTime endTime = DateTime.MaxValue;
            powerPool.WorkCanceled += (s, e) =>
            {
                eid = e.ID;
                queueTime = e.QueueDateTime;
                startTime = e.StartDateTime;
                endTime = e.EndDateTime;
            };
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    logList.Add(res.Result);
                }
                else if (res.Status == Status.Canceled)
                {
                    cid = res.ID;
                }
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    logList.Add(res.Result);
                }
                else if (res.Status == Status.Canceled)
                {
                    cid = res.ID;
                }
            });
            Thread.Sleep(100);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    logList.Add(res.Result);
                }
                else if (res.Status == Status.Canceled)
                {
                    cid = res.ID;
                }
            });

            powerPool.Cancel(id);
            powerPool.Wait();

            Assert.NotEqual(queueTime, DateTime.MinValue.ToLocalTime());
            Assert.Equal(startTime, DateTime.MinValue.ToLocalTime());
            Assert.NotEqual(endTime, DateTime.MinValue.ToLocalTime());
            Assert.Equal(id, cid);
            Assert.Equal(id, eid);
            Assert.Equal(2, logList.Count);
        }

        [Fact]
        public void TestCancelByIDHasWorkGroup()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption()
            {
                MaxThreads = 2,
                EnableStatisticsCollection = true,
            });
            List<long> logList = new List<long>();
            WorkID cid = default;
            WorkID eid = default;
            DateTime queueTime = DateTime.MaxValue;
            DateTime startTime = DateTime.MaxValue;
            DateTime endTime = DateTime.MaxValue;
            powerPool.WorkCanceled += (s, e) =>
            {
                eid = e.ID;
                queueTime = e.QueueDateTime;
                startTime = e.StartDateTime;
                endTime = e.EndDateTime;
            };
            WorkOption<long> workOption = new WorkOption<long>
            {
                Group = "1",
                ShouldStoreResult = true,
                Callback = (res) =>
                {
                    if (res.Status == Status.Succeed)
                    {
                        logList.Add(res.Result);
                    }
                    else if (res.Status == Status.Canceled)
                    {
                        cid = res.ID;
                    }
                }
            };
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, workOption);
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, workOption);
            Thread.Sleep(100);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, workOption);

            powerPool.Cancel(id);
            powerPool.Wait();

            Assert.NotEqual(queueTime, DateTime.MinValue.ToLocalTime());
            Assert.Equal(startTime, DateTime.MinValue.ToLocalTime());
            Assert.NotEqual(endTime, DateTime.MinValue.ToLocalTime());
            Assert.Equal(id, cid);
            Assert.Equal(id, eid);
            Assert.Equal(2, logList.Count);
        }

        [Fact]
        public void TestCancelByIDSuspended()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { StartSuspended = true });

            bool canceled = true;

            WorkID id = powerPool.QueueWorkItem(() =>
            {
                canceled = false;
            });

            powerPool.Cancel(id);

            powerPool.Start();
            powerPool.Wait();

            Assert.True(canceled);
        }

        [Fact]
        public void TestCancelByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });
            List<long> logList = new List<long>();
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    logList.Add(res.Result);
                }
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    logList.Add(res.Result);
                }
            });
            Thread.Sleep(100);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    logList.Add(res.Result);
                }
            });

            powerPool.Cancel(new List<WorkID>() { id });
            powerPool.Wait();

            Assert.Equal(2, logList.Count);
        }

        [Fact]
        public void TestCancelByGroup()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });
            List<long> logList = new List<long>();
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, new WorkOption<long>()
            {
                Callback = (res) =>
                {
                    if (res.Status == Status.Succeed)
                    {
                        logList.Add(res.Result);
                    }
                }
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, new WorkOption<long>()
            {
                Callback = (res) =>
                {
                    if (res.Status == Status.Succeed)
                    {
                        logList.Add(res.Result);
                    }
                },
                Group = "B"
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, new WorkOption<long>()
            {
                Callback = (res) =>
                {
                    if (res.Status == Status.Succeed)
                    {
                        logList.Add(res.Result);
                    }
                },
                Group = "A"
            });

            powerPool.Cancel(powerPool.GetGroupMemberList("A"));
            powerPool.Wait();

            Assert.Equal(2, logList.Count);
        }

        [Fact]
        public void TestCancelByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });
            List<long> logList = new List<long>();
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, new WorkOption<long>()
            {
                Callback = (res) =>
                {
                    if (res.Status == Status.Succeed)
                    {
                        logList.Add(res.Result);
                    }
                }
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, new WorkOption<long>()
            {
                Callback = (res) =>
                {
                    if (res.Status == Status.Succeed)
                    {
                        logList.Add(res.Result);
                    }
                },
                Group = "B"
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, new WorkOption<long>()
            {
                Callback = (res) =>
                {
                    if (res.Status == Status.Succeed)
                    {
                        logList.Add(res.Result);
                    }
                },
                Group = "A"
            });

            powerPool.GetGroup("A").Cancel();
            powerPool.Wait();

            Assert.Equal(2, logList.Count);
        }

        [Fact]
        public void TestCancelAll()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 1 });
            List<long> logList = new List<long>();
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    logList.Add(res.Result);
                }
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    logList.Add(res.Result);
                }
            });
            Thread.Sleep(100);
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                long start = GetNowSs();
                for (int i = 0; i < 100; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
                return GetNowSs() - start;
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    logList.Add(res.Result);
                }
            });

            powerPool.Cancel();
            powerPool.Wait();

            Assert.Single(logList);
        }

        [Fact]
        public async void TestIDEmpty()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            WorkID workID = null;
            Assert.False(powerPool.Wait(workID));
            Assert.False(powerPool.Pause(workID));
            Assert.False(powerPool.Resume(workID));
            Assert.False(powerPool.Stop(workID));
            Assert.False(powerPool.Cancel(workID));
            Assert.Null(powerPool.Wait(new List<WorkID>() { workID }).First());
            Assert.Null(powerPool.Pause(new List<WorkID>() { workID }).First());
            Assert.Null(powerPool.Resume(new List<WorkID>() { workID }).First());
            Assert.Null(powerPool.Stop(new List<WorkID>() { workID }).First());
            Assert.Null(powerPool.Cancel(new List<WorkID>() { workID }).First());
            Assert.Null((await powerPool.WaitAsync(new List<WorkID>() { workID })).First());
        }

        [Fact]
        public void TestStopAfterIdle()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
            });

            Thread.Sleep(1000);
            Assert.False(powerPool.Stop());
        }

        [Fact]
        public void TestWaitByAll()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            powerPool.Wait();

            Assert.False(powerPool.PoolRunning);
        }

        [Fact]
        public void TestWaitByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            powerPool.Wait(id);

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public void TestWaitByIDNotRunningYet()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 1 });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            powerPool.Wait(id);

            Assert.True(GetNowSs() - start >= 2000);
        }

        [Fact]
        public void TestWaitByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            powerPool.Wait(new List<WorkID>() { id });

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public void TestWaitByGroup()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption()
            {
                Group = "A"
            });

            powerPool.Wait(powerPool.GetGroupMemberList("A"));

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public void TestWaitByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption()
            {
                Group = "A"
            });

            powerPool.GetGroup("A").Wait();

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public async Task TestWaitAsyncByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption()
            {
                Group = "A"
            });

            await powerPool.GetGroup("A").WaitAsync();

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public async Task TestWaitByIDInterruptEnd()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            });
            Task<bool> task = powerPool.WaitAsync(id);
            Thread.Sleep(100);
            powerPool.ForceStop();

            bool res = await task;
            Assert.True(res);
        }

        [Fact]
        public async Task TestWaitByIDSuspended()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { StartSuspended = true });
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            });

            Task<bool> task = powerPool.WaitAsync(id);
            Thread.Sleep(50);

            powerPool.Start();
            Thread.Sleep(50);

            powerPool.ForceStop();

            bool res = await task;
            Assert.True(res);
        }

        [Fact]
        public async void TestWaitByIDAsync()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            await powerPool.WaitAsync(id);

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public async void TestWaitByIDListAsync()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            await powerPool.WaitAsync(new List<WorkID>() { id });

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public void TestFetchByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return true;
            });

            ExecuteResult<bool> res = powerPool.Fetch<bool>(id);

            Assert.True(res.Result);
        }

        [Fact]
        public async void TestFetchByIDInterrupted()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(100);
                }
            });

            Task<ExecuteResult<object>> res = powerPool.FetchAsync(id);

            Thread.Sleep(1000);

            powerPool.ForceStop();

            Assert.NotNull((await res).Exception);
        }

        [Fact]
        public void TestFetchByIDAlreadyDone()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { ClearResultStorageWhenPoolStart = false });
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(10);
                return true;
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            powerPool.Wait();

            ExecuteResult<bool> res = powerPool.Fetch<bool>(id);

            Assert.True(res.Result);
        }

        [Fact]
        public void TestFetchByIDClearResultStorageWhenPoolStart()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { ClearResultStorageWhenPoolStart = true });
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(10);
                return "1";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            powerPool.Wait();

            powerPool.QueueWorkItem(() =>
            {
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            ExecuteResult<string> res = powerPool.Fetch<string>(id);

            Assert.Null(res.Result);
        }

        [Fact]
        public void TestFetchByIDRemoveAfterFetch()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { ClearResultStorageWhenPoolStart = true });
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(10);
                return "1";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            powerPool.Wait();

            ExecuteResult<string> res = powerPool.Fetch<string>(id);
            Assert.Equal("1", res.Result);

            res = powerPool.Fetch<string>(id, true);
            Assert.Equal("1", res.Result);

            res = powerPool.Fetch<string>(id, true);
            Assert.Null(res.Result);
        }

        [Fact]
        public void TestFetchByIDRemoveAfterFetchNeedWait()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { ClearResultStorageWhenPoolStart = true });
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "1";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            ExecuteResult<string> res = powerPool.Fetch<string>(id, true);
            Assert.Equal("1", res.Result);

            res = powerPool.Fetch<string>(id, true);
            Assert.Null(res.Result);

            powerPool.Wait();
        }

        [Fact]
        public void TestFetchObjByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return true;
            });

            ExecuteResult<object> resObj = powerPool.Fetch(id);

            Assert.True((bool)resObj.Result);
        }

        [Fact]
        public async void TestFetchObjByIDAsync()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return true;
            });

            ExecuteResult<object> resObj = await powerPool.FetchAsync(id);

            Assert.True((bool)resObj.Result);
        }

        [Fact]
        public void TestFetchByIDNotExist()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ExecuteResult<object> res = powerPool.Fetch(WorkID.FromString("id"));

            Assert.Null(res.Result);
        }

        [Fact]
        public void TestFetchByIDEmpty()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            WorkID workID = null;
            ExecuteResult<object> res = powerPool.Fetch(workID);

            Assert.Null(res);
        }

        [Fact]
        public async void TestFetchByIDSuspending()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { StartSuspended = true });

            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return true;
            });
            Task<ExecuteResult<bool>> resTask = powerPool.FetchAsync<bool>(id);

            powerPool.Start();
            powerPool.Wait();

            ExecuteResult<bool> res = await resTask;

            Assert.True(res.Result);
        }

        [Fact]
        public void TestFetchByIDListRemoveAfterFetch()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { ClearResultStorageWhenPoolStart = true });
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(10);
                return "0";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(10);
                return "1";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            powerPool.Wait();

            List<ExecuteResult<string>> res = powerPool.Fetch<string>(new List<WorkID> { id0, id1 });
            Assert.Equal("0", res[0].Result);
            Assert.Equal("1", res[1].Result);

            res = powerPool.Fetch<string>(new List<WorkID> { id0, id1 }, true);
            Assert.Equal("0", res[0].Result);
            Assert.Equal("1", res[1].Result);

            res = powerPool.Fetch<string>(new List<WorkID> { id0, id1 }, true);
            Assert.Null(res[0].Result);
            Assert.Null(res[1].Result);
        }

        [Fact]
        public void TestFetchObjByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return true;
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return false;
            });

            List<ExecuteResult<object>> resList = powerPool.Fetch(new List<WorkID>() { id0, id1, WorkID.FromString("id") });

            foreach (ExecuteResult<object> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.True((bool)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.False((bool)res.Result);
                }
                if (res.ID == WorkID.FromString("id"))
                {
                    Assert.True(res.Result == null);
                }
            }
        }

        [Fact]
        public async void TestFetchObjByIDListAsync()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return true;
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return false;
            });

            List<ExecuteResult<object>> resList = await powerPool.FetchAsync(new List<WorkID>() { id0, id1, WorkID.FromString("id") });

            foreach (ExecuteResult<object> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.True((bool)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.False((bool)res.Result);
                }
                if (res.ID == WorkID.FromString("id"))
                {
                    Assert.True(res.Result == null);
                }
            }
        }

        [Fact]
        public void TestFetchByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "0";
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "1";
            });

            List<ExecuteResult<string>> resList = powerPool.Fetch<string>(new List<WorkID>() { id0, id1, WorkID.FromString("id") });

            foreach (ExecuteResult<string> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.Equal("0", (string)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.Equal("1", (string)res.Result);
                }
                if (res.ID == WorkID.FromString("id"))
                {
                    Assert.True(res.Result == null);
                }
            }
        }

        [Fact]
        public void TestFetchByIDListAlreadyDone()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { ClearResultStorageWhenPoolStart = false });
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                return "0";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "1";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            powerPool.Wait(id0);

            List<ExecuteResult<string>> resList = powerPool.Fetch<string>(new List<WorkID>() { id0, id1, WorkID.FromString("id") });

            foreach (ExecuteResult<string> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.Equal("0", (string)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.Equal("1", (string)res.Result);
                }
                if (res.ID == WorkID.FromString("id"))
                {
                    Assert.True(res.Result == null);
                }
            }
        }

        [Fact]
        public async void TestFetchByIDListAsync()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "0";
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "1";
            });

            List<ExecuteResult<string>> resList = await powerPool.FetchAsync<string>(new List<WorkID>() { id0, id1, WorkID.FromString("id") });

            foreach (ExecuteResult<string> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.Equal("0", (string)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.Equal("1", (string)res.Result);
                }
                if (res.ID == WorkID.FromString("id"))
                {
                    Assert.True(res.Result == null);
                }
            }
        }

        [Fact]
        public async void TestFetchByIDListAsyncSuspended()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { StartSuspended = true });
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "0";
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "1";
            });

            Task<List<ExecuteResult<string>>> resListTask = powerPool.FetchAsync<string>(new List<WorkID>() { id0, id1, WorkID.FromString("id") });

            powerPool.Start();
            powerPool.Wait();

            List<ExecuteResult<string>> resList = await resListTask;
            foreach (ExecuteResult<string> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.Equal("0", (string)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.Equal("1", (string)res.Result);
                }
                if (res.ID == WorkID.FromString("id"))
                {
                    Assert.True(res.Result == null);
                }
            }
        }

        [Fact]
        public void TestFetchByPredicate()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                ClearResultStorageWhenPoolStart = false
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                return 1;
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                return 2;
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                return 3;
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });
            WorkID id4 = powerPool.QueueWorkItem(() =>
            {
                return 4;
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            powerPool.Wait();

            List<ExecuteResult<int>> resList = powerPool.Fetch<int>(x => x.Result >= 3);

            Assert.Equal(2, resList.Count);
            Assert.True(resList[0].ID == id3 || resList[0].ID == id4);
            Assert.True(resList[1].ID == id3 || resList[1].ID == id4);

            resList = powerPool.Fetch<int>(x => x.Result >= 3, true);

            Assert.Equal(2, resList.Count);
            Assert.True(resList[0].ID == id3 || resList[0].ID == id4);
            Assert.True(resList[1].ID == id3 || resList[1].ID == id4);

            resList = powerPool.Fetch<int>(x => x.Result >= 3, true);

            Assert.Empty(resList);
        }

        [Fact]
        public void TestFetchObjByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return true;
            }, new WorkOption()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return false;
            }, new WorkOption()
            {
                Group = "A"
            });

            List<ExecuteResult<object>> resList = powerPool.GetGroup("A").Fetch();

            foreach (ExecuteResult<object> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.True((bool)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.False((bool)res.Result);
                }
            }
        }

        [Fact]
        public async void TestFetchObjByGroupObjectAsync()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return true;
            }, new WorkOption()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return false;
            }, new WorkOption()
            {
                Group = "A"
            });

            List<ExecuteResult<object>> resList = await powerPool.GetGroup("A").FetchAsync();

            foreach (ExecuteResult<object> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.True((bool)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.False((bool)res.Result);
                }
            }
        }

        [Fact]
        public void TestFetchByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "0";
            }, new WorkOption()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "1";
            }, new WorkOption()
            {
                Group = "A"
            });

            List<ExecuteResult<string>> resList = powerPool.GetGroup("A").Fetch<string>();

            foreach (ExecuteResult<string> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.Equal("0", (string)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.Equal("1", (string)res.Result);
                }
            }
        }

        [Fact]
        public async void TestFetchByGroupObjectAsync()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "0";
            }, new WorkOption()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                return "1";
            }, new WorkOption()
            {
                Group = "A"
            });

            List<ExecuteResult<string>> resList = await powerPool.GetGroup("A").FetchAsync<string>();

            foreach (ExecuteResult<string> res in resList)
            {
                if (res.ID == id0)
                {
                    Assert.Equal("0", (string)res.Result);
                }
                if (res.ID == id1)
                {
                    Assert.Equal("1", (string)res.Result);
                }
            }
        }

        [Fact]
        public void TestFetchByPredicateByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                return 1;
            }, new WorkOption()
            {
                ShouldStoreResult = true,
                Group = "A",
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                return 2;
            }, new WorkOption()
            {
                ShouldStoreResult = true,
                Group = "B",
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                return 3;
            }, new WorkOption()
            {
                ShouldStoreResult = true,
                Group = "A",
            });
            WorkID id4 = powerPool.QueueWorkItem(() =>
            {
                return 4;
            }, new WorkOption()
            {
                ShouldStoreResult = true,
                Group = "B",
            });

            powerPool.Wait();

            List<ExecuteResult<int>> resList = powerPool.GetGroup("B").Fetch<int>(x => x.Result >= 3);

            Assert.True(resList.Count == 1);
            Assert.True(resList[0].ID == id4);

            resList = powerPool.GetGroup("B").Fetch<int>(x => x.Result >= 3, true);

            Assert.True(resList.Count == 1);
            Assert.True(resList[0].ID == id4);

            resList = powerPool.GetGroup("B").Fetch<int>(x => x.Result >= 3, true);

            Assert.True(resList.Count == 0);
        }

        [Fact]
        public void TestPauseWorkTimer()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 2000, ForceStop = true } });
            List<long> logList = new List<long>();
            object lockObj = new object();
            long start = GetNowSs();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(100);
                }
            });

            powerPool.Pause(id);
            Thread.Sleep(1000);
            powerPool.Resume(id);
            powerPool.Wait();
            long duration = GetNowSs() - start;

            Assert.True(duration >= 2800);
        }

        [Fact]
        public void TestPauseThreadTimer()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { TimeoutOption = new TimeoutOption() { Duration = 2000, ForceStop = true } });
            List<long> logList = new List<long>();
            object lockObj = new object();
            long start = GetNowSs();
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.PauseIfRequested();
                    Thread.Sleep(100);
                }
            });

            powerPool.Pause();
            Thread.Sleep(1000);
            powerPool.Resume();
            powerPool.Wait();
            long duration = GetNowSs() - start;

            Assert.True(duration >= 2900);
        }

        [Fact]
        public void TestResumeByIDDirectlyWithoutPause()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.PauseIfRequested();
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            });

            bool res = powerPool.Resume(id);

            powerPool.Stop();

            Assert.False(res);
        }

        [Fact]
        public void TestResumeAllDirectlyWithoutPause()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.PauseIfRequested();
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            });

            powerPool.Resume(true);
            powerPool.Stop();
        }

        [Fact]
        public void TestStartSuspended()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { StartSuspended = true });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            });

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.False(powerPool.PoolRunning);

            powerPool.Start();

            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.True(powerPool.PoolRunning);

            powerPool.Stop();
            powerPool.Wait();

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.False(powerPool.PoolRunning);

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            });

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.False(powerPool.PoolRunning);

            powerPool.Start();

            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.True(powerPool.PoolRunning);

            powerPool.Stop();
            powerPool.Wait();

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.False(powerPool.PoolRunning);
        }

        [Fact]
        public void TestStartWhenNotSuspended()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { StartSuspended = false });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    Thread.Sleep(100);
                }
            });

            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.True(powerPool.PoolRunning);

            powerPool.Start();

            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.True(powerPool.PoolRunning);

            powerPool.Stop();
            powerPool.Wait();
        }

        [Fact]
        public void TestStartSuspendedWithDependents()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption()
            {
                StartSuspended = true,
                EnableStatisticsCollection = true,
            });
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            });

            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i >= 0; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption()
            {
                Dependents = new PowerThreadPool.Collections.ConcurrentSet<WorkID>() { id }
            });

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.False(powerPool.PoolRunning);

            powerPool.Start();

            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.True(powerPool.PoolRunning);

            powerPool.Stop();
            powerPool.Wait();

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.False(powerPool.PoolRunning);
        }

        [Fact]
        public void TestWorkGroup()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "AAA"
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "AAA"
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "BBB"
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "BBB"
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "AAA"
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "BBB"
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "BBB"
            });

            Assert.Equal(3, powerPool.GetGroupMemberList("AAA").Count());
            Assert.Equal(4, powerPool.GetGroupMemberList("BBB").Count());

            powerPool.Stop(powerPool.GetGroupMemberList("AAA"));
            powerPool.Wait(powerPool.GetGroupMemberList("AAA"));

            Assert.Empty(powerPool.GetGroupMemberList("AAA"));
            Assert.Equal(4, powerPool.GetGroupMemberList("BBB").Count());

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "AAA"
            });

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "AAA"
            });

            Assert.Equal(2, powerPool.GetGroupMemberList("AAA").Count());
            Assert.Equal(4, powerPool.GetGroupMemberList("BBB").Count());

            powerPool.Stop();
            powerPool.Wait();

            Assert.Empty(powerPool.GetGroupMemberList("AAA"));
            Assert.Empty(powerPool.GetGroupMemberList("BBB"));
        }

        [Fact]
        public void TestCallPauseIfRequestedNotOnPowerPoolWorkerThread()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            Assert.Throws<InvalidOperationException>(() => powerPool.PauseIfRequested());
        }

        [Fact]
        public void TestCallCheckIfRequestedStopNotOnPowerPoolWorkerThread()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            Assert.Throws<InvalidOperationException>(() => powerPool.CheckIfRequestedStop());
        }
    }
}
