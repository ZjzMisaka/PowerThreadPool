using PowerThreadPool;
using PowerThreadPool.Collections;
using PowerThreadPool.Option;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Xunit.Sdk;

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
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    logList.Add("DefaultCallback");
                    result = (string)res.Result;
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = false },
                DefaultWorkTimeout = new TimeoutOption() { Duration = 3000, ForceStop = false },
            };
            powerPool.PoolStart += (s, e) =>
            {
                logList.Add("PoolStart");
            };
            powerPool.PoolIdle += (s, e) =>
            {
                logList.Add("PoolIdle");
            };
            powerPool.WorkStart += (s, e) =>
            {
                logList.Add("WorkStart");
            };
            powerPool.WorkEnd += (s, e) =>
            {
                logList.Add("WorkEnd");
            };
            powerPool.WorkTimeout += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimeout += (s, e) =>
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
            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    Assert.Fail("Should not run DefaultCallback");
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = false },
                DefaultWorkTimeout = new TimeoutOption() { Duration = 3000, ForceStop = false },
            };

            string id = "";
            string resId = "";
            id = powerPool.QueueWorkItem(() =>
            {
                return 1024;
            }, (res) =>
            {
                resId = res.ID;
                Assert.Equal(Status.Succeed, res.Status);
                Assert.Equal(1024, res.Result);
            });
            powerPool.Wait();
            Assert.NotEqual("", id);
            Assert.Equal(id, resId);
        }

        [Fact]
        public void TestDefaultWorkTimeout()
        {
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
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = true },
                DefaultWorkTimeout = new TimeoutOption() { Duration = 3000, ForceStop = true },
            };
            powerPool.WorkTimeout += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimeout += (s, e) =>
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
                Timeout = new TimeoutOption() { Duration = 1000, ForceStop = true },
                DefaultWorkTimeout = new TimeoutOption() { Duration = 30000, ForceStop = true },
            };
            bool timeOut = false;
            powerPool.WorkTimeout += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimeout += (s, e) =>
            {
                timeOut = true;
                logList.Add("PoolTimeout");
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
                item => Assert.Equal("PoolTimeout", item)
                );
        }

        [Fact]
        public void TestWorkTimeout()
        {
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
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = true },
                DefaultWorkTimeout = new TimeoutOption() { Duration = 300000000, ForceStop = true },
            };
            powerPool.WorkTimeout += (s, e) =>
            {
                logList.Add("WorkTimeout");
            };
            powerPool.PoolTimeout += (s, e) =>
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
                Timeout = new TimeoutOption() { Duration = 100, ForceStop = true }
            });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("WorkTimeout", item)
                );
        }

        [Fact]
        public void TestError()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    Assert.Fail("Should not run DefaultCallback");
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                Timeout = new TimeoutOption() { Duration = 10000, ForceStop = false },
                DefaultWorkTimeout = new TimeoutOption() { Duration = 3000, ForceStop = false },
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
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };
            powerPool.PoolStart += (s, e) =>
            {
                logList.Add("PoolStart");
            };
            powerPool.PoolIdle += (s, e) =>
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
            new WorkOption()
            {
                Dependents = new ConcurrentSet<string>() { id0, id1 }
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
        public void TestWorkPriority()
        {
            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 0, KeepAliveTime = 3000 }
            };
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                logList.Add("Work0 Priority0 END");
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1100);
                logList.Add("Work1 Priority0 END");
            }, new WorkOption()
            {
            });
            Thread.Sleep(200);
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
                logList.Add("Work2 Priority0 END");
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(200);
                logList.Add("Work3 Priority0 END");
            }, new WorkOption()
            {
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(100);
                logList.Add("Work4 Priority1 END");
            }, new WorkOption()
            {
                WorkPriority = 1
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(200);
                logList.Add("Work5 Priority1 END");
            }, new WorkOption()
            {
                WorkPriority = 1
            });

            powerPool.Wait();

            //Assert.Collection<string>(logList,
            //    item => Assert.Equal("Work0 Priority0 END", item),
            //    item => Assert.Equal("Work1 Priority0 END", item),
            //    item => Assert.Equal("Work4 Priority1 END", item),
            //    item => Assert.Equal("Work5 Priority1 END", item),
            //    item => Assert.Equal("Work2 Priority0 END", item),
            //    item => Assert.Equal("Work3 Priority0 END", item)
            //    );
        }

        [Fact]
        public void TestThreadPriority()
        {
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

            Thread.Sleep(100);
            powerPool.Stop();
            powerPool.Wait();
        }

        [Fact]
        public void TestRunningStatus()
        {
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

            Thread.Sleep(10);

            Assert.Equal(1, powerPool.IdleWorkerCount);
        }

        [Fact]
        public void TestCustomWorkIDStatus()
        {
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, 
            new WorkOption() 
            {
                CustomWorkID = "1024"
            });

            powerPool.WorkEnd += (s, e) =>
            {
                Assert.Equal("1024", e.ID);
            };
            Assert.Equal("1024", id);
        }

        [Fact]
        public void TestThreadsNumberError()
        {
            bool errored = false;
            try
            {
                PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 10, DestroyThreadOption = new DestroyThreadOption() { MinThreads = 100 } });
                string id = powerPool.QueueWorkItem(() =>
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
        public void TestWaitFailed()
        {
            PowerPool powerPool = new PowerPool();
            string id1 = powerPool.QueueWorkItem(() =>
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
            string id2 = powerPool.QueueWorkItem(() =>
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
            PowerPool powerPool = new PowerPool();
            string id1 = powerPool.QueueWorkItem(() =>
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
            string id2 = powerPool.QueueWorkItem(() =>
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
            PowerPool powerPool = new PowerPool();
            string id1 = powerPool.QueueWorkItem(() =>
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
            string id2 = powerPool.QueueWorkItem(() =>
            {
            }, new WorkOption()
            {
            });

            Thread.Sleep(100);

            bool res = powerPool.Cancel(id2);

            Assert.False(res);

            powerPool.Stop();
        }

        [Fact]
        public void TestQueueWhenStopping()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 9999999; ++i)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }, new WorkOption()
            {
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 9999999; ++i)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }, new WorkOption()
            {
            });
            Thread.Sleep(100);
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 9999999; ++i)
                {
                    if (powerPool.CheckIfRequestedStop())
                    {
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }, new WorkOption()
            {
            });

            powerPool.Stop(false);

            string id2 = powerPool.QueueWorkItem(() =>
            {
            }, new WorkOption()
            {
            });

            Assert.Null(id2);
        }
    }
}