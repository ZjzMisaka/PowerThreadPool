using System.Collections.Concurrent;
using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Collections;
using PowerThreadPool.Constants;
using PowerThreadPool.EventArguments;
using PowerThreadPool.Exceptions;
using PowerThreadPool.Groups;
using PowerThreadPool.Helpers.LockFree;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using Xunit.Abstractions;

namespace UnitTest
{
    public class PowerPoolTest
    {
        private readonly ITestOutputHelper _output;

        public PowerPoolTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestOrderAndDefaultCallback()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            List<string> logList = new List<string>();
            string result = "";
            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    logList.Add("DefaultCallback");
                    result = (string)res.Result;
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                TimeoutOption = new TimeoutOption() { Duration = 10000, ForceStop = false },
                DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 3000, ForceStop = false },
            };
            powerPool.PoolStarted += (s, e) =>
            {
                logList.Add("PoolStart");
            };
            powerPool.PoolIdled += (s, e) =>
            {
                logList.Add("PoolIdle");
            };
            powerPool.WorkStarted += (s, e) =>
            {
                logList.Add("WorkStart");
            };
            powerPool.WorkEnded += (s, e) =>
            {
                logList.Add("WorkEnd");
            };
            powerPool.WorkTimedOut += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimedOut += (s, e) =>
            {
                logList.Add("PoolTimeout");
            };

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                return "TestOrder Result";
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("PoolStart", item),
                item => Assert.Equal("WorkStart", item),
                item => Assert.Equal("WorkEnd", item),
                item => Assert.Equal("DefaultCallback", item),
                item => Assert.Equal("PoolIdle", item)
                );

            Assert.Equal("TestOrder Result", result);
        }

        [Fact]
        public void TestCallback()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    Assert.Fail("Should not run DefaultCallback");
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                TimeoutOption = new TimeoutOption() { Duration = 10000, ForceStop = false },
                DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 3000, ForceStop = false },
                EnableStatisticsCollection = true,
            };

            WorkID id = default;
            WorkID resId = default;
            id = powerPool.QueueWorkItem(() =>
            {
                return 1024;
            }, (res) =>
            {
                resId = res.ID;
                Assert.Equal(Status.Succeed, res.Status);
                Assert.Equal(1024, res.Result);
                Assert.True(res.QueueDateTime < res.StartDateTime);
                Assert.True(res.StartDateTime < res.EndDateTime);
            });
            powerPool.Wait();
            Assert.NotNull(id);
            Assert.Equal(id, resId);
        }

        [Fact]
        public void TestDefaultWorkTimeout()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            List<string> logList = new List<string>();
            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    Assert.IsType<ThreadInterruptedException>(res.Exception);
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                TimeoutOption = new TimeoutOption() { Duration = 10000, ForceStop = true },
                DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 3000, ForceStop = true },
            };
            powerPool.WorkTimedOut += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimedOut += (s, e) =>
            {
                logList.Add("PoolTimeout");
            };

            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 20; ++i)
                {
                    Thread.Sleep(1000);
                }
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("WorkTimeout", item)
                );
        }

        [Fact]
        public void TestThreadPoolTimeout()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            List<string> logList = new List<string>();
            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    // Assert.IsType<ThreadInterruptedException>(res.Exception);
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                TimeoutOption = new TimeoutOption() { Duration = 1000, ForceStop = true },
                DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 30000, ForceStop = true },
            };
            bool timeOut = false;
            powerPool.WorkTimedOut += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimedOut += (s, e) =>
            {
                timeOut = true;
                logList.Add("PoolTimeout");
            };

            WorkID id = default;
            for (int j = 0; j < 50; ++j)
            {
                id = powerPool.QueueWorkItem(() =>
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        Thread.Sleep(100);
                    }
                });
                Thread.Sleep(250);
                if (timeOut)
                {
                    break;
                }
            }

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("PoolTimeout", item)
                );
        }

        [Fact]
        public void TestThreadPoolTimeoutStartTwice()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            List<string> logList = new List<string>();
            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    // Assert.IsType<ThreadInterruptedException>(res.Exception);
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                TimeoutOption = new TimeoutOption() { Duration = 1000, ForceStop = true },
                DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 30000, ForceStop = true },
            };
            bool timeOut = false;
            powerPool.WorkTimedOut += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimedOut += (s, e) =>
            {
                timeOut = true;
                logList.Add("PoolTimeout");
            };

            WorkID id = default;
            for (int j = 0; j < 50; ++j)
            {
                id = powerPool.QueueWorkItem(() =>
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        Thread.Sleep(100);
                    }
                });
                Thread.Sleep(250);
                if (timeOut)
                {
                    break;
                }
            }

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("PoolTimeout", item)
                );

            powerPool.WorkTimedOut += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimedOut += (s, e) =>
            {
                timeOut = true;
                logList.Add("PoolTimeout");
            };

            for (int j = 0; j < 50; ++j)
            {
                powerPool.QueueWorkItem(() =>
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        Thread.Sleep(100);
                    }
                });
                Thread.Sleep(250);
                if (timeOut)
                {
                    break;
                }
            }

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("PoolTimeout", item)
                );
        }

        [Fact]
        public void TestWorkTimeout()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            List<string> logList = new List<string>();
            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    Assert.IsType<ThreadInterruptedException>(res.Exception);
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                TimeoutOption = new TimeoutOption() { Duration = 10000, ForceStop = true },
                DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 300000000, ForceStop = true },
            };
            powerPool.WorkTimedOut += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimedOut += (s, e) =>
            {
                logList.Add("PoolTimeout");
            };

            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 20; ++i)
                {
                    Thread.Sleep(1000);
                }
            },
            new WorkOption()
            {
                TimeoutOption = new TimeoutOption() { Duration = 100, ForceStop = true }
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("WorkTimeout", item)
                );
        }

        [Fact]
        public void TestError()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    Assert.Fail("Should not run DefaultCallback");
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                TimeoutOption = new TimeoutOption() { Duration = 10000, ForceStop = false },
                DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 3000, ForceStop = false },
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception("custom error");
            }, (res) =>
            {
                Assert.Equal("custom error", res.Exception.Message);
                Assert.Equal(Status.Failed, res.Status);
            });
        }

        [Fact]
        public void TestThreadInterruptedErrorInPoolIdledEvent()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int count = 0;
            int inEvent = 0;
            PowerPool powerPool = new PowerPool();

            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.ForceStop();
            };
            powerPool.PoolIdled += (s, e) =>
            {
                try
                {
                    Thread.Sleep(10000000);
                }
                catch
                {
                    Interlocked.Increment(ref inEvent);
                    throw;
                }
            };

            powerPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref count);
            });

            powerPool.Wait();

            Assert.Equal(1, count);
            Assert.Equal(1, inEvent);
        }

        [Fact]
        public void TestThreadInterruptedWhenWorkerIdle()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 0, KeepAliveTime = 10000 }
            };
            powerPool.WorkEnded += (s, e) =>
            {
                powerPool.ForceStop();
            };
            powerPool.QueueWorkItem(() =>
            {
            });

            Thread.Sleep(200);

            Assert.Equal(0, powerPool.IdleWorkerCount);
            Assert.Equal(0, powerPool.AliveWorkerCount);
        }

        [Fact]
        public void TestDependents()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            powerPool.PoolStarted += (s, e) =>
            {
                logList.Add("PoolStart");
            };
            powerPool.PoolIdled += (s, e) =>
            {
                logList.Add("PoolIdle");
            };

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1700);
                logList.Add("Work3 END");
            }, (res) =>
            {
                Thread.Sleep(1300);
                logList.Add("Work3 callback END");
            });

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                logList.Add("Work0 END");
            }, (res) =>
            {
                Thread.Sleep(1000);
                logList.Add("Work0 callback END");
            });

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1500);
                logList.Add("Work1 END");
            });

            powerPool.QueueWorkItem(() =>
            {
                logList.Add("Work2 denpend on work0, work1 END");
            },
            new WorkOption()
            {
                Dependents = new ConcurrentSet<WorkID>() { id0, id1 }
            }
            );

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("PoolStart", item),
                item => Assert.Equal("Work0 END", item),
                item => Assert.Equal("Work1 END", item),
                item => Assert.Equal("Work3 END", item),
                item => Assert.Equal("Work0 callback END", item),
                item => Assert.Equal("Work2 denpend on work0, work1 END", item),
                item => Assert.Equal("Work3 callback END", item),
                item => Assert.Equal("PoolIdle", item)
                );
        }

        [Fact]
        public void TestDependentsFailed()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 }
            };

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref doneCount);
            };

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                throw new Exception();
            });

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            powerPool.QueueWorkItem(() =>
            {
            },
           new WorkOption()
           {
               Dependents = new ConcurrentSet<WorkID>() { id0, id1 }
           });

            powerPool.Wait();

            Assert.Equal(3, doneCount);
            Assert.Equal(2, powerPool.FailedWorkCount);
            Assert.Equal(id0, powerPool.FailedWorkList.First());
            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestDependentsSucceeded()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
            };

            WorkID id0 = default;

            powerPool.EnablePoolIdleCheck = false;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref doneCount);

                if (doneCount == 1)
                {
                    powerPool.QueueWorkItem(() =>
                    {
                        powerPool.EnablePoolIdleCheck = true;
                    }, new WorkOption
                    {
                        ShouldStoreResult = true,
                        Dependents = new ConcurrentSet<WorkID> { e.ID }
                    });
                }
            };

            id0 = powerPool.QueueWorkItem(() =>
            {
            }, new WorkOption
            {
                ShouldStoreResult = true
            });

            powerPool.Wait();

            Assert.Equal(2, doneCount);
        }

        [Fact]
        public void TestDependentsFailedHoldFailtureRecord()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 },
                ClearFailedWorkRecordWhenPoolStart = false,
            };

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref doneCount);
            };

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(10);
                throw new Exception();
            });

            powerPool.Wait();

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(10);
            });

            powerPool.QueueWorkItem(() =>
            {
            },
           new WorkOption()
           {
               Dependents = new ConcurrentSet<WorkID>() { id0, id1 }
           });

            powerPool.Wait();

            Assert.Equal(3, doneCount);
            Assert.Equal(2, powerPool.FailedWorkCount);
            Assert.Equal(id0, powerPool.FailedWorkList.First());
            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestDependentsFailedBeforeWorkRun()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 },
                EnableStatisticsCollection = true,
            };
            powerPool.EnablePoolIdleCheck = false;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref doneCount);
            };

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
            });

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            });

            Thread.Sleep(2000);

            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            Thread.Sleep(100);

            powerPool.QueueWorkItem(() =>
            {
            },
           new WorkOption()
           {
               Dependents = new ConcurrentSet<WorkID>() { id0, id1, id2 }
           });

            powerPool.EnablePoolIdleCheck = true;

            powerPool.Wait();

            Assert.Equal(4, doneCount);
            Assert.Equal(2, powerPool.FailedWorkCount);
            Assert.Equal(id1, powerPool.FailedWorkList.First());
            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestDependentsAllSucceedBeforeWorkRun()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 },
                EnableStatisticsCollection = true,
            };
            powerPool.EnablePoolIdleCheck = false;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref doneCount);
            };

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
            });

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
            });

            Thread.Sleep(200);

            powerPool.QueueWorkItem(() =>
            {
            },
           new WorkOption()
           {
               Dependents = new ConcurrentSet<WorkID>() { id0, id1 }
           });

            powerPool.EnablePoolIdleCheck = true;

            Assert.Equal(2, doneCount);
            Assert.Equal(0, powerPool.FailedWorkCount);
            Assert.Equal(1, powerPool.WaitingWorkCount);

            powerPool.Stop();

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestDependentsHasCycle()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            WorkID id = powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(2) }
            });
            Assert.Throws<CycleDetectedException>(() =>
            {
                powerPool.QueueWorkItem<object>(() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                }, new WorkOption
                {
                    Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(1) }
                });
            });

            powerPool.Wait();
        }

        [Fact]
        public void TestDependentsHasDifficultCycle()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(3) }
            });
            powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(1) }
            });
            Assert.Throws<CycleDetectedException>(() =>
            {
                powerPool.QueueWorkItem<object>(() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                }, new WorkOption
                {
                    Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(2) }
                });
            });

            powerPool.Wait();
        }

        [Fact]
        public void TestDependentsHasDifficultDependency1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            });
            powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(1) }
            });
            powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(2) }
            });
            powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(1) }
            });
            powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(3), WorkID.FromLong(4) }
            });

            powerPool.Stop();
        }

        [Fact]
        public void TestDependentsHasDifficultDependency2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            powerPool.QueueWorkItem<object>(() =>//c
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                CustomWorkID = "3",
            });
            powerPool.QueueWorkItem<object>(() =>//b
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                CustomWorkID = "2",
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromString("3") }
            });
            powerPool.QueueWorkItem<object>(() =>//d
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                CustomWorkID = "4",
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromString("2") }
            });
            powerPool.QueueWorkItem<object>(() =>//a
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                CustomWorkID = "1",
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromString("2"), WorkID.FromString("4") }
            });

            powerPool.Stop();
        }

        [Fact]
        public void TestDependentsDoesNotHaveCycle()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            WorkID id = powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(2) }
            });
            powerPool.QueueWorkItem<object>(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(100);
                }
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(8) }
            });

            powerPool.Stop();
        }

        [Fact]
        public void TestDependentsOldWorkDependOnNewWork()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                EnableStatisticsCollection = true,
            };
            int done = 0;
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref done);
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(2) }
            });
            powerPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref done);
            });

            powerPool.Wait();

            Assert.Equal(2, done);
        }

        [Fact]
        public void TestDependentsStopByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            int done = 0;
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref done);
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(2) }
            });

            powerPool.Stop(id);

            powerPool.Wait();

            Assert.Equal(0, done);
        }

        [Fact]
        public void TestDependentsCancelByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            int done = 0;
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref done);
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { WorkID.FromLong(2) }
            });

            powerPool.Cancel(id);

            powerPool.Wait();

            Assert.Equal(0, done);
        }

        [Fact]
        public void TestWorkPriority()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.EnablePoolIdleCheck = false;
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 0, KeepAliveTime = 3000 }
            };
            bool work1Started = false;
            bool work2Started = false;
            powerPool.QueueWorkItem(() =>
            {
                work1Started = true;
                Thread.Sleep(300);
                lock (powerPool)
                {
                    logList.Add("Work0 Priority0 END");
                }
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                work2Started = true;
                Thread.Sleep(300);
                lock (powerPool)
                {
                    logList.Add("Work1 Priority0 END");
                }
            }, new WorkOption()
            {
            });
            while (!work1Started || !work2Started)
            {
                Thread.Yield();
            }
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(300);
                lock (powerPool)
                {
                    logList.Add("Work2 Priority0 END");
                }
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(300);
                lock (powerPool)
                {
                    logList.Add("Work3 Priority0 END");
                }
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(300);
                lock (powerPool)
                {
                    logList.Add("Work4 Priority1 END");
                }
            }, new WorkOption()
            {
                WorkPriority = 1
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(300);
                lock (powerPool)
                {
                    logList.Add("Work5 Priority1 END");
                }
            }, new WorkOption()
            {
                WorkPriority = 1
            });
            powerPool.EnablePoolIdleCheck = true;
            powerPool.Wait();

            string[] logGroup1 = new[] { "Work0 Priority0 END", "Work1 Priority0 END" };
            string[] logGroup2 = new[] { "Work4 Priority1 END", "Work5 Priority1 END" };
            string[] logGroup3 = new[] { "Work2 Priority0 END", "Work3 Priority0 END" };

            Assert.Contains(logGroup1[0], logList);
            Assert.Contains(logGroup1[1], logList);
            int index1_0 = logList.IndexOf(logGroup1[0]);
            int index1_1 = logList.IndexOf(logGroup1[1]);
            Assert.True(index1_0 < index1_1 || index1_0 > index1_1);

            Assert.Contains(logGroup2[0], logList);
            Assert.Contains(logGroup2[1], logList);
            int index2_0 = logList.IndexOf(logGroup2[0]);
            int index2_1 = logList.IndexOf(logGroup2[1]);
            Assert.True(index2_0 < index2_1 || index2_0 > index2_1);

            Assert.Contains(logGroup3[0], logList);
            Assert.Contains(logGroup3[1], logList);
            int index3_0 = logList.IndexOf(logGroup3[0]);
            int index3_1 = logList.IndexOf(logGroup3[1]);
            Assert.True(index3_0 < index3_1 || index3_0 > index3_1);

            Assert.True((index1_0 < index2_0 && index1_1 < index2_1) || (index1_0 > index2_0 && index1_1 > index2_1));

            Assert.True((index2_0 < index3_0 && index2_1 < index3_1) || (index2_0 > index3_0 && index2_1 > index3_1));
        }

        [Fact]
        public void TestThreadPriority()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            object lockObj1 = new object();
            object lockObj2 = new object();
            long counter1 = 0;
            long counter2 = 0;
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    lock (lockObj1)
                    {
                        ++counter1;
                    }
                    Thread.Sleep(1);
                }
            }, new WorkOption()
            {
                ThreadPriority = ThreadPriority.Lowest
            });
            powerPool.QueueWorkItem(() =>
            {
                DateTime start = DateTime.Now;

                while (true)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    lock (lockObj2)
                    {
                        ++counter2;
                    }
                    Thread.Sleep(1);
                }
            }, new WorkOption()
            {
                ThreadPriority = ThreadPriority.Highest
            });

            powerPool.Stop();
        }

        [Fact]
        public void TestThreadSwitchOnForegroundOrBackground()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            var powerPool = new PowerPool(new PowerPoolOption()
            {
                MaxThreads = 1,
            });

            bool allSame = true;

            foreach (var i in new[] { false, true, false, true, false })
            {
                powerPool.QueueWorkItem(value =>
                {
                    if (Thread.CurrentThread.IsBackground != value)
                    {
                        allSame = false;
                    }
                }, i,
                                        new WorkOption()
                                        {
                                            IsBackground = i
                                        });
            }

            powerPool.Wait();

            Assert.True(allSame);
        }

        [Fact]
        public void TestRunningStatus()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 1, DestroyThreadOption = new DestroyThreadOption() { KeepAliveTime = 1000, MinThreads = 0 } });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            powerPool.QueueWorkItem(() =>
            {
            });
            Thread.Sleep(10);
            Assert.Equal(0, powerPool.IdleWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(1, powerPool.WaitingWorkCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Single(powerPool.WaitingWorkList);

            powerPool.Wait();
            Assert.Equal(1, powerPool.IdleWorkerCount);
        }

        [Fact]
        public void TestCustomWorkID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            },
            new WorkOption()
            {
                CustomWorkID = "1024"
            });

            powerPool.WorkEnded += (s, e) =>
            {
                Assert.Equal(WorkID.FromString("1024"), e.ID);
            };
            Assert.Equal(WorkID.FromString("1024"), id);
        }

        [Fact]
        public void TestDuplicateCustomWorkID1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            },
            new WorkOption()
            {
                CustomWorkID = "1024"
            });
            InvalidOperationException ex = null;
            try
            {
                WorkID id1 = powerPool.QueueWorkItem(() =>
                {
                    Thread.Sleep(1000);
                },
                new WorkOption()
                {
                    CustomWorkID = "1024"
                });
            }
            catch (InvalidOperationException e)
            {
                ex = e;
            }

            Assert.Equal("The work ID '1024' already exists.", ex.Message);
        }

        [Fact]
        public void TestDuplicateCustomWorkID2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { StartSuspended = true });
            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            },
            new WorkOption()
            {
                CustomWorkID = "1024"
            });
            InvalidOperationException ex = null;
            try
            {
                WorkID id1 = powerPool.QueueWorkItem(() =>
                {
                    Thread.Sleep(1000);
                },
                new WorkOption()
                {
                    CustomWorkID = "1024"
                });
            }
            catch (InvalidOperationException e)
            {
                ex = e;
            }

            powerPool.Start();

            Assert.Equal("The work ID '1024' already exists.", ex.Message);
        }

        [Fact]
        public void TestMaxThreadsNumberError()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            bool errored = false;
            try
            {
                PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 10, DestroyThreadOption = new DestroyThreadOption() { MinThreads = 100 } });
                WorkID id = powerPool.QueueWorkItem(() =>
                {
                    Thread.Sleep(1000);
                });
            }
            catch (Exception ex)
            {
                Assert.Equal("The minimum number of threads cannot be greater than the maximum number of threads.", ex.Message);
                errored = true;
            }
            Assert.True(errored);
        }

        [Fact]
        public void TestMaxThreadsNumberErrorWhenSetAgainError()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            bool errored = false;
            try
            {
                PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 10, DestroyThreadOption = new DestroyThreadOption() { MinThreads = 5 } });
                powerPool.PowerPoolOption.MaxThreads = 1;
                WorkID id = powerPool.QueueWorkItem(() =>
                {
                    Thread.Sleep(1000);
                });
            }
            catch (Exception ex)
            {
                Assert.Equal("The minimum number of threads cannot be greater than the maximum number of threads.", ex.Message);
                errored = true;
            }
            Assert.True(errored);
        }

        [Fact]
        public void TestMinThreadsNumberErrorWhenSetAgainError()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            bool errored = false;
            try
            {
                PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 10, DestroyThreadOption = new DestroyThreadOption() { MinThreads = 5 } });
                powerPool.PowerPoolOption.DestroyThreadOption.MinThreads = 10000;
                WorkID id = powerPool.QueueWorkItem(() =>
                {
                    Thread.Sleep(1000);
                });
            }
            catch (Exception ex)
            {
                Assert.Equal("The minimum number of threads cannot be greater than the maximum number of threads.", ex.Message);
                errored = true;
            }
            Assert.True(errored);
        }

        [Fact]
        public void TestMaxThreadsNumberErrorWhenSetAgain()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            bool errored = false;
            try
            {
                PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 10, DestroyThreadOption = new DestroyThreadOption() { MinThreads = 5 } });
                powerPool.PowerPoolOption.MaxThreads = 20;
                WorkID id = powerPool.QueueWorkItem(() =>
                {
                    Thread.Sleep(1000);
                });
            }
            catch (Exception)
            {
                errored = true;
            }
            Assert.False(errored);
        }

        [Fact]
        public void TestMinThreadsNumberErrorWhenSetAgain()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            bool errored = false;
            try
            {
                PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 10, DestroyThreadOption = new DestroyThreadOption() { MinThreads = 5 } });
                powerPool.PowerPoolOption.DestroyThreadOption.MinThreads = 8;
                WorkID id = powerPool.QueueWorkItem(() =>
                {
                    Thread.Sleep(1000);
                });
            }
            catch (Exception)
            {
                errored = true;
            }
            Assert.False(errored);
        }

        [Fact]
        public void TestWaitFailed()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    Thread.Sleep(1);
                }
            }, new WorkOption()
            {
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
            }, new WorkOption()
            {
            });

            Thread.Sleep(100);

            bool res = powerPool.Wait(id2);

            Assert.False(res);

            powerPool.Stop();
        }

        [Fact]
        public void TestPauseFailed()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    Thread.Sleep(1);
                }
            }, new WorkOption()
            {
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
            }, new WorkOption()
            {
            });

            Thread.Sleep(100);

            bool res = powerPool.Pause(id2);

            Assert.False(res);

            powerPool.Stop();
        }

        [Fact]
        public void TestCancelFailed()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    Thread.Sleep(1);
                }
            }, new WorkOption()
            {
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
            }, new WorkOption()
            {
            });

            Thread.Sleep(100);

            bool res = powerPool.Cancel(id2);

            Assert.False(res);

            powerPool.Stop();
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestQueueWhenStopping()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            bool canReturn = false;

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 9999999; ++i)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        while (!canReturn)
                        {
                            Thread.Sleep(100);
                        }
                        return;
                    }
                    Thread.Sleep(100);
                }
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Stop();

            await Task.Delay(100);

            WorkID id = powerPool.QueueWorkItem(() =>
            {
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            canReturn = true;

            await powerPool.WaitAsync();

            Assert.False(id == null);

            await Task.Delay(100);

            Assert.Equal(2, doneCount);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestQueueWhenStoppingAndCancel()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            bool canReturn = false;

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 9999999; ++i)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        while (!canReturn)
                        {
                            Thread.Sleep(100);
                        }
                        return;
                    }
                    Thread.Sleep(100);
                }
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Stop();

            await Task.Delay(100);

            WorkID id = powerPool.QueueWorkItem(() =>
            {
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Cancel(id);

            canReturn = true;

            await powerPool.WaitAsync();

            Assert.False(id == null);

            await Task.Delay(100);

            Assert.Equal(1, doneCount);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestQueueWhenStoppingAndCancelAll()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            bool canReturn = false;

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 9999999; ++i)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        while (!canReturn)
                        {
                            Thread.Sleep(100);
                        }
                        return;
                    }
                    Thread.Sleep(100);
                }
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Stop();

            await Task.Delay(100);

            WorkID id = powerPool.QueueWorkItem(() =>
            {
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Cancel();

            canReturn = true;

            await powerPool.WaitAsync();

            Assert.False(id == null);

            await Task.Delay(100);

            Assert.Equal(1, doneCount);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestStartSuspendWhenStopping()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            bool canReturn = false;

            PowerPool powerPool = new PowerPool(new PowerPoolOption { StartSuspended = true });
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 9999999; ++i)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        while (!canReturn)
                        {
                            Thread.Sleep(100);
                        }
                        return;
                    }
                    Thread.Sleep(100);
                }
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Start();

            powerPool.Stop();

            await Task.Delay(100);

            WorkID id = powerPool.QueueWorkItem(() =>
            {
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Start();

            canReturn = true;

            await powerPool.WaitAsync();

            Assert.False(id == null);

            await Task.Delay(100);

            Assert.Equal(2, doneCount);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestStartSuspendWhenStoppingAndCancel()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            bool canReturn = false;

            PowerPool powerPool = new PowerPool(new PowerPoolOption { StartSuspended = true });
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 9999999; ++i)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        while (!canReturn)
                        {
                            Thread.Sleep(100);
                        }
                        return;
                    }
                    Thread.Sleep(100);
                }
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Start();

            powerPool.Stop();

            await Task.Delay(100);

            WorkID id = powerPool.QueueWorkItem(() =>
            {
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Start();

            powerPool.Cancel(id);

            canReturn = true;

            await powerPool.WaitAsync();

            Assert.False(id == null);

            await Task.Delay(100);

            Assert.Equal(1, doneCount);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestStartSuspendWhenStoppingAndCancelAll()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            bool canReturn = false;

            PowerPool powerPool = new PowerPool(new PowerPoolOption { StartSuspended = true });
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 9999999; ++i)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        while (!canReturn)
                        {
                            Thread.Sleep(100);
                        }
                        return;
                    }
                    Thread.Sleep(100);
                }
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Start();

            powerPool.Stop();

            await Task.Delay(100);

            WorkID id = powerPool.QueueWorkItem(() =>
            {
            }, (e) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Start();

            powerPool.Cancel();

            canReturn = true;

            await powerPool.WaitAsync();

            Assert.False(id == null);

            await Task.Delay(100);

            Assert.Equal(1, doneCount);
        }

        [Fact]
        public void TestResetWaitingWorkWhenForceStopEnd()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 2, KeepAliveTime = 30000 }
            };

            WorkID id3 = default;
            powerPool.WorkStarted += (s, e) =>
            {
                if (e.ID == id3)
                {
                    powerPool.ForceStop(id3);
                }
            };

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            id3 = powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 200; ++i)
                {
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            WorkID id4 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            WorkID id5 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            WorkID id6 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            powerPool.Wait();

            Assert.Equal(5, doneCount);
        }

        [Fact]
        public void TestDispose()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2, DestroyThreadOption = new DestroyThreadOption() { MinThreads = 2, KeepAliveTime = 1000000 } });
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

            powerPool.Dispose();

            Assert.Equal(0, powerPool.AliveWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);
        }

        [Fact]
        public void TestDisposeHasTimers()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption()
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 2, KeepAliveTime = 1000000 },
                TimeoutOption = new TimeoutOption() { Duration = 1000, ForceStop = true },
                DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 30000, ForceStop = true },
                RunningTimerOption = new RunningTimerOption() { Elapsed = _ => { }, Interval = 1000 },
            });
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

            powerPool.Dispose();

            Assert.Equal(0, powerPool.AliveWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);
        }

        [Fact]
        public void TestDisposeIdleWorker()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 8, DestroyThreadOption = new DestroyThreadOption() { MinThreads = 8, KeepAliveTime = 1000000 } });
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

            powerPool.Dispose();

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.AliveWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);
        }

        [Fact]
        public void TestEnablePoolIdleCheck()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int idleCount = 0;
            int doneCount = 0;
            PowerPool powerPool = new PowerPool();
            powerPool.PoolIdled += (s, e) =>
            {
                Interlocked.Increment(ref idleCount);
            };

            Assert.True(powerPool.EnablePoolIdleCheck);
            powerPool.EnablePoolIdleCheck = false;

            powerPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref doneCount);
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref doneCount);
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.EnablePoolIdleCheck = true;
            Assert.True(powerPool.EnablePoolIdleCheck);

            powerPool.Wait();

            Assert.Equal(1, idleCount);
            Assert.Equal(3, doneCount);
        }

        [Fact]
        public void TestSetWorkAfterDispose()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.Dispose();

            Exception exception = null;

            try
            {
                powerPool.QueueWorkItem(() =>
                {
                });
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
            Assert.Equal("ObjectDisposedException", exception.GetType().Name);
        }

        [Fact]
        public void TestStartSuspendAfterDispose()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool() { PowerPoolOption = new PowerPoolOption() { StartSuspended = true } };

            Exception exception = null;

            powerPool.QueueWorkItem(() =>
            {
            });
            powerPool.Dispose();

            try
            {
                powerPool.Start();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
            Assert.Equal("ObjectDisposedException", exception.GetType().Name);
        }

        [Fact]
        public void TestLongWork()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 }
            };

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                }
            }, new WorkOption()
            {
                LongRunning = true,
            });

            Thread.Sleep(500);

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, new WorkOption()
            {
            });

            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(1, powerPool.LongRunningWorkerCount);

            Thread.Sleep(1000);

            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(1, powerPool.LongRunningWorkerCount);

            powerPool.Stop();

            Thread.Sleep(1000);

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.LongRunningWorkerCount);
        }

        [Fact]
        public void TestLongWorkWithNormalWork()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 10000 }
            };

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                }
            }, new WorkOption()
            {
                LongRunning = true,
            });

            Thread.Sleep(100);

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption()
            {
            });

            Thread.Sleep(10);

            powerPool.Stop();

            Thread.Sleep(500);

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.IdleWorkerCount);
            Assert.Equal(0, powerPool.LongRunningWorkerCount);
        }

        [Fact]
        public void TestLongWorkForceStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 }
            };

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, new WorkOption()
            {
            });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, new WorkOption()
            {
            });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, new WorkOption()
            {
            });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, new WorkOption()
            {
            });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, new WorkOption()
            {
            });

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    if (powerPool.CheckIfRequestedStop())
                    {
                        throw new Exception();
                    }
                }
            }, new WorkOption<object>()
            {
                LongRunning = true,
            });

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    if (powerPool.CheckIfRequestedStop())
                    {
                        throw new Exception();
                    }
                }
            }, new WorkOption<object>()
            {
                LongRunning = true,
            });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, new WorkOption()
            {
            });

            Thread.Sleep(500);

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, new WorkOption()
            {
            });

            Assert.Equal(4, powerPool.RunningWorkerCount);
            Assert.Equal(2, powerPool.LongRunningWorkerCount);

            Thread.Sleep(2000);

            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(2, powerPool.LongRunningWorkerCount);

            powerPool.ForceStop();

            Thread.Sleep(1000);

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.LongRunningWorkerCount);
        }

        [Fact]
        public void TestLIFO()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            List<string> logList = new List<string>();

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                QueueType = QueueType.LIFO,
            };

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("1");
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("2");
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("3");
            });
            WorkID id4 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("4");
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("1", item),
                item => Assert.Equal("4", item),
                item => Assert.Equal("3", item),
                item => Assert.Equal("2", item)
                );
        }

        [Fact]
        public void TestFIFO()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            List<string> logList = new List<string>();

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                QueueType = QueueType.FIFO,
            };

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("1");
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("2");
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("3");
            });
            WorkID id4 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("4");
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("1", item),
                item => Assert.Equal("2", item),
                item => Assert.Equal("3", item),
                item => Assert.Equal("4", item)
                );
        }

        [Fact]
        public void TestCustomQueueLIFO()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            List<string> logList = new List<string>();

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                CustomQueueFactory = () => new ConcurrentStealablePriorityStack<WorkItemBase>(),
            };

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("1");
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("2");
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("3");
            });
            WorkID id4 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("4");
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("1", item),
                item => Assert.Equal("4", item),
                item => Assert.Equal("3", item),
                item => Assert.Equal("2", item)
                );
        }

        [Fact]
        public void TestCustomQueueFIFO()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            List<string> logList = new List<string>();

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                CustomQueueFactory = () => new ConcurrentStealablePriorityQueue<WorkItemBase>(),
            };

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("1");
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("2");
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("3");
            });
            WorkID id4 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("4");
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("1", item),
                item => Assert.Equal("2", item),
                item => Assert.Equal("3", item),
                item => Assert.Equal("4", item)
                );
        }

        [Fact]
        public void TestImmediateRetry()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            int runCount = 0;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref runCount);
                Assert.Equal(5, e.RetryInfo.MaxRetryCount);
                Assert.Equal(RetryPolicy.Limited, e.RetryInfo.RetryPolicy);
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            }, new WorkOption<object>()
            {
                RetryOption = new RetryOption() { RetryBehavior = RetryBehavior.ImmediateRetry, MaxRetryCount = 5 }
            });

            powerPool.Wait();

            Assert.Equal(6, runCount);
        }

        [Fact]
        public void TestImmediateRetryUnlimited()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            int retryCount = 0;

            powerPool.WorkEnded += (s, e) =>
            {
                retryCount = e.RetryInfo.CurrentRetryCount;
                if (e.RetryInfo.CurrentRetryCount == 100)
                {
                    e.RetryInfo.StopRetry = true;
                }
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            }, new WorkOption<object>()
            {
                RetryOption = new RetryOption() { RetryBehavior = RetryBehavior.ImmediateRetry, RetryPolicy = RetryPolicy.Unlimited }
            });

            powerPool.Wait();

            Assert.Equal(100, retryCount);
        }

        [Fact]
        public void TestImmediateRetryStopRetryByCallback()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            int runCount = 0;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref runCount);
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            }, new WorkOption<object>()
            {
                RetryOption = new RetryOption() { RetryBehavior = RetryBehavior.ImmediateRetry, MaxRetryCount = 5 },
                Callback = (res) =>
                {
                    if (res.Status == Status.Failed)
                    {
                        if (res.RetryInfo.CurrentRetryCount == 2)
                        {
                            res.RetryInfo.StopRetry = true;
                        }
                    }
                }
            });

            powerPool.Wait();

            Assert.Equal(3, runCount);
        }

        [Fact]
        public void TestImmediateRetryStopRetryByEvent()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            int runCount = 0;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref runCount);
                if (!e.Succeed)
                {
                    if (e.RetryInfo.CurrentRetryCount == 2)
                    {
                        e.RetryInfo.StopRetry = true;
                    }
                }
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            }, new WorkOption<object>()
            {
                RetryOption = new RetryOption() { RetryBehavior = RetryBehavior.ImmediateRetry, MaxRetryCount = 5 },
            });

            powerPool.Wait();

            Assert.Equal(3, runCount);
        }

        [Fact]
        public void TestRequeue()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            int runCount = 0;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref runCount);
                Assert.Equal(5, e.RetryInfo.MaxRetryCount);
                Assert.Equal(RetryPolicy.Limited, e.RetryInfo.RetryPolicy);
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            }, new WorkOption<object>()
            {
                RetryOption = new RetryOption() { RetryBehavior = RetryBehavior.Requeue, MaxRetryCount = 5 },
                Callback = (res) =>
                {
                    if (res.Status == Status.Failed)
                    {
                        if (res.RetryInfo.CurrentRetryCount == 2)
                        {
                            res.RetryInfo.StopRetry = true;
                        }
                    }
                }
            });

            powerPool.Wait();

            Assert.Equal(3, runCount);
        }

        [Fact]
        public void TestRequeueStopRetryByCallback()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            int runCount = 0;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref runCount);
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            }, new WorkOption<object>()
            {
                RetryOption = new RetryOption() { RetryBehavior = RetryBehavior.Requeue, MaxRetryCount = 5 }
            });

            powerPool.Wait();

            Assert.Equal(6, runCount);
        }

        [Fact]
        public void TestRequeueStopRetryByEvent()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            int runCount = 0;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref runCount);
                if (!e.Succeed)
                {
                    if (e.RetryInfo.CurrentRetryCount == 2)
                    {
                        e.RetryInfo.StopRetry = true;
                    }
                }
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            }, new WorkOption<object>()
            {
                RetryOption = new RetryOption() { RetryBehavior = RetryBehavior.Requeue, MaxRetryCount = 5 },
            });

            powerPool.Wait();

            Assert.Equal(3, runCount);
        }

        [Fact]
        public void TestRequeueUnlimited()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            int retryCount = 0;

            powerPool.WorkEnded += (s, e) =>
            {
                retryCount = e.RetryInfo.CurrentRetryCount;
                if (e.RetryInfo.CurrentRetryCount == 100)
                {
                    e.RetryInfo.StopRetry = true;
                }
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            }, new WorkOption<object>()
            {
                RetryOption = new RetryOption() { RetryBehavior = RetryBehavior.Requeue, RetryPolicy = RetryPolicy.Unlimited }
            });

            powerPool.Wait();

            Assert.Equal(100, retryCount);
        }

        [Fact]
        public void TestPoolIdledEventArgs()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                EnableStatisticsCollection = true,
            });

            DateTime startDateTime = DateTime.MinValue;
            DateTime endDateTime = DateTime.MinValue;
            TimeSpan runtimeDuration = TimeSpan.MinValue;
            powerPool.PoolIdled += (s, e) =>
            {
                startDateTime = e.StartDateTime;
                endDateTime = e.EndDateTime;
                runtimeDuration = e.RuntimeDuration;
            };

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(50);
                return;
            });

            powerPool.Wait();

            Assert.NotEqual(startDateTime, DateTime.MinValue);
            Assert.NotEqual(endDateTime, DateTime.MinValue);
            Assert.NotEqual(runtimeDuration, TimeSpan.MinValue);
            Assert.Equal(runtimeDuration, endDateTime - startDateTime);
        }

        [Fact]
        public void TestRunningWorkerCountChanged()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            int count0 = -1;
            int count1 = -1;
            int count2 = -1;
            int count3 = -1;
            powerPool.RunningWorkerCountChanged += (s, e) =>
            {
                if (e.PreviousCount < e.NowCount)
                {
                    count0 = e.PreviousCount;
                    count1 = e.NowCount;
                }
                else
                {
                    count2 = e.PreviousCount;
                    count3 = e.NowCount;
                }
            };

            powerPool.QueueWorkItem(() =>
            {
                return;
            });

            powerPool.Wait();

            Assert.Equal(0, count0);
            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
            Assert.Equal(0, count3);
        }

        [Fact]
        public void TestErrorWhenCallback()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.QueueWorkItem(() =>
            {
                return;
            }, (res) =>
            {
                throw new Exception();
            });

            powerPool.Wait();

            Assert.Equal(ErrorFrom.Callback, errorFrom);
        }

        [Fact]
        public void TestErrorWhenDefaultCallback()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool()
            {
                PowerPoolOption = new PowerPoolOption()
                {
                    DefaultCallback = (res) =>
                    {
                        throw new Exception();
                    }
                }
            };

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.QueueWorkItem(() =>
            {
                return;
            });

            powerPool.Wait();

            Assert.Equal(ErrorFrom.DefaultCallback, errorFrom);
        }

        [Fact]
        public void TestErrorWhenPoolStarted()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.PoolStarted += (s, e) =>
            {
                throw new Exception();
            };

            powerPool.QueueWorkItem(() =>
            {
                return;
            });

            powerPool.Wait();

            Assert.Equal(ErrorFrom.PoolStarted, errorFrom);
        }

        [Fact]
        public void TestErrorWhenPoolIdled()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.PoolIdled += (s, e) =>
            {
                throw new Exception();
            };

            powerPool.QueueWorkItem(() =>
            {
                return;
            });

            powerPool.Wait();

            Assert.Equal(ErrorFrom.PoolIdled, errorFrom);
        }

        [Fact]
        public void TestErrorWhenWorkStarted()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.WorkStarted += (s, e) =>
            {
                throw new Exception();
            };

            powerPool.QueueWorkItem(() =>
            {
                return;
            });

            powerPool.Wait();

            Assert.Equal(ErrorFrom.WorkStarted, errorFrom);
        }

        [Fact]
        public void TestErrorWhenWorkEnded()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.WorkEnded += (s, e) =>
            {
                throw new Exception();
            };

            powerPool.QueueWorkItem(() =>
            {
                return;
            });

            powerPool.Wait();

            Assert.Equal(ErrorFrom.WorkEnded, errorFrom);
        }

        [Fact]
        public void TestErrorWhenPoolTimedOut()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool()
            {
                PowerPoolOption = new PowerPoolOption()
                {
                    TimeoutOption = new TimeoutOption()
                    {
                        Duration = 100,
                    }
                }
            };

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.PoolTimedOut += (s, e) =>
            {
                throw new Exception();
            };

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                }
            });

            powerPool.Wait();

            Assert.Equal(ErrorFrom.PoolTimedOut, errorFrom);
        }

        [Fact]
        public void TestErrorWhenWorkTimedOut()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.WorkTimedOut += (s, e) =>
            {
                throw new Exception();
            };

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                }
            }, new WorkOption()
            {
                TimeoutOption = new TimeoutOption()
                {
                    Duration = 100,
                }
            });

            powerPool.Wait();

            Assert.Equal(ErrorFrom.WorkTimedOut, errorFrom);
        }

        [Fact]
        public void TestErrorWhenWorkStopped()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.WorkStopped += (s, e) =>
            {
                throw new Exception();
            };

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(100);
                    powerPool.StopIfRequested();
                }
            });

            powerPool.Stop();
            powerPool.Wait();

            Assert.Equal(ErrorFrom.WorkStopped, errorFrom);
        }

        [Fact]
        public void TestErrorWhenWorkCanceled()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool()
            {
                PowerPoolOption = new PowerPoolOption()
                {
                    MaxThreads = 1
                }
            };

            ErrorFrom errorFrom = ErrorFrom.WorkLogic;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.WorkCanceled += (s, e) =>
            {
                throw new Exception();
            };

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            powerPool.QueueWorkItem(() =>
            {
                return;
            });

            powerPool.Cancel();
            powerPool.Wait();

            Assert.Equal(ErrorFrom.WorkCanceled, errorFrom);
        }

        [Fact]
        public void TestErrorWhenWorkLogic()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ErrorFrom errorFrom = ErrorFrom.Callback;

            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            });

            powerPool.Wait();

            Assert.Equal(ErrorFrom.WorkLogic, errorFrom);
        }

        [Fact]
        public void TestTimes()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption()
            {
                StartSuspended = true,
                MaxThreads = 2,
                EnableStatisticsCollection = true,
            });

            powerPool.QueueWorkItem(() =>
            {
            });
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(2000);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(3000);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(4000);
            });

            powerPool.Start();

            powerPool.Wait(id);
            Assert.True(powerPool.RuntimeDuration.TotalMilliseconds > 0);

            powerPool.Wait();

            Assert.True(powerPool.TotalQueueTime > 0);
            Assert.True(powerPool.TotalExecuteTime > 0);
            Assert.Equal(powerPool.AverageQueueTime, powerPool.TotalQueueTime / 5);
            Assert.Equal(powerPool.AverageExecuteTime, powerPool.TotalExecuteTime / 5);
            Assert.Equal(powerPool.AverageElapsedTime, powerPool.AverageQueueTime + powerPool.AverageExecuteTime);
            Assert.Equal(powerPool.TotalElapsedTime, powerPool.TotalQueueTime + powerPool.TotalExecuteTime);
            Assert.True(powerPool.RuntimeDuration.TotalMilliseconds > 0);
        }

        [Fact]
        public void TestGetTimesBeforePoolStart()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { StartSuspended = true, MaxThreads = 2 });

            Assert.Equal(0, powerPool.TotalQueueTime);
            Assert.Equal(0, powerPool.TotalExecuteTime);
            Assert.Equal(0, powerPool.AverageQueueTime);
            Assert.Equal(0, powerPool.AverageExecuteTime);
            Assert.Equal(0, powerPool.AverageElapsedTime);
            Assert.Equal(0, powerPool.TotalElapsedTime);
            Assert.Equal(TimeSpan.Zero, powerPool.RuntimeDuration);
        }

        [Fact]
        public void TestClearFailedWorkRecord()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 }
            };

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            });

            powerPool.Wait();
            Assert.Equal(1, powerPool.FailedWorkCount);

            powerPool.ClearFailedWorkRecord();
            Assert.Equal(0, powerPool.FailedWorkCount);
        }

        [Fact]
        public void TestClearResultStorage()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            powerPool.EnablePoolIdleCheck = false;

            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 }
            };

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                return "0";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            powerPool.EnablePoolIdleCheck = true;

            ExecuteResult<string> res = powerPool.Fetch<string>(id0);

            Assert.Equal("0", res.Result);
            Assert.True(res.IsFound);

            powerPool.ClearResultStorage();
            res = powerPool.Fetch<string>(id0);
            Assert.Null(res.Result);
            Assert.False(res.IsFound);
        }

        [Fact]
        public void TestClearResultStorageByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 }
            };

            powerPool.EnablePoolIdleCheck = false;

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                return "0";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                return "1";
            }, new WorkOption()
            {
                ShouldStoreResult = true
            });

            powerPool.EnablePoolIdleCheck = true;

            powerPool.Wait();

            ExecuteResult<string> res0 = powerPool.Fetch<string>(id0);
            Assert.Equal("0", res0.Result);
            ExecuteResult<string> res1 = powerPool.Fetch<string>(id1);
            Assert.Equal("1", res1.Result);

            powerPool.ClearResultStorage(id0);
            res0 = powerPool.Fetch<string>(id0);
            res1 = powerPool.Fetch<string>(id1);
            Assert.Null(res0.Result);
            Assert.Equal("1", res1.Result);
        }

        [Fact]
        public void TestClearResultStorageByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 }
            };

            powerPool.EnablePoolIdleCheck = false;

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                return "0";
            }, new WorkOption()
            {
                ShouldStoreResult = true,
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                return "1";
            }, new WorkOption()
            {
                ShouldStoreResult = true,
            });

            powerPool.EnablePoolIdleCheck = true;

            powerPool.Wait();

            ExecuteResult<string> res0 = powerPool.Fetch<string>(id0);
            Assert.Equal("0", res0.Result);
            ExecuteResult<string> res1 = powerPool.Fetch<string>(id1);
            Assert.Equal("1", res1.Result);

            powerPool.ClearResultStorage(new List<WorkID> { id0 });
            res0 = powerPool.Fetch<string>(id0);
            res1 = powerPool.Fetch<string>(id1);
            Assert.Null(res0.Result);
            Assert.Equal("1", res1.Result);
        }

        [Fact]
        public void TestWorkGroupRelation()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });

            powerPool.SetGroupRelation("A", "B");

            powerPool.GetGroup("A").Stop();
            powerPool.GetGroup("A").Wait();
            Thread.Sleep(100);
            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public void TestWorkGroupRelationStopChild()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });

            powerPool.SetGroupRelation("A", "B");

            powerPool.GetGroup("B").Stop();
            powerPool.GetGroup("B").Wait();
            Thread.Sleep(100);
            Assert.Equal(2, powerPool.RunningWorkerCount);

            powerPool.GetGroup("A").Stop();
            powerPool.GetGroup("A").Wait();
            Thread.Sleep(100);
            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public void TestWorkGroupRelationRemoveGroupRelation()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });

            powerPool.SetGroupRelation("A", "B");
            bool res = powerPool.RemoveGroupRelation("A", "B");
            Assert.True(res);

            powerPool.GetGroup("A").Stop();
            powerPool.GetGroup("A").Wait();
            Thread.Sleep(10);
            Assert.Equal(2, powerPool.RunningWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestWorkGroupRelationRemoveWholeGroupRelation()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });

            powerPool.SetGroupRelation("A", "B");
            powerPool.RemoveGroupRelation("A");

            powerPool.GetGroup("A").Stop();
            powerPool.GetGroup("A").Wait();
            Thread.Sleep(10);
            Assert.Equal(2, powerPool.RunningWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestWorkGroupRelationRemoveChildGroupRelation()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });

            powerPool.SetGroupRelation("A", "B");
            bool res = powerPool.RemoveGroupRelation("B", "A");
            Assert.False(res);

            powerPool.GetGroup("A").Stop();
            powerPool.GetGroup("A").Wait();
            Thread.Sleep(100);
            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public void TestWorkGroupRelationRemoveWholeChildGroupRelation()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });

            powerPool.SetGroupRelation("A", "B");
            powerPool.RemoveGroupRelation("B");

            powerPool.GetGroup("A").Stop();
            powerPool.GetGroup("A").Wait();
            Thread.Sleep(10);
            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public void TestWorkGroupRelationResetGroupRelation()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });

            powerPool.SetGroupRelation("A", "B");
            powerPool.ResetGroupRelation();

            powerPool.GetGroup("A").Stop();
            powerPool.GetGroup("A").Wait();
            Thread.Sleep(10);
            Assert.Equal(2, powerPool.RunningWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestWorkGroupRelationCyclicGroupRelation()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "A"
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    powerPool.StopIfRequested();
                }
            }, new WorkOption<object>()
            {
                Group = "B"
            });

            InvalidOperationException e = null;

            powerPool.SetGroupRelation("A", "B");
            try
            {
                powerPool.SetGroupRelation("B", "A");
            }
            catch (InvalidOperationException ex)
            {
                e = ex;
            }

            Assert.Equal($"Cannot create a cyclic group relation: 'B' is already a subgroup of 'A'.", e.Message);

            powerPool.Stop();
        }

        [Fact]
        public void TestAddWorkToGroup()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            bool res = powerPool.AddWorkToGroup("AAA", id);
            Assert.True(res);

            Assert.Contains(id, powerPool.GetGroupMemberList("AAA"));
            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.GetGroup("AAA").Stop();

            Thread.Sleep(100);

            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public void TestAddWorkToGroupWorkNotExist()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            bool res = powerPool.AddWorkToGroup("AAA", WorkID.FromString("AAA"));
            Assert.False(res);

            Assert.DoesNotContain(id, powerPool.GetGroupMemberList("AAA"));
            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.GetGroup("AAA").Stop();

            Thread.Sleep(100);

            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestRemoveWorkFromGroup()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
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

            Assert.Contains(id, powerPool.GetGroupMemberList("AAA"));

            bool res = powerPool.RemoveWorkFromGroup("AAA", id);
            Assert.True(res);

            Assert.DoesNotContain(id, powerPool.GetGroupMemberList("AAA"));
            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.GetGroup("AAA").Stop();

            Thread.Sleep(100);

            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestRemoveWorkFromGroupWorkNotExist()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
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

            Assert.Contains(id, powerPool.GetGroupMemberList("AAA"));

            bool res = powerPool.RemoveWorkFromGroup("AAA", WorkID.FromString("AAA"));
            Assert.False(res);

            Assert.Contains(id, powerPool.GetGroupMemberList("AAA"));
            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.GetGroup("AAA").Stop();

            Thread.Sleep(100);

            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public void TestRemoveWorkFromGroupGroupNotExist()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
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

            Assert.Contains(id, powerPool.GetGroupMemberList("AAA"));

            bool res = powerPool.RemoveWorkFromGroup("BBB", id);
            Assert.False(res);

            Assert.Contains(id, powerPool.GetGroupMemberList("AAA"));
            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.GetGroup("AAA").Stop();

            Thread.Sleep(100);

            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public void TestRemoveWorkFromGroupWorkNotBelong()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
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

            Assert.Contains(id, powerPool.GetGroupMemberList("AAA"));

            bool res = powerPool.RemoveWorkFromGroup("AAA", id);
            Assert.True(res);
            res = powerPool.RemoveWorkFromGroup("AAA", id);
            Assert.False(res);

            Assert.DoesNotContain(id, powerPool.GetGroupMemberList("AAA"));
            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.GetGroup("AAA").Stop();

            Thread.Sleep(100);

            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestAddWorkToGroupByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            bool res = powerPool.GetGroup("AAA").Add(id);
            Assert.True(res);

            Assert.Contains(id, powerPool.GetGroupMemberList("AAA"));
            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.GetGroup("AAA").Stop();

            Thread.Sleep(100);

            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public void TestRemoveWorkFromGroupByGroupObject()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItem(() =>
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

            Assert.Contains(id, powerPool.GetGroupMemberList("AAA"));

            bool res = powerPool.GetGroup("AAA").Remove(id);
            Assert.True(res);

            Assert.DoesNotContain(id, powerPool.GetGroupMemberList("AAA"));
            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.GetGroup("AAA").Stop();

            Thread.Sleep(100);

            Assert.Equal(1, powerPool.RunningWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestParallelFor()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentSet<int> result = new ConcurrentSet<int>();

            powerPool.For(1, 10, (i) => result.Add(i)).Wait();

            Assert.Equal(9, result.Count);
        }

        [Fact]
        public void TestParallelForWithSource()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentSet<int> result = new ConcurrentSet<int>();
            List<int> source = new List<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            powerPool.For<int>(0, 3, source, (item) => result.Add(item)).Wait();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void TestParallelForWithSourceAndIndex()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentSet<int> result = new ConcurrentSet<int>();
            List<int> source = new List<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            powerPool.For<int>(0, 3, source, (item, index) => result.Add(index)).Wait();

            Assert.Contains(0, result);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
        }

        [Fact]
        public void TestParallelForWithSourceAndIndexReverse()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 1 });

            ConcurrentDictionary<int, int> result = new ConcurrentDictionary<int, int>();
            List<int> source = new List<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            int i = 0;

            powerPool.For<int>(2, -1, source, (item, index) => result[i++] = item).Wait();

            Assert.Equal(3, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(1, result[2]);
        }

        [Fact]
        public void TestParallelForGroupName()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentSet<int> result = new ConcurrentSet<int>();

            string name = powerPool.For(1, 10, (i) => result.Add(i), 1, "Group1").Name;

            Assert.Equal("Group1", name);
        }

        [Fact]
        public void TestParallelForError1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentSet<int> result = new ConcurrentSet<int>();
            List<int> source = new List<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            ArgumentException ex = null;
            try
            {
                powerPool.For<int>(0, 3, source, (item, index) => result.Add(index), 0).Wait();
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            Assert.Equal("Step cannot be zero. (Parameter 'step')", ex.Message);
            Assert.Equal("step", ex.ParamName);
        }

        [Fact]
        public void TestParallelForError2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentSet<int> result = new ConcurrentSet<int>();
            List<int> source = new List<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            ArgumentException ex = null;
            try
            {
                powerPool.For<int>(0, 3, source, (item, index) => result.Add(index), -10).Wait();
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            Assert.Equal("Invalid start, end, and step combination. The loop will never terminate. (Parameter 'step')", ex.Message);
            Assert.Equal("step", ex.ParamName);
        }

        [Fact]
        public void TestParallelForError3()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentSet<int> result = new ConcurrentSet<int>();
            List<int> source = new List<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            ArgumentException ex = null;
            try
            {
                powerPool.For<int>(3, 0, source, (item, index) => result.Add(index), 10).Wait();
            }
            catch (ArgumentException e)
            {
                ex = e;
            }
            Assert.Equal("Invalid start, end, and step combination. The loop will never terminate. (Parameter 'step')", ex.Message);
            Assert.Equal("step", ex.ParamName);
        }

        [Fact]
        public void TestParallelForEach()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            List<int> list = new List<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            powerPool.ForEach(list, (i) => result.Add(i)).Wait();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void TestParallelForEachWithIndex()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            List<int> list = new List<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            powerPool.ForEach(list, (i, index) => result.Add(index)).Wait();

            Assert.Contains(0, result);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
        }

        [Fact]
        public void TestParallelForEachGroupID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            List<int> list = new List<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            string groupName = powerPool.ForEach(list, (i) => result.Add(i), "Group1").Name;

            Assert.Equal("Group1", groupName);
        }

        [Fact]
        public void TestParallelWatch()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) => result.Add(i));

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.Wait();

            Assert.Equal(6, result.Count);
        }

        [Fact]
        public void TestParallelWatchConcurrentBag()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            bag.Add(1);
            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>(bag);
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) => result.Add(i));

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.Wait();

            Assert.Equal(6, result.Count);
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void TestParallelWatchBlockingCollection()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            BlockingCollection<int> bag = new BlockingCollection<int>();
            bag.Add(1);
            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>(bag);
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) => result.Add(i));

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.Wait();

            Assert.Equal(6, result.Count);
        }

        [Fact]
        public void TestParallelWatchGroupID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>(new ConcurrentBag<int>());
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            string groupName = powerPool.Watch(list, (i) => result.Add(i), true, true, true, "Group1").Name;

            Assert.Equal("Group1", groupName);
        }

        [Fact]
        public void TestStopWatching()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) => result.Add(i));

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.StopWatching(list);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(6, result.Count);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void TestStopWatchingBeforeWatching()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.StopWatching(list);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(0, result.Count);
            Assert.Equal(9, list.Count);
        }

        [Fact]
        public void TestStopWatchingKeepRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) => result.Add(i));

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            Thread.Sleep(1);

            powerPool.StopWatching(list, true);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(6, result.Count);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void TestStopWatchingDirectly()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) => result.Add(i));

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            Thread.Sleep(1);

            list.StopWatching(true);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(6, result.Count);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void TestStopWatchingHalfFailed()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) =>
            {
                if (i % 2 == 1)
                {
                    result.Add(i);
                }
                else
                {
                    throw new Exception();
                }
            });

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.StopWatching(list);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(3, result.Count);
            Assert.Equal(6, list.Count);
        }

        [Fact]
        public void TestStopWatchingCancel()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) =>
            {
                Thread.Sleep(1000);
                result.Add(i);
            });

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.StopWatching(list);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(2, result.Count);
            Assert.Equal(7, list.Count);
        }

        [Fact]
        public void TestStopWatchingForceStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) =>
            {
                Thread.Sleep(1000000);
                result.Add(i);
            });

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.ForceStopWatching(list, false);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(0, result.Count);
            Assert.Equal(9, list.Count);
        }

        [Fact]
        public void TestStopWatchingHalfFailedNotAddBack()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) =>
            {
                if (i % 2 == 1)
                {
                    result.Add(i);
                }
                else
                {
                    throw new Exception();
                }
            }, false, false, false);

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.StopWatching(list);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(3, result.Count);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void TestStopWatchingCancelNotAddBack()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) =>
            {
                Thread.Sleep(1000);
                result.Add(i);
            }, false, false, false);

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.StopWatching(list);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(2, result.Count);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void TestStopWatchingForceStopNotAddBack()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            powerPool.Watch(list, (i) =>
            {
                Thread.Sleep(1000000);
                result.Add(i);
            }, false, false, false);

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.ForceStopWatching(list, false);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(0, result.Count);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void TestWatchTwice()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });

            ConcurrentObservableCollection<int> list = new ConcurrentObservableCollection<int>();
            ConcurrentSet<int> result = new ConcurrentSet<int>();
            list.TryAdd(1);
            list.TryAdd(2);
            list.TryAdd(3);

            Group group1 = powerPool.Watch(list, (i) =>
            {
                Thread.Sleep(1000000);
                result.Add(i);
            });

            Group group2 = powerPool.Watch(list, (i) =>
            {
                Thread.Sleep(1000000);
                result.Add(i);
            });

            list.TryAdd(4);
            list.TryAdd(5);
            list.TryAdd(6);

            powerPool.ForceStopWatching(list, false);

            list.TryAdd(7);
            list.TryAdd(8);
            list.TryAdd(9);

            powerPool.Wait();

            Assert.Equal(0, result.Count);
            Assert.Equal(9, list.Count);
            Assert.NotNull(group1);
            Assert.Null(group2);
        }

        [Fact]
        public void TestRunningTimer()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            DateTime d0 = DateTime.MinValue;
            DateTime d1 = DateTime.MinValue;
            DateTime d2 = DateTime.MinValue;
            DateTime d3 = DateTime.MinValue;
            DateTime d4 = DateTime.MinValue;
            DateTime d5 = DateTime.MinValue;
            DateTime d6 = DateTime.MinValue;

            TimeSpan t0 = TimeSpan.MinValue;
            TimeSpan t1 = TimeSpan.MinValue;
            TimeSpan t2 = TimeSpan.MinValue;
            TimeSpan t3 = TimeSpan.MinValue;
            TimeSpan t4 = TimeSpan.MinValue;
            TimeSpan t5 = TimeSpan.MinValue;
            TimeSpan t6 = TimeSpan.MinValue;

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = 1,
                RunningTimerOption = new RunningTimerOption
                {
                    Elapsed = (e) =>
                    {
                        if (d0 == DateTime.MinValue)
                        {
                            d0 = e.SignalTime;
                        }
                        else if (d1 == DateTime.MinValue)
                        {
                            d1 = e.SignalTime;
                        }
                        else if (d2 == DateTime.MinValue)
                        {
                            d2 = e.SignalTime;
                        }
                        else if (d3 == DateTime.MinValue)
                        {
                            d3 = e.SignalTime;
                        }
                        else if (d4 == DateTime.MinValue)
                        {
                            d4 = e.SignalTime;
                        }
                        else if (d5 == DateTime.MinValue)
                        {
                            d5 = e.SignalTime;
                        }
                        else if (d6 == DateTime.MinValue)
                        {
                            d6 = e.SignalTime;
                        }

                        if (t0 == TimeSpan.MinValue)
                        {
                            t0 = e.RuntimeDuration;
                        }
                        else if (t1 == TimeSpan.MinValue)
                        {
                            t1 = e.RuntimeDuration;
                        }
                        else if (t2 == TimeSpan.MinValue)
                        {
                            t2 = e.RuntimeDuration;
                        }
                        else if (t3 == TimeSpan.MinValue)
                        {
                            t3 = e.RuntimeDuration;
                        }
                        else if (t4 == TimeSpan.MinValue)
                        {
                            t4 = e.RuntimeDuration;
                        }
                        else if (t5 == TimeSpan.MinValue)
                        {
                            t5 = e.RuntimeDuration;
                        }
                        else if (t6 == TimeSpan.MinValue)
                        {
                            t6 = e.RuntimeDuration;
                        }
                    },
                    Interval = 500,
                },
                EnableStatisticsCollection = true,
            });

            Assert.Equal(DateTime.MinValue, d0);
            Assert.Equal(DateTime.MinValue, d1);
            Assert.Equal(DateTime.MinValue, d2);
            Assert.Equal(DateTime.MinValue, d3);
            Assert.Equal(DateTime.MinValue, d4);
            Assert.Equal(DateTime.MinValue, d5);
            Assert.Equal(DateTime.MinValue, d6);

            Assert.Equal(TimeSpan.MinValue, t0);
            Assert.Equal(TimeSpan.MinValue, t1);
            Assert.Equal(TimeSpan.MinValue, t2);
            Assert.Equal(TimeSpan.MinValue, t3);
            Assert.Equal(TimeSpan.MinValue, t4);
            Assert.Equal(TimeSpan.MinValue, t5);
            Assert.Equal(TimeSpan.MinValue, t6);

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1100);
            });

            powerPool.Wait();

            Assert.NotEqual(DateTime.MinValue, d0);
            Assert.NotEqual(DateTime.MinValue, d1);
            Assert.Equal(DateTime.MinValue, d2);
            Assert.Equal(DateTime.MinValue, d3);
            Assert.Equal(DateTime.MinValue, d4);
            Assert.Equal(DateTime.MinValue, d5);
            Assert.Equal(DateTime.MinValue, d6);

            Assert.NotEqual(TimeSpan.MinValue, t0);
            Assert.NotEqual(TimeSpan.MinValue, t1);
            Assert.Equal(TimeSpan.MinValue, t2);
            Assert.Equal(TimeSpan.MinValue, t3);
            Assert.Equal(TimeSpan.MinValue, t4);
            Assert.Equal(TimeSpan.MinValue, t5);
            Assert.Equal(TimeSpan.MinValue, t6);

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1100);
            });

            powerPool.Wait();

            Assert.NotEqual(DateTime.MinValue, d0);
            Assert.NotEqual(DateTime.MinValue, d1);
            Assert.NotEqual(DateTime.MinValue, d2);
            Assert.NotEqual(DateTime.MinValue, d3);
            Assert.Equal(DateTime.MinValue, d4);
            Assert.Equal(DateTime.MinValue, d5);
            Assert.Equal(DateTime.MinValue, d6);

            Assert.NotEqual(TimeSpan.MinValue, t0);
            Assert.NotEqual(TimeSpan.MinValue, t1);
            Assert.NotEqual(TimeSpan.MinValue, t2);
            Assert.NotEqual(TimeSpan.MinValue, t3);
            Assert.Equal(TimeSpan.MinValue, t4);
            Assert.Equal(TimeSpan.MinValue, t5);
            Assert.Equal(TimeSpan.MinValue, t6);

            powerPool.PowerPoolOption.RunningTimerOption.Interval = 200;

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1100);
            });

            powerPool.Wait();

            Assert.NotEqual(DateTime.MinValue, d0);
            Assert.NotEqual(DateTime.MinValue, d1);
            Assert.NotEqual(DateTime.MinValue, d2);
            Assert.NotEqual(DateTime.MinValue, d3);
            Assert.NotEqual(DateTime.MinValue, d4);
            Assert.NotEqual(DateTime.MinValue, d5);
            Assert.NotEqual(DateTime.MinValue, d6);

            Assert.NotEqual(TimeSpan.MinValue, t0);
            Assert.NotEqual(TimeSpan.MinValue, t1);
            Assert.NotEqual(TimeSpan.MinValue, t2);
            Assert.NotEqual(TimeSpan.MinValue, t3);
            Assert.NotEqual(TimeSpan.MinValue, t4);
            Assert.NotEqual(TimeSpan.MinValue, t5);
            Assert.NotEqual(TimeSpan.MinValue, t6);

            powerPool.PowerPoolOption.RunningTimerOption = null;

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            });

            powerPool.Wait();
        }

        [Fact]
        public void TestSetMaxThreadsWhenRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            int stopCount1 = 0;
            int cancelCount1 = 0;
            int doneCount2 = 0;

            powerPool.WorkStopped += (s, e) => { Interlocked.Increment(ref stopCount1); };
            powerPool.WorkCanceled += (s, e) => { Interlocked.Increment(ref cancelCount1); };
            powerPool.PowerPoolOption.DefaultCallback = (e) => { Interlocked.Increment(ref doneCount2); };

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 2;

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 1;

            Thread.Sleep(500);

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.Stop(id2);

            Thread.Sleep(500);

            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 2;

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 1;

            powerPool.Stop(id1);
            Thread.Sleep(1000);

            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.Stop();

            Thread.Sleep(600);
            Assert.Equal(3, stopCount1);
            Assert.Equal(5, cancelCount1);
            Assert.Equal(8, doneCount2);
        }

        [Fact]
        public void TestSetMaxThreadsWhenRunningHasWaitingWork()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            int stopCount1 = 0;
            int cancelCount1 = 0;
            int doneCount2 = 0;

            powerPool.WorkStopped += (s, e) => { Interlocked.Increment(ref stopCount1); };
            powerPool.WorkCanceled += (s, e) => { Interlocked.Increment(ref cancelCount1); };
            powerPool.PowerPoolOption.DefaultCallback = (e) => { Interlocked.Increment(ref doneCount2); };

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 2;

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 1;

            Thread.Sleep(500);

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.Stop(id1);

            Thread.Sleep(500);

            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 2;

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 1;

            powerPool.Stop(id2);
            Thread.Sleep(1000);

            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.Stop();

            Thread.Sleep(600);
            Assert.Equal(3, stopCount1);
            Assert.Equal(5, cancelCount1);
            Assert.Equal(8, doneCount2);
        }

        [Fact]
        public void TestSetMaxThreadsWhenRunningAndStealWork()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            int stopCount1 = 0;
            int cancelCount1 = 0;
            int doneCount2 = 0;

            powerPool.WorkStopped += (s, e) => { Interlocked.Increment(ref stopCount1); };
            powerPool.WorkCanceled += (s, e) => { Interlocked.Increment(ref cancelCount1); };
            powerPool.PowerPoolOption.DefaultCallback = (e) => { Interlocked.Increment(ref doneCount2); };

            powerPool.PowerPoolOption.MaxThreads = 1;

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 4;

            Thread.Sleep(500);

            Assert.Equal(4, powerPool.AliveWorkerCount);
            Assert.Equal(4, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.MaxThreads = 10;

            Thread.Sleep(500);
            Assert.Equal(10, powerPool.AliveWorkerCount);
            Assert.Equal(9, powerPool.RunningWorkerCount);
            Assert.Equal(1, powerPool.IdleWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestSetMinThreadsWhenRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 100, DestroyThreadOption = new DestroyThreadOption { MinThreads = 1 } });

            int stopCount1 = 0;
            int cancelCount1 = 0;
            int doneCount2 = 0;

            powerPool.WorkStopped += (s, e) => { Interlocked.Increment(ref stopCount1); };
            powerPool.WorkCanceled += (s, e) => { Interlocked.Increment(ref cancelCount1); };
            powerPool.PowerPoolOption.DefaultCallback = (e) => { Interlocked.Increment(ref doneCount2); };

            powerPool.PowerPoolOption.DestroyThreadOption.MinThreads = 10;

            Assert.Equal(10, powerPool.AliveWorkerCount);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(10, powerPool.IdleWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestSetMaxThreadsAfterDispose()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            int stopCount1 = 0;
            int cancelCount1 = 0;
            int doneCount2 = 0;

            powerPool.WorkStopped += (s, e) => { Interlocked.Increment(ref stopCount1); };
            powerPool.WorkCanceled += (s, e) => { Interlocked.Increment(ref cancelCount1); };
            powerPool.PowerPoolOption.DefaultCallback = (e) => { Interlocked.Increment(ref doneCount2); };

            powerPool.PowerPoolOption.MaxThreads = 1;

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.Dispose();

            powerPool.PowerPoolOption.MaxThreads = 4;

            Thread.Sleep(500);

            Assert.Equal(0, powerPool.AliveWorkerCount);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);
        }

        [Fact]
        public void TestChangeNewPowerPoolOption()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            int stopCount1 = 0;
            int cancelCount1 = 0;
            int doneCount2 = 0;

            powerPool.WorkStopped += (s, e) => { Interlocked.Increment(ref stopCount1); };
            powerPool.WorkCanceled += (s, e) => { Interlocked.Increment(ref cancelCount1); };
            powerPool.PowerPoolOption.DefaultCallback = (e) => { Interlocked.Increment(ref doneCount2); };

            powerPool.PowerPoolOption.MaxThreads = 1;

            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(1, powerPool.AliveWorkerCount);
            Assert.Equal(1, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption = new PowerPoolOption { MaxThreads = 4 };

            Thread.Sleep(500);

            Assert.Equal(4, powerPool.AliveWorkerCount);
            Assert.Equal(4, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption = new PowerPoolOption { MaxThreads = 10 };

            Thread.Sleep(500);
            Assert.Equal(10, powerPool.AliveWorkerCount);
            Assert.Equal(9, powerPool.RunningWorkerCount);
            Assert.Equal(1, powerPool.IdleWorkerCount);

            powerPool.Stop();
        }

        [Fact]
        public void TestSetDestroyThreadOptionWhenRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 2, DestroyThreadOption = new DestroyThreadOption { KeepAliveTime = 1000, MinThreads = 0 } });

            int stopCount1 = 0;
            int cancelCount1 = 0;
            int doneCount2 = 0;

            powerPool.WorkStopped += (s, e) => { Interlocked.Increment(ref stopCount1); };
            powerPool.WorkCanceled += (s, e) => { Interlocked.Increment(ref cancelCount1); };
            powerPool.PowerPoolOption.DefaultCallback = (e) => { Interlocked.Increment(ref doneCount2); };

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.Stop(id1);
            Thread.Sleep(100);
            powerPool.Stop(id2);

            Thread.Sleep(500);

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(2, powerPool.IdleWorkerCount);

            Thread.Sleep(2000);

            Assert.Equal(0, powerPool.AliveWorkerCount);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(2, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.PowerPoolOption.DestroyThreadOption = null;

            powerPool.Stop(id1);
            Thread.Sleep(100);
            powerPool.Stop(id2);

            Thread.Sleep(2000);

            Assert.Equal(2, powerPool.AliveWorkerCount);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(2, powerPool.IdleWorkerCount);
        }

        [Fact]
        public void TestKeepAliveTimeIsZero()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 2, DestroyThreadOption = new DestroyThreadOption { KeepAliveTime = 0, MinThreads = 0 } });

            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    powerPool.StopIfRequested();
                }
            });

            powerPool.Stop();

            Thread.Sleep(100);

            Assert.Equal(0, powerPool.AliveWorkerCount);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.IdleWorkerCount);
        }

        [Fact]
        public void TestDisposeSelfShouldSetCanGetWorkToAllowedWhenStateTransitionFails()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption
                {
                    MinThreads = 1,
                    KeepAliveTime = 100000
                }
            };

            PowerPool powerPool = new PowerPool(powerPoolOption);

            powerPool.QueueWorkItem(() => { Thread.Sleep(1000); });
            powerPool.QueueWorkItem(() => { Thread.Sleep(1000); });

            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.Wait();

            Assert.Equal(2, powerPool.IdleWorkerCount);

            var worker = new Worker(powerPool);

            worker.CanGetWork.InterlockedValue = CanGetWork.Allowed;
            worker.WorkerState.InterlockedValue = WorkerStates.ToBeDisposed;

            worker.TryDisposeSelf(isIdle: true);

            Assert.Equal(CanGetWork.Allowed, worker.CanGetWork.InterlockedValue);
            Assert.Equal(WorkerStates.ToBeDisposed, worker.WorkerState.InterlockedValue);
        }

        [Fact]
        public void TestDisposeSelfSetDestroyThreadOptionAsNull()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption
                {
                    MinThreads = 1,
                    KeepAliveTime = 100000
                }
            };

            PowerPool powerPool = new PowerPool(powerPoolOption);

            powerPool.QueueWorkItem(() => { Thread.Sleep(1000); });
            powerPool.QueueWorkItem(() => { Thread.Sleep(1000); });

            powerPoolOption.DestroyThreadOption = null;

            Assert.Equal(0, powerPool.IdleWorkerCount);

            powerPool.Wait();

            Assert.Equal(2, powerPool.IdleWorkerCount);

            var worker = new Worker(powerPool);

            worker.CanGetWork.InterlockedValue = CanGetWork.Allowed;
            worker.WorkerState.InterlockedValue = WorkerStates.ToBeDisposed;

            worker.TryDisposeSelf(isIdle: true);

            Assert.Equal(CanGetWork.Allowed, worker.CanGetWork.InterlockedValue);
            Assert.Equal(WorkerStates.ToBeDisposed, worker.WorkerState.InterlockedValue);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestRunWorkGuardTests5Times()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            for (int i = 0; i < 5; ++i)
            {
                await TestWorkGuardFreezeLoopAsync();
                TestWorkGuardFreezeNotLoop();
            }
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestWorkGuardFreezeLoopAsync()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption());
            WorkOption<string> workOption = new WorkOption<string>();
            Work<string> work = new WorkFunc<string>(powerPool, null, () => { return ""; }, workOption);
            work.IsDone = false;
            Worker worker = new Worker(powerPool);
            worker.WorkStealability.InterlockedValue = WorkStealability.NotAllowed;
            work.Worker = worker;
            Task task1 = Task.Run(async () =>
            {
                await Task.Delay(1000);
                work.Worker = null;
                worker.WorkStealability.InterlockedValue = WorkStealability.Allowed;
            });
            Task task2 = Task.Run(async () =>
            {
                await Task.Delay(2000);
                work.Worker = worker;
            });
#if DEBUG
            Spinner.s_enableTimeoutLog = false;
#endif
            WorkGuard workGuard = new WorkGuard(work, true);
#if DEBUG
            Spinner.s_enableTimeoutLog = true;
#endif

            await task1;
            await task2;

            Assert.NotNull(work.Worker);
        }

        [Fact]
        public void TestWorkGuardFreezeNotLoop()
        {
            PowerPool powerPool = new PowerPool(new PowerPoolOption());
            WorkOption<string> workOption = new WorkOption<string>();
            Work<string> work = new WorkFunc<string>(powerPool, null, () => { return ""; }, workOption);
            work.IsDone = false;
            Worker worker = new Worker(powerPool);
            worker.WorkStealability.InterlockedValue = WorkStealability.Allowed;
            work.Worker = worker;
            WorkGuard workGuard = new WorkGuard(work, true);
            Assert.NotNull(work.Worker);
        }

        [Fact]
        public void TestWorkIDType()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption { WorkIDType = WorkIDType.LongIncrement };
            PowerPool powerPool = new PowerPool(powerPoolOption);
            WorkID longID = powerPool.QueueWorkItem(() => { });
            bool parseLong = longID.TryGetLong(out long _);

            powerPoolOption.WorkIDType = WorkIDType.Guid;
            WorkID guidID = powerPool.QueueWorkItem(() => { });
            bool parseGuid = guidID.TryGetGuid(out Guid _);

            Assert.True(parseLong);
            Assert.True(parseGuid);
        }

        [Fact]
        public void TestRejectAbortPolicy()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.AbortPolicy,
                    ThreadQueueLimit = 1,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            WorkID errID = default;
            var exception = Record.Exception(() =>
            {
                _ = powerPool
                    | (() =>
                    {
                        while (true)
                        {
                            powerPool.StopIfRequested();
                            Thread.Sleep(100);
                        }
                    })
                    | (() =>
                    {
                        while (true)
                        {
                            powerPool.StopIfRequested();
                            Thread.Sleep(100);
                        }
                    })
                    | (() =>
                    {
                        while (true)
                        {
                            powerPool.StopIfRequested();
                            Thread.Sleep(100);
                        }
                    })
                    | (() =>
                    {
                        while (true)
                        {
                            powerPool.StopIfRequested();
                            Thread.Sleep(100);
                        }
                    })
                    | (() =>
                    {
                        while (true)
                        {
                            powerPool.StopIfRequested();
                            Thread.Sleep(100);
                        }
                    })
                    | (() =>
                    {
                        while (true)
                        {
                            powerPool.StopIfRequested();
                            Thread.Sleep(100);
                        }
                    })
                    | (() =>
                    {
                        while (true)
                        {
                            powerPool.StopIfRequested();
                            Thread.Sleep(100);
                        }
                    })
                    | (() =>
                    {
                        while (true)
                        {
                            powerPool.StopIfRequested();
                            Thread.Sleep(100);
                        }
                    });

                errID = powerPool.QueueWorkItem(() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                });
            });
            Assert.NotNull(exception);
            Assert.IsType<WorkRejectedException>(exception);
            Assert.False(((WorkRejectedException)exception).ID == null);

            powerPool.Stop();
        }

        [Fact]
        public void TestRejectCallerRunsPolicy()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.CallerRunsPolicy,
                    ThreadQueueLimit = 1,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            _ = powerPool
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                });

            bool done = false;
            powerPool.QueueWorkItem(() =>
            {
                done = true;
            });
            Assert.True(done);
            Assert.Equal(4, powerPool.WaitingWorkCount);

            powerPool.Stop();

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardPolicy()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardPolicy,
                    ThreadQueueLimit = 1,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            _ = powerPool
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                });

            bool done = false;
            powerPool.QueueWorkItem(() =>
            {
                done = true;
            });
            Assert.False(done);
            Assert.Equal(4, powerPool.WaitingWorkCount);

            powerPool.Stop();

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardPolicyWorkDiscardedEvent()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardPolicy,
                    ThreadQueueLimit = 1,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            WorkID discardID = default;
            RejectType rejectType = RejectType.AbortPolicy;
            powerPool.WorkDiscarded += (s, e) =>
            {
                discardID = e.ID;
                rejectType = e.RejectType;
            };

            _ = powerPool
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                });

            bool done = false;
            WorkID id = powerPool.QueueWorkItem(() =>
            {
                done = true;
            });
            Assert.False(done);
            Assert.Equal(4, powerPool.WaitingWorkCount);

            powerPool.Stop();
            powerPool.Wait();

            Assert.False(done);
            Assert.Equal(id, discardID);
            Assert.Equal(RejectType.DiscardPolicy, rejectType);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicy()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 1,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            _ = powerPool
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                })
                | (() =>
                {
                    Thread.Sleep(100);
                });

            bool done = false;
            powerPool.QueueWorkItem(() =>
            {
                done = true;
            });
            Assert.False(done);
            Assert.Equal(4, powerPool.WaitingWorkCount);

            powerPool.Wait();

            Assert.True(done);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicyDiscardOneWorkFail()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 0,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            _ = powerPool
                | (() =>
                {
                    Thread.Sleep(500);
                })
                | (() =>
                {
                    Thread.Sleep(500);
                })
                | (() =>
                {
                    Thread.Sleep(500);
                })
                | (() =>
                {
                    Thread.Sleep(500);
                })
                | (() =>
                {
                })
                | (() =>
                {
                })
                | (() =>
                {
                })
                | (() =>
                {
                });

            bool done = false;
            powerPool.QueueWorkItem(() =>
            {
                done = true;
            });
            Assert.Equal(0, powerPool.WaitingWorkCount);

            powerPool.Wait();

            Assert.True(done);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectEvent()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    ThreadQueueLimit = 1,
                    RejectType = RejectType.DiscardPolicy,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            bool workRejected = false;
            WorkID id = default;
            RejectType rejectType = RejectType.CallerRunsPolicy;
            powerPool.WorkRejected += (s, e) =>
            {
                workRejected = true;
                id = e.ID;
                rejectType = e.RejectType;
            };

            _ = powerPool
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                });

            powerPool.QueueWorkItem(() =>
            {
            });

            powerPool.Stop();

            Assert.True(workRejected);
            Assert.False(id == null);
            Assembly.Equals(RejectType.DiscardPolicy, rejectType);
        }

        [Fact]
        public void TestRejectEventError()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    ThreadQueueLimit = 1,
                    RejectType = RejectType.DiscardPolicy,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            powerPool.WorkRejected += (s, e) =>
            {
                throw new Exception();
            };
            ErrorFrom errorFrom = ErrorFrom.Callback;
            powerPool.ErrorOccurred += (s, e) =>
            {
                errorFrom = e.ErrorFrom;
            };

            _ = powerPool
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                });

            powerPool.QueueWorkItem(() =>
            {
            });

            powerPool.Stop();

            Assert.Equal(ErrorFrom.WorkRejected, errorFrom);
        }

        [Fact]
        public void TestRejectNormalRun()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardPolicy,
                    ThreadQueueLimit = 20,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            _ = powerPool
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                });
            Assert.Equal(15, powerPool.WaitingWorkCount);

            powerPool.Stop();

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectLIFO()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardPolicy,
                    ThreadQueueLimit = 1,
                },
                QueueType = QueueType.LIFO,
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            _ = powerPool
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                })
                | (() =>
                {
                    while (true)
                    {
                        powerPool.StopIfRequested();
                        Thread.Sleep(100);
                    }
                });

            bool done = false;
            powerPool.QueueWorkItem(() =>
            {
                done = true;
            });
            Assert.False(done);
            Assert.Equal(4, powerPool.WaitingWorkCount);

            powerPool.Stop();

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicyGetPriorityLoop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 1,
                },
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            powerPool.QueueWorkItem(() => { }, new WorkOption { WorkPriority = 0 });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption { WorkPriority = 1 });

            bool done = false;
            powerPool.QueueWorkItem(() =>
            {
                done = true;
            }, new WorkOption { WorkPriority = 1 });
            Assert.False(done);
            Assert.True(powerPool.WaitingWorkCount > 0);

            powerPool.Wait();

            Assert.True(done);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicyGetPriorityLoopLIFO()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 1,
                },
                QueueType = QueueType.LIFO,
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            powerPool.QueueWorkItem(() => { }, new WorkOption { WorkPriority = 0 });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption { WorkPriority = 1 });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
            }, new WorkOption { WorkPriority = 1 });

            bool done = false;
            powerPool.QueueWorkItem(() =>
            {
                done = true;
            }, new WorkOption { WorkPriority = 1 });
            Assert.False(done);
            Assert.True(powerPool.WaitingWorkCount > 0);

            powerPool.Wait();

            Assert.True(done);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicyWithDependents()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 4,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 1,
                },
                EnableStatisticsCollection = true,
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            int doneCount = 0;

            WorkID id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            });
            WorkID id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            });
            WorkID id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            });
            WorkID id3 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            });

            WorkID id4 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            });
            WorkID id5 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            });
            WorkID id6 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            });
            WorkID id7 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            });

            WorkID id8 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { id4 }
            });
            WorkID id9 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { id5 }
            });
            WorkID id10 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { id6 }
            });
            WorkID id11 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            }, new WorkOption
            {
                Dependents = new ConcurrentSet<WorkID> { id7 }
            });

            bool done = false;
            powerPool.QueueWorkItem(() =>
            {
                done = true;
            });
            Assert.False(done);

            powerPool.Wait();

            Assert.True(done);
            Assert.Equal(10, doneCount);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicyDiscardAsyncWork1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 1,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 2,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            int doneCount = 0;

            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });

            powerPool.Wait();

            Assert.Equal(3, doneCount);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicyDiscardAsyncWork2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 1,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 2,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            int doneCount = 0;

            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });

            powerPool.Wait();

            Assert.Equal(3, doneCount);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicyDiscardAsyncWork3()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 1,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 2,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            int doneCount = 0;

            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });

            powerPool.Wait();

            Assert.Equal(3, doneCount);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicyDiscardAsyncWork4()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 1,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 2,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            int doneCount = 0;

            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });

            powerPool.Wait();

            Assert.Equal(3, doneCount);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestRejectDiscardOldestPolicyDiscardAsyncWork5()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 1,
                RejectOption = new RejectOption
                {
                    RejectType = RejectType.DiscardOldestPolicy,
                    ThreadQueueLimit = 2,
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            int doneCount = 0;

            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(10);
                }

                Interlocked.Increment(ref doneCount);
            });

            powerPool.Wait();

            Assert.Equal(3, doneCount);

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestNestedDependenciesFailed()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPoolOption powerPoolOption = new PowerPoolOption
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption
                {
                    MinThreads = 1,
                    KeepAliveTime = 0
                }
            };
            PowerPool powerPool = new PowerPool(powerPoolOption);

            WorkID aID = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
                throw new InvalidOperationException("Fail A");
            });

            ConcurrentSet<WorkID> deps1 = new ConcurrentSet<WorkID>(); deps1.Add(aID);
            ConcurrentSet<WorkID> deps2 = new ConcurrentSet<WorkID>(); deps2.Add(aID);

            WorkID b1ID = powerPool.QueueWorkItem(() => { },
                new WorkOption { Dependents = deps1 });

            WorkID b2ID = powerPool.QueueWorkItem(() => { },
                new WorkOption { Dependents = deps2 });

            powerPool.Wait();

            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestDisposeWhenHelping()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 2,
            };

            WorkID id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000000);
            });
            powerPool.QueueWorkItem(() =>
            {
                powerPool.Wait(id, true);
                Thread.Sleep(100000);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000000);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000000);
            });
            Task.Run(() =>
            {
                Thread.Sleep(100);
                powerPool.Dispose();
            });
            powerPool.Wait();
        }

        [Fact]
        public void TestDivideAndConquer50Times()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            for (int i = 0; i < 50; ++i)
            {
                TestDivideAndConquerDemoHelpInWorkWaitPreferIdleThenLocal();
                TestDivideAndConquerDemoHelpInPoolWaitPreferIdleThenLocal();
                TestDivideAndConquerDemoHelpInWorkWaitPreferLocalWorker();
                TestDivideAndConquerDemoHelpInPoolWaitPreferLocalWorker();
                TestDivideAndConquerDemoHelpInWorkWaitPreferIdleThenLocalDeque();
                TestDivideAndConquerDemoHelpInPoolWaitPreferIdleThenLocalDeque();
                TestDivideAndConquerDemoHelpInWorkWaitPreferLocalWorkerDeque();
                TestDivideAndConquerDemoHelpInPoolWaitPreferLocalWorkerDeque();
            }
        }

        [Fact]
        public void TestDivideAndConquerDemoHelpInWorkWaitPreferIdleThenLocal()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                QueueType = QueueType.LIFO,
                StealOneWorkOnly = true,
            });

            int max = 100000;
            int[] data = Enumerable.Range(0, max).ToArray();

            long result = DivideAndConquerDemoHelpInWorkWait.Run(data, WorkPlacementPolicy.PreferIdleThenLocal, QueueType.LIFO);

            Assert.Equal(4999950000, result);

            powerPool.Dispose();
        }

        [Fact]
        public void TestDivideAndConquerDemoHelpInPoolWaitPreferIdleThenLocal()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int n = 10_000_000;
            long res = DivideAndConquerDemoHelpInPoolWait.Run(n, WorkPlacementPolicy.PreferIdleThenLocal, QueueType.LIFO);
            Assert.Equal(10000000, res);
        }

        [Fact]
        public void TestDivideAndConquerDemoHelpInWorkWaitPreferLocalWorker()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                QueueType = QueueType.LIFO,
                StealOneWorkOnly = true,
            });

            int max = 100000;
            int[] data = Enumerable.Range(0, max).ToArray();

            long result = DivideAndConquerDemoHelpInWorkWait.Run(data, WorkPlacementPolicy.PreferLocalWorker, QueueType.LIFO);

            Assert.Equal(4999950000, result);

            powerPool.Dispose();
        }

        [Fact]
        public void TestDivideAndConquerDemoHelpInPoolWaitPreferLocalWorker()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int n = 10_000_000;
            long res = DivideAndConquerDemoHelpInPoolWait.Run(n, WorkPlacementPolicy.PreferLocalWorker, QueueType.LIFO);
            Assert.Equal(10000000, res);
        }

        [Fact]
        public void TestDivideAndConquerDemoHelpInWorkWaitPreferIdleThenLocalDeque()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                QueueType = QueueType.Deque,
                EnforceDequeOwnership = true,
                StealOneWorkOnly = true,
            });

            int max = 100000;
            int[] data = Enumerable.Range(0, max).ToArray();

            long result = DivideAndConquerDemoHelpInWorkWait.Run(data, WorkPlacementPolicy.PreferIdleThenLocal, QueueType.Deque);

            Assert.Equal(4999950000, result);

            powerPool.Dispose();
        }

        [Fact]
        public void TestDivideAndConquerDemoHelpInPoolWaitPreferIdleThenLocalDeque()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int n = 10_000_000;
            long res = DivideAndConquerDemoHelpInPoolWait.Run(n, WorkPlacementPolicy.PreferIdleThenLocal, QueueType.Deque);
            Assert.Equal(10000000, res);
        }

        [Fact]
        public void TestDivideAndConquerDemoHelpInWorkWaitPreferLocalWorkerDeque()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                QueueType = QueueType.Deque,
                EnforceDequeOwnership = true,
                StealOneWorkOnly = true,
            });

            int max = 100000;
            int[] data = Enumerable.Range(0, max).ToArray();

            long result = DivideAndConquerDemoHelpInWorkWait.Run(data, WorkPlacementPolicy.PreferLocalWorker, QueueType.Deque);

            Assert.Equal(4999950000, result);

            powerPool.Dispose();
        }

        [Fact]
        public void TestDivideAndConquerDemoHelpInPoolWaitPreferLocalWorkerDeque()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int n = 10_000_000;
            long res = DivideAndConquerDemoHelpInPoolWait.Run(n, WorkPlacementPolicy.PreferLocalWorker, QueueType.Deque);
            Assert.Equal(10000000, res);
        }

        public static class DivideAndConquerDemoHelpInWorkWait
        {
            public static WorkID ParallelSum(
                PowerPool powerPool,
                int[] arr, int l, int r,
                int threshold,
                WorkPlacementPolicy workPlacementPolicy,
                string groupName = null)
            {
                int len = r - l + 1;

                if (len <= threshold)
                {
                    return powerPool.QueueWorkItem<long>(() =>
                    {
                        long sum = 0;
                        for (int i = l; i <= r; i++) sum += arr[i];
                        return sum;
                    }, new WorkOption<long>
                    {
                        Group = groupName,
                        ShouldStoreResult = true,
                        WorkPlacementPolicy = workPlacementPolicy
                    });
                }

                int mid = (l + r) >> 1;

                long leftSum = ParallelSumDirect(powerPool, arr, l, mid, threshold, workPlacementPolicy, groupName);

                WorkID rightId = ParallelSum(powerPool, arr, mid + 1, r, threshold, workPlacementPolicy, groupName);

                long rightSum = powerPool.Fetch<long>(rightId, false, true).Result;

                return powerPool.QueueWorkItem<long>(() => leftSum + rightSum, new WorkOption<long>
                {
                    Group = groupName,
                    ShouldStoreResult = true,
                    WorkPlacementPolicy = workPlacementPolicy
                });
            }

            private static long ParallelSumDirect(
                PowerPool powerPool,
                int[] arr, int l, int r,
                int threshold,
                WorkPlacementPolicy workPlacementPolicy,
                string groupName)
            {
                int len = r - l + 1;

                if (len <= threshold)
                {
                    long sum = 0;
                    for (int i = l; i <= r; i++) sum += arr[i];
                    return sum;
                }

                int mid = (l + r) >> 1;

                long leftSum = ParallelSumDirect(powerPool, arr, l, mid, threshold, workPlacementPolicy, groupName);

                WorkID rightId = ParallelSum(powerPool, arr, mid + 1, r, threshold, workPlacementPolicy, groupName);
                long rightSum = powerPool.Fetch<long>(rightId, false, true).Result;

                return leftSum + rightSum;
            }

            public static long Run(int[] arr, WorkPlacementPolicy workPlacementPolicy, QueueType queueType)
            {
                PowerPoolOption options = new PowerPoolOption
                {
                    MaxThreads = Environment.ProcessorCount,
                    QueueType = QueueType.LIFO,
                    StealOneWorkOnly = true,
                    DestroyThreadOption = new DestroyThreadOption
                    {
                        MinThreads = Environment.ProcessorCount,
                        KeepAliveTime = 10_000
                    },
                };

                using PowerPool powerPool = new PowerPool(options);
                string groupName = "ParallelSum";
                Group group = powerPool.GetGroup(groupName);

                WorkID rootId = ParallelSum(powerPool, arr, 0, arr.Length - 1, 10_000, workPlacementPolicy, groupName);

                powerPool.Wait(helpWhileWaiting: true);

                ExecuteResult<long> result = powerPool.Fetch<long>(rootId, false, true);
                return result.Result;
            }
        }

        class DivideAndConquerDemoHelpInPoolWait
        {
            static long ParallelSum(PowerPool powerPool, int[] a, int l, int r, WorkPlacementPolicy workPlacementPolicy, int cutoff = 10_000)
            {
                int n = r - l + 1;
                if (n <= cutoff)
                {
                    long s = 0;
                    for (int i = l; i <= r; i++) s += a[i];
                    return s;
                }

                int m = (l + r) >> 1;

                WorkOption<long> opt = new WorkOption<long>
                {
                    WorkPlacementPolicy = workPlacementPolicy,
                    ShouldStoreResult = true,
                };
                WorkID rightId = powerPool.QueueWorkItem(() => ParallelSum(powerPool, a, m + 1, r, workPlacementPolicy, cutoff), opt);

                long left = ParallelSum(powerPool, a, l, m, workPlacementPolicy, cutoff);

                ExecuteResult<long> rightRes = powerPool.Fetch<long>(rightId, removeAfterFetch: false, helpWhileWaiting: true);
                return left + rightRes.Result;
            }

            public static long Run(int n, WorkPlacementPolicy workPlacementPolicy, QueueType queueType)
            {
                PowerPoolOption option = new PowerPoolOption
                {
                    MaxThreads = Environment.ProcessorCount,
                    QueueType = queueType,
                    StealOneWorkOnly = true,
                    DestroyThreadOption = new DestroyThreadOption
                    {
                        MinThreads = Environment.ProcessorCount,
                        KeepAliveTime = 10_000
                    }
                };

                PowerPool powerPool = new PowerPool(option);

                int[] arr = new int[n];
                Array.Fill(arr, 1);

                WorkID id = powerPool.QueueWorkItem(() => ParallelSum(powerPool, arr, 0, n - 1, workPlacementPolicy));
                long sum = powerPool.Fetch<long>(id, removeAfterFetch: false, helpWhileWaiting: true).Result;

                powerPool.Dispose();

                return sum;
            }
        }

        [Fact]
        public void TestSpinner()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int i = 0;

            Spinner.Start(() =>
            {
                Thread.Sleep(1);
                ++i;

                return i == 5;
            });
        }

        [Fact]
        public void TestWorkerCountOutOfRange1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int done = 0;

            PowerPoolOption ppo = new PowerPoolOption { MaxThreads = 100 };
            PowerPool powerPool = new PowerPool(ppo);
            for (int i = 0; i < 100; ++i)
            {
                if (ppo.MaxThreads >= 2)
                {
                    ppo.MaxThreads = ppo.MaxThreads - 1;
                }

                for (int j = 0; j < 100; ++j)
                {
                    powerPool.QueueWorkItem(() => { Interlocked.Increment(ref done); });
                }
            }

            powerPool.Wait();

            Assert.Equal(10000, done);
        }

        [Fact]
        public void TestWorkerCountOutOfRange2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int done = 0;

            PowerPoolOption ppo = new PowerPoolOption { MaxThreads = 100 };
            PowerPool powerPool = new PowerPool(ppo);
            for (int i = 0; i < 100; ++i)
            {
                if (ppo.MaxThreads >= 2)
                {
                    ppo.MaxThreads = ppo.MaxThreads - 1;
                }

                for (int j = 0; j < 100; ++j)
                {
                    powerPool.QueueWorkItem(() => { Interlocked.Increment(ref done); });
                }
            }

            powerPool.Wait();

            Assert.Equal(10000, done);
        }

        [Fact]
        public void TestWorkerCountOutOfRange3()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int done = 0;

            PowerPoolOption ppo = new PowerPoolOption { MaxThreads = 100 };
            PowerPool powerPool = new PowerPool(ppo);
            for (int i = 0; i < 100; ++i)
            {
                if (ppo.MaxThreads >= 2)
                {
                    ppo.MaxThreads = ppo.MaxThreads - 1;
                }

                for (int j = 0; j < 100; ++j)
                {
                    powerPool.QueueWorkItem(() => { Interlocked.Increment(ref done); });
                }
            }

            powerPool.Wait();

            Assert.Equal(10000, done);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestWorkerCountOutOfRangeDeque()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int done = 0;

            PowerPoolOption ppo = new PowerPoolOption
            {
                MaxThreads = 100,
                QueueType = QueueType.Deque,
                EnforceDequeOwnership = true,
            };
            PowerPool powerPool = new PowerPool(ppo);
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; ++i)
            {
                if (ppo.MaxThreads >= 2)
                {
                    ppo.MaxThreads = ppo.MaxThreads - 1;
                }

                for (int j = 0; j < 100; ++j)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        powerPool.QueueWorkItem(() => { Interlocked.Increment(ref done); });
                    }));
                }
            }
            foreach (var task in tasks)
            {
                await task;
            }

            powerPool.Wait();

            Assert.Equal(10000, done);
        }

        [Fact]
        public void TestSyncDurationNotEnableStatisticsCollection()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            long d1 = -1;
            long d2 = -1;

            double rt1 = -1;
            double rt2 = -1;

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
            }, (res) =>
            {
                d1 = res.Duration;
                rt1 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
            }, (res) =>
            {
                d2 = res.Duration;
                rt2 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });

            powerPool.Wait();

            Assert.InRange(d1, 0, 0);
            Assert.InRange(d2, 0, 0);

            Assert.InRange(rt1, 0, 0);
            Assert.InRange(rt2, 0, 0);
        }

        [Fact]
        public void TestSyncDuration()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { EnableStatisticsCollection = true });

            long d1 = -1;
            long d2 = -1;

            double rt1 = -1;
            double rt2 = -1;

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
            }, (res) =>
            {
                d1 = res.Duration;
                rt1 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
                Thread.Sleep(100);
            }, (res) =>
            {
                d2 = res.Duration;
                rt2 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });

            powerPool.Wait();

            Assert.InRange(d1, 500, 550);
            Assert.InRange(d2, 500, 550);

            Assert.InRange(rt1, 500, 550);
            Assert.InRange(rt2, 500, 550);
        }

        [Fact]
        public void TestAsyncDurationNotEnableStatisticsCollection()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            long d1 = -1;
            long d2 = -1;

            double rt1 = -1;
            double rt2 = -1;

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
            }, (res) =>
            {
                d1 = res.Duration;
                rt1 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
            }, (res) =>
            {
                d2 = res.Duration;
                rt2 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });

            powerPool.Wait();

            Assert.InRange(d1, 0, 0);
            Assert.InRange(d2, 0, 0);

            Assert.InRange(rt1, 0, 0);
            Assert.InRange(rt2, 0, 0);
        }

        [Fact]
        public void TestAsyncDuration()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { EnableStatisticsCollection = true });

            long d1 = -1;
            long d2 = -1;

            double rt1 = -1;
            double rt2 = -1;

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
            }, (res) =>
            {
                d1 = res.Duration;
                rt1 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
            }, (res) =>
            {
                d2 = res.Duration;
                rt2 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });

            powerPool.Wait();

            Assert.InRange(d1, 0, 5);
            Assert.InRange(d2, 0, 5);

            Assert.InRange(rt1, 500, 550);
            Assert.InRange(rt2, 500, 550);
        }

        [Fact]
        public void TestAsyncWithSyncLogicDuration()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { EnableStatisticsCollection = true });

            long d1 = -1;
            long d2 = -1;

            double rt1 = -1;
            double rt2 = -1;

            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(50);
                await Task.Delay(100);
                await Task.Delay(100);
                Thread.Sleep(50);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                Thread.Sleep(50);
            }, (res) =>
            {
                d1 = res.Duration;
                rt1 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(50);
                await Task.Delay(100);
                await Task.Delay(100);
                Thread.Sleep(50);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                Thread.Sleep(50);
            }, (res) =>
            {
                d2 = res.Duration;
                rt2 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });

            powerPool.Wait();

            Assert.InRange(d1, 150, 200);
            Assert.InRange(d2, 150, 200);

            Assert.InRange(rt1, 650, double.MaxValue);
            Assert.InRange(rt2, 650, double.MaxValue);
        }

        [Fact]
        public void TestAsyncWithSyncLogicDurationManyWorker()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                EnableStatisticsCollection = true,
                MaxThreads = 10
            });

            long d1 = -1;
            long d2 = -1;
            long d3 = -1;
            long d4 = -1;

            double rt1 = -1;
            double rt2 = -1;
            double rt3 = -1;
            double rt4 = -1;

            DateTime s1 = default;
            DateTime s2 = default;
            DateTime s3 = default;
            DateTime s4 = default;

            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
            }, (res) =>
            {
                d1 = res.Duration;
                rt1 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
                s1 = res.StartDateTime;
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
            }, (res) =>
            {
                d2 = res.Duration;
                rt2 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
                s2 = res.StartDateTime;
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
            }, (res) =>
            {
                d3 = res.Duration;
                rt3 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
                s3 = res.StartDateTime;
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
            }, (res) =>
            {
                d4 = res.Duration;
                rt4 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
                s4 = res.StartDateTime;
            });

            powerPool.Wait();

            Assert.InRange(d1, 300, 350);
            Assert.InRange(d2, 300, 350);
            Assert.InRange(d3, 300, 350);
            Assert.InRange(d4, 300, 350);

            Assert.InRange(rt1, 300, 450);
            Assert.InRange(rt2, 300, 450);
            Assert.InRange(rt3, 300, 450);
            Assert.InRange(rt4, 300, 450);

            DateTime min = new[] { s1, s2, s3, s4 }.Min();
            DateTime max = new[] { s1, s2, s3, s4 }.Max();
            Assert.InRange((max - min).TotalMilliseconds, 0, 50);
        }

        [Fact]
        public void TestAsyncWithSyncLogicDurationOneWorker()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption
            {
                EnableStatisticsCollection = true,
                MaxThreads = 1
            });

            long d1 = -1;
            long d2 = -1;
            long d3 = -1;
            long d4 = -1;

            double rt1 = -1;
            double rt2 = -1;
            double rt3 = -1;
            double rt4 = -1;

            DateTime s1 = default;
            DateTime s2 = default;
            DateTime s3 = default;
            DateTime s4 = default;

            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
            }, (res) =>
            {
                d1 = res.Duration;
                rt1 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
                s1 = res.StartDateTime;
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
            }, (res) =>
            {
                d2 = res.Duration;
                rt2 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
                s2 = res.StartDateTime;
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
            }, (res) =>
            {
                d3 = res.Duration;
                rt3 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
                s3 = res.StartDateTime;
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                Thread.Sleep(100);
            }, (res) =>
            {
                d4 = res.Duration;
                rt4 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
                s4 = res.StartDateTime;
            });

            powerPool.Wait();

            Assert.InRange(d1, 300, 350);
            Assert.InRange(d2, 300, 350);
            Assert.InRange(d3, 300, 350);
            Assert.InRange(d4, 300, 350);

            Assert.InRange(rt1, 450, double.MaxValue);
            Assert.InRange(rt2, 450, double.MaxValue);
            Assert.InRange(rt3, 450, double.MaxValue);
            Assert.InRange(rt4, 450, double.MaxValue);

            var arr = new[] { s1, s2, s3, s4 }.OrderBy(s => s).ToArray();
            TimeSpan minDiff = new[]
            {
                arr[1] - arr[0],
                arr[2] - arr[1],
                arr[3] - arr[2],
            }.Min();
            Assert.InRange(minDiff.TotalMilliseconds, 100, 150);
        }


        [Fact]
        public void TestAsyncDurationWorkEnded()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { EnableStatisticsCollection = true });

            long d1 = -1;
            long d2 = -1;

            double rt1 = -1;
            double rt2 = -1;

            powerPool.WorkEnded += (s, e) =>
            {
                d2 = e.Duration;
                rt2 = (e.EndDateTime - e.StartDateTime).TotalMilliseconds;
            };

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
            }, (res) =>
            {
                d1 = res.Duration;
                rt1 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });

            powerPool.Wait();

            Assert.InRange(d1, 0, 5);
            Assert.InRange(d2, 0, 5);

            Assert.InRange(rt1, 500, 550);
            Assert.InRange(rt2, 500, 550);
        }

        [Fact]
        public void TestAsyncDurationWorkStopped()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { EnableStatisticsCollection = true });

            long d1 = -1;
            long d2 = -1;

            double rt1 = -1;
            double rt2 = -1;

            powerPool.WorkStopped += (s, e) =>
            {
                d2 = e.Duration;
                rt2 = (e.EndDateTime - e.StartDateTime).TotalMilliseconds;
            };

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
            }, (res) =>
            {
                d1 = res.Duration;
                rt1 = (res.EndDateTime - res.StartDateTime).TotalMilliseconds;
            });

            Thread.Sleep(1);
            powerPool.Stop();

            powerPool.Wait();

            Assert.InRange(d1, 0, 5);
            Assert.InRange(d2, 0, 5);

            Assert.InRange(rt1, 100, 150);
            Assert.InRange(rt2, 100, 150);
        }
    }
}
