using PowerThreadPool;
using PowerThreadPool.Collections;
using PowerThreadPool.Option;

namespace UnitTest
{
    public class PowerPoolTest
    {
        [Fact]
        public void TestOrderAndDefaultCallback()
        {
            List<string> logList = new List<string>();
            string result = "";
            PowerPool powerPool = new PowerPool();
            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    logList.Add("DefaultCallback");
                    result = (string)res.Result;
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = false },
                DefaultThreadTimeout = new TimeoutOption() { Duration = 3000, ForceStop = false },
            };
            powerPool.ThreadPoolStart += (s, e) =>
            {
                logList.Add("ThreadPoolStart");
            };
            powerPool.ThreadPoolIdle += (s, e) =>
            {
                logList.Add("ThreadPoolIdle");
            };
            powerPool.ThreadStart += (s, e) =>
            {
                logList.Add("ThreadStart");
            };
            powerPool.ThreadEnd += (s, e) =>
            {
                logList.Add("ThreadEnd");
            };
            powerPool.ThreadTimeout += (s, e) =>
            {
                logList.Add("ThreadTimeout");
            };
            powerPool.ThreadPoolTimeout += (s, e) =>
            {
                logList.Add("ThreadPoolTimeout");
            };

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                return "TestOrder Result";
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("ThreadPoolStart", item),
                item => Assert.Equal("ThreadStart", item),
                item => Assert.Equal("ThreadEnd", item),
                item => Assert.Equal("DefaultCallback", item),
                item => Assert.Equal("ThreadPoolIdle", item)
                );

            Assert.Equal("TestOrder Result", result);
        }

        [Fact]
        public void TestCallback()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    Assert.Fail("Should not run DefaultCallback");
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = false },
                DefaultThreadTimeout = new TimeoutOption() { Duration = 3000, ForceStop = false },
            };

            string id = "";
            id = powerPool.QueueWorkItem(() =>
            {
                return 1024;
            }, (res) =>
            {
                Assert.Equal(Status.Succeed, res.Status);
                Assert.Equal(id, res.ID);
                Assert.Equal(1024, res.Result);
            });
        }

        [Fact]
        public void TestTimeout()
        {
            List<string> logList = new List<string>();
            PowerPool powerPool = new PowerPool();
            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    Assert.IsType<ThreadInterruptedException>(res.Exception);
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = true },
                DefaultThreadTimeout = new TimeoutOption() { Duration = 3000, ForceStop = true },
            };
            powerPool.ThreadTimeout += (s, e) =>
            {
                logList.Add("ThreadTimeout");
            };
            powerPool.ThreadPoolTimeout += (s, e) =>
            {
                logList.Add("ThreadPoolTimeout");
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
                item => Assert.Equal("ThreadTimeout", item)
                );
        }

        [Fact]
        public void TestThreadPoolTimeout()
        {
            List<string> logList = new List<string>();
            PowerPool powerPool = new PowerPool();
            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    // Assert.IsType<ThreadInterruptedException>(res.Exception);
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = true },
                DefaultThreadTimeout = new TimeoutOption() { Duration = 3000, ForceStop = true },
            };
            bool timeOut = false;
            powerPool.ThreadTimeout += (s, e) =>
            {
                logList.Add("ThreadTimeout");
            };
            powerPool.ThreadPoolTimeout += (s, e) =>
            {
                timeOut = true;
                logList.Add("ThreadPoolTimeout");
            };

            string id;
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
                item => Assert.Equal("ThreadPoolTimeout", item)
                );
        }

        [Fact]
        public void TestError()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    Assert.Fail("Should not run DefaultCallback");
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = false },
                DefaultThreadTimeout = new TimeoutOption() { Duration = 3000, ForceStop = false },
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
        public void TestDependents()
        {
            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            powerPool.ThreadPoolStart += (s, e) =>
            {
                logList.Add("ThreadPoolStart");
            };
            powerPool.ThreadPoolIdle += (s, e) =>
            {
                logList.Add("ThreadPoolIdle");
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

            string id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                logList.Add("Work0 END");
            }, (res) =>
            {
                Thread.Sleep(1000);
                logList.Add("Work0 callback END");
            });

            string id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1500);
                logList.Add("Work1 END");
            });

            powerPool.QueueWorkItem(() =>
            {
                logList.Add("Work2 denpend on work0, work1 END");
            },
            new ThreadOption()
            {
                Dependents = new ConcurrentSet<string>() { id0, id1 }
            }
            );

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("ThreadPoolStart", item),
                item => Assert.Equal("Work0 END", item),
                item => Assert.Equal("Work1 END", item),
                item => Assert.Equal("Work3 END", item),
                item => Assert.Equal("Work0 callback END", item),
                item => Assert.Equal("Work2 denpend on work0, work1 END", item),
                item => Assert.Equal("Work3 callback END", item),
                item => Assert.Equal("ThreadPoolIdle", item)
                );
        }

        [Fact]
        public void TestPriority()
        {
            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 0, KeepAliveTime = 3000 }
            };
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(2000);
            }, new ThreadOption()
            {
                Callback = (res) => 
                {
                    logList.Add("Work0 Priority0 END");
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(2000);
            }, new ThreadOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work1 Priority1 END");
                },
                Priority = 1,
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(2000);
            }, new ThreadOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work2 Priority2 END");
                },
                Priority = 2,
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(2000);
            }, new ThreadOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work3 Priority0 END");
                }
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(2000);
            }, new ThreadOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work4 Priority1 END");
                },
                Priority = 1,
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(2000);
            }, new ThreadOption()
            {
                Callback = (res) =>
                {
                    logList.Add("Work5 Priority2 END");
                },
                Priority = 2,
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("Work0 Priority0 END", item),
                item => Assert.Equal("Work2 Priority2 END", item),
                item => Assert.Equal("Work5 Priority2 END", item),
                item => Assert.Equal("Work1 Priority1 END", item),
                item => Assert.Equal("Work4 Priority1 END", item),
                item => Assert.Equal("Work3 Priority0 END", item)
                );
        }
    }
}