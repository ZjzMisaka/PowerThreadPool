using PowerThreadPool;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using System.Diagnostics;

namespace UnitTest
{
    public class ControlTest
    {
        private long GetNowSs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        [Fact]
        public void TestPauseAll()
        {
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
            string id = powerPool.QueueWorkItem(() =>
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
            string id = powerPool.QueueWorkItem(() =>
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
            List<string> pauseRes = powerPool.Pause(new List<string>() { id });
            Assert.Empty(pauseRes);
            Thread.Sleep(1000);
            List<string> resumeRes = powerPool.Resume(new List<string>() { id });
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
            List<string> pauseRes = powerPool.Pause(powerPool.GetGroupMemberList("A"));
            Assert.Empty(pauseRes);
            Thread.Sleep(1000);
            List<string> resumeRes = powerPool.Resume(powerPool.GetGroupMemberList("A"));
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
            List<string> pauseRes = powerPool.GetGroup("A").Pause();
            Assert.Empty(pauseRes);
            Thread.Sleep(1000);
            List<string> resumeRes = powerPool.GetGroup("A").Resume();
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
            string id = powerPool.QueueWorkItem(() =>
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
            string id = powerPool.QueueWorkItem(() =>
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
            
            powerPool.Stop(true);
            powerPool.Wait();
            long end = GetNowSs() - start;

            Assert.IsType<ThreadInterruptedException>(res0);
            Assert.IsType<ThreadInterruptedException>(res1);
            Assert.IsType<ThreadInterruptedException>(res2);
        }

        [Fact]
        public void TestForceStopBeforeRunning()
        {
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
            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                if (res.Status == Status.Succeed)
                {
                    Interlocked.Increment(ref doneCount);
                }
            });

            powerPool.Stop(id, true);
            powerPool.Wait();

            Assert.Equal(3, doneCount);
            Assert.Equal(1, cancelCount);
        }

        [Fact]
        public void TestStopBeforeRunning()
        {
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
            string id = powerPool.QueueWorkItem(() =>
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
            string resId = null;
            PowerPool powerPool = new PowerPool();
            string id = null;
            powerPool.WorkEnded += (s, e) =>
            {
                if (e.Succeed)
                {
                    powerPool.Stop(true);
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
            PowerPool powerPool = new PowerPool() { PowerPoolOption = new PowerPoolOption() { MaxThreads = 1 } };
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

            powerPool.Stop(true);
            powerPool.Wait();

            Assert.True(forceStopped);
        }

        [Fact]
        public void TestForceStopWhenInvoke()
        {
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

            powerPool.Stop(true);
            powerPool.Wait();

            Assert.True(forceStopped);
        }

        [Fact]
        public void TestStopAll()
        {
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
            string id = powerPool.QueueWorkItem(() =>
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
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 8 });
            string id = null;
            string resID = null;

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

            powerPool.Stop(true);
            await powerPool.WaitAsync();
            Assert.Equal(0, powerPool.RunningWorkerCount);
        }

        [Fact]
        public async void TestStopByID()
        {
            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            string id = null;
            string resID = null;
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
        }

        [Fact]
        public async void TestStopByIDList()
        {
            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            string id = null;
            string resID = null;
            powerPool.WorkStarted += (s, e) =>
            {
                powerPool.Stop(new List<string>() { e.ID });
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

            await powerPool.WaitAsync(new List<string>() { id });
            await powerPool.WaitAsync();

            Assert.Equal(id, resID);
        }

        [Fact]
        public async void TestStopByGroup()
        {
            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            string id = null;
            string resID = null;
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
                , Group = "A"
            });

            await powerPool.WaitAsync(powerPool.GetGroupMemberList("A"));
            await powerPool.WaitAsync();

            Assert.Equal(id, resID);
        }

        [Fact]
        public async void TestStopByGroupObject()
        {
            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            string id = null;
            string resID = null;
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
        public async void TestStopByIDUseCheckIfRequestedStop()
        {
            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            string id = null;
            string resID = null;
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
        public void TestCancelByID()
        {
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 2 });
            List<long> logList = new List<long>();
            string cid = "";
            string eid = "";
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
            string id = powerPool.QueueWorkItem(() =>
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
        public void TestCancelByIDList()
        {
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
            string id = powerPool.QueueWorkItem(() =>
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

            powerPool.Cancel(new List<string>() { id });
            powerPool.Wait();

            Assert.Equal(2, logList.Count);
        }

        [Fact]
        public void TestCancelByGroup()
        {
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
            string id = powerPool.QueueWorkItem(() =>
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
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            Assert.False(powerPool.Wait(""));
            Assert.False(powerPool.Pause(""));
            Assert.False(powerPool.Resume(""));
            Assert.False(powerPool.Stop(""));
            Assert.False(powerPool.Cancel(""));
            Assert.Equal("", powerPool.Wait(new List<string>() { "" }).First());
            Assert.Equal("", powerPool.Pause(new List<string>() { "" }).First());
            Assert.Equal("", powerPool.Resume(new List<string>() { "" }).First());
            Assert.Equal("", powerPool.Stop(new List<string>() { "" }).First());
            Assert.Equal("", powerPool.Cancel(new List<string>() { "" }).First());
            Assert.Equal("", (await powerPool.WaitAsync(new List<string>() { "" })).First());
        }

        [Fact]
        public void TestStopAfterIdle()
        {
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
            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            powerPool.Wait(id);

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public void TestWaitByIDNotRunningYet()
        {
            long start = GetNowSs();
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { MaxThreads = 1 });

            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            powerPool.Wait(id);

            Assert.True(GetNowSs() - start >= 2000);
        }

        [Fact]
        public void TestWaitByIDList()
        {
            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            powerPool.Wait(new List<string>() { id });

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public void TestWaitByGroup()
        {
            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
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
            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
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
            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
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
            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            });
            Task<bool> task = powerPool.WaitAsync(id);
            Thread.Sleep(100);
            powerPool.Stop(true);

            bool res = await task;
            Assert.True(res);
        }

        [Fact]
        public async void TestWaitByIDAsync()
        {
            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            await powerPool.WaitAsync(id);

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public async void TestWaitByIDListAsync()
        {
            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });

            await powerPool.WaitAsync(new List<string>() { id });

            Assert.True(GetNowSs() - start >= 1000);
        }

        [Fact]
        public void TestPauseWorkTimer()
        {
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { DefaultWorkTimeoutOption = new TimeoutOption() { Duration = 2000, ForceStop = true } });
            List<long> logList = new List<long>();
            object lockObj = new object();
            long start = GetNowSs();
            string id = powerPool.QueueWorkItem(() =>
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

            Assert.True(duration >= 2990);
        }

        [Fact]
        public void TestPauseThreadTimer()
        {
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
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItem(() =>
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
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { StartSuspended = true });
            string id = powerPool.QueueWorkItem(() =>
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
                Dependents = new PowerThreadPool.Collections.ConcurrentSet<string>() { id }
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
    }
}
