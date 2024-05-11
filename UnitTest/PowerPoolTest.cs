using PowerThreadPool;
using PowerThreadPool.Collections;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.EventArguments;

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
                Assert.True(res.QueueDateTime < res.StartDateTime);
                Assert.True(res.StartDateTime < res.EndDateTime);
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
        public void TestDependents()
        {
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
        public void TestDependentsFailed()
        {
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

            string id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
                throw new Exception();
            });

            string id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            powerPool.QueueWorkItem(() =>
            {
            },
           new WorkOption()
           {
               Dependents = new ConcurrentSet<string>() { id0, id1 }
           });

            powerPool.Wait();

            Assert.Equal(2, doneCount);
            Assert.Equal(1, powerPool.FailedWorkCount);
            Assert.Equal(id0, powerPool.FailedWorkList.First());
            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestDependentsFailedBeforeWorkRun()
        {
            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 },
            };
            powerPool.EnablePoolIdleCheck = false;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref doneCount);
            };

            string id0 = powerPool.QueueWorkItem(() =>
            {
            });

            string id1 = powerPool.QueueWorkItem(() =>
            {
                throw new Exception();
            });

            Thread.Sleep(2000);

            string id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            Thread.Sleep(100);

            powerPool.QueueWorkItem(() =>
            {
            },
           new WorkOption()
           {
               Dependents = new ConcurrentSet<string>() { id0, id1, id2 }
           });

            powerPool.EnablePoolIdleCheck = true;

            powerPool.Wait();

            Assert.Equal(3, doneCount);
            Assert.Equal(1, powerPool.FailedWorkCount);
            Assert.Equal(id1, powerPool.FailedWorkList.First());
            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestDependentsAllSucceedBeforeWorkRun()
        {
            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 },
            };
            powerPool.EnablePoolIdleCheck = false;

            powerPool.WorkEnded += (s, e) =>
            {
                Interlocked.Increment(ref doneCount);
            };

            string id0 = powerPool.QueueWorkItem(() =>
            {
            });

            string id1 = powerPool.QueueWorkItem(() =>
            {
            });

            Thread.Sleep(2000);

            powerPool.QueueWorkItem(() =>
            {
            },
           new WorkOption()
           {
               Dependents = new ConcurrentSet<string>() { id0, id1 }
           });

            powerPool.EnablePoolIdleCheck = true;

            powerPool.Wait();

            Assert.Equal(3, doneCount);
            Assert.Equal(0, powerPool.FailedWorkCount);
            Assert.Equal(0, powerPool.WaitingWorkCount);
        }

        [Fact]
        public void TestWorkPriority()
        {
            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.EnablePoolIdleCheck = false;
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
            powerPool.EnablePoolIdleCheck = true;
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

            powerPool.Stop();
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
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, 
            new WorkOption() 
            {
                CustomWorkID = "1024"
            });

            powerPool.WorkEnded += (s, e) =>
            {
                Assert.Equal("1024", e.ID);
            };
            Assert.Equal("1024", id);
        }

        [Fact]
        public void TestDuplicateCustomWorkID()
        {
            PowerPool powerPool = new PowerPool();
            string id0 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            },
            new WorkOption()
            {
                CustomWorkID = "1024"
            });
            ArgumentException ex = null;
            try
            {
                string id1 = powerPool.QueueWorkItem(() =>
                {
                    Thread.Sleep(1000);
                },
                new WorkOption()
                {
                    CustomWorkID = "1024"
                });
            }
            catch (ArgumentException e)
            {
                ex = e;
            }

            Assert.Equal("CustomWorkID", ex.ParamName);
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
        public async Task TestQueueWhenStopping()
        {
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
            }, new WorkOption()
            {
            });

            powerPool.Stop(false);

            await Task.Delay(100);

            string id = powerPool.QueueWorkItem(() =>
            {
            }, new WorkOption()
            {
            });

            canReturn = true;

            await powerPool.WaitAsync();

            Assert.Null(id);
        }

        [Fact]
        public void TestResetWaitingWorkWhenForceStopEnd()
        {
            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 2, KeepAliveTime = 30000 }
            };

            string id3 = null;
            powerPool.WorkStarted += (s, e) =>
            {
                if (e.ID == id3)
                {
                    powerPool.Stop(id3, true);
                }
            };

            string id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            string id2 = powerPool.QueueWorkItem(() =>
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

            string id4 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            string id5 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            string id6 = powerPool.QueueWorkItem(() =>
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
            string id = powerPool.QueueWorkItem(() =>
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
            string id = powerPool.QueueWorkItem(() =>
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
        public void TestLongWorkForceStop()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 2,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 1, KeepAliveTime = 3000 }
            };

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption()
            {
            });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption()
            {
            });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption()
            {
            });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, new WorkOption()
            {
            });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
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
                Thread.Sleep(1000);
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

            powerPool.Stop(true);

            Thread.Sleep(1000);

            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.LongRunningWorkerCount);
        }

        [Fact]
        public void TestLIFO()
        {
            List<string> logList = new List<string>();

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                QueueType = QueueType.LIFO,
            };

            string id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("1");
            });
            string id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("2");
            });
            string id3 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("3");
            });
            string id4 = powerPool.QueueWorkItem(() =>
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
            List<string> logList = new List<string>();

            PowerPool powerPool = new PowerPool();
            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 1,
                QueueType = QueueType.FIFO,
            };

            string id1 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("1");
            });
            string id2 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("2");
            });
            string id3 = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
            }, (res) =>
            {
                logList.Add("3");
            });
            string id4 = powerPool.QueueWorkItem(() =>
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
            }); ;

            powerPool.Wait();

            Assert.Equal(3, runCount);
        }

        [Fact]
        public void TestImmediateRetryStopRetryByEvent()
        {
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
            }); ;

            powerPool.Wait();

            Assert.Equal(3, runCount);
        }

        [Fact]
        public void TestRequeue()
        {
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
        public void TestErrorWhenCallback()
        {
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
    }
}