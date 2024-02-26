using PowerThreadPool;
using PowerThreadPool.Option;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                item => Assert.True(item >= 1500),
                item => Assert.True(item >= 1500),
                item => Assert.True(item >= 1500)
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
            List<string> pauseRes = powerPool.Pause(new List<string>() { id });
            Assert.Empty(pauseRes);
            Thread.Sleep(500);
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
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                Interlocked.Increment(ref doneCount);
            });
            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                Interlocked.Increment(ref doneCount);
            });

            powerPool.Stop(id, true);
            powerPool.Wait();

            Assert.Equal(3, doneCount);
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
                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                Interlocked.Increment(ref doneCount);
            });
            string id = powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            }, (res) =>
            {
                Interlocked.Increment(ref doneCount);
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
            powerPool.WorkEnd += (s, e) =>
            {
                powerPool.Stop(true);
            };
            powerPool.ForceStop += (s, e) =>
            {
                resId = e.ID;
            };
            id = powerPool.QueueWorkItem(() =>
            {
            }, (res) =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            });
            powerPool.Wait();

            Thread.Sleep(1000);

            Assert.Equal(resId, id);
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
            Thread.Sleep(50);
            powerPool.Stop();
            powerPool.Wait();
            long end = GetNowSs() - start;

            Assert.True(end >= 50 && end <= 350);
        }

        [Fact]
        public async void TestStopAllAsync()
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
                for (int i = 0; i < 1000000; ++i)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
            }, (res) =>
            {
                logList.Add("Work2 END");
            });
            long start = GetNowSs();
            Thread.Sleep(50);
            await powerPool.StopAsync();
            await powerPool.WaitAsync();
            long end = GetNowSs() - start;

            Assert.True(end >= 50 && end <= 5000);
        }

        [Fact]
        public async void TestStopByID()
        {
            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            string id = null;
            string resID = null;
            powerPool.WorkStart += async (s, e) =>
            {
                await powerPool.StopAsync(e.ID);
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
        public async void TestStopByIDList()
        {
            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            string id = null;
            string resID = null;
            powerPool.WorkStart += async (s, e) =>
            {
                await powerPool.StopAsync(new List<string>() { e.ID });
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
        public async void TestStopByIDUseCheckIfRequestedStop()
        {
            PowerPool powerPool = new PowerPool();
            List<long> logList = new List<long>();

            object lockObj = new object();

            string id = null;
            string resID = null;
            powerPool.WorkStart += async (s, e) =>
            {
                await powerPool.StopAsync(e.ID);
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
                logList.Add(res.Result);
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
                logList.Add(res.Result);
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
                logList.Add(res.Result);
            });

            powerPool.Cancel(id);
            powerPool.Wait();

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
                logList.Add(res.Result);
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
                logList.Add(res.Result);
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
                logList.Add(res.Result);
            });

            powerPool.Cancel(new List<string>() { id });
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
                logList.Add(res.Result);
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
                logList.Add(res.Result);
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
                logList.Add(res.Result);
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
            Assert.Equal("", (await powerPool.StopAsync(new List<string>() { "" })).First());
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
            Thread.Sleep(10);
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
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { DefaultWorkTimeout = new TimeoutOption() { Duration = 2000, ForceStop = true } });
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

            Thread.Sleep(100);

            powerPool.Pause(id);
            Thread.Sleep(1000);
            powerPool.Resume(id);
            powerPool.Wait();
            long duration = GetNowSs() - start;

            Assert.True(duration >= 3000);
        }

        [Fact]
        public void TestPauseThreadTimer()
        {
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { Timeout = new TimeoutOption() { Duration = 2000, ForceStop = true } });
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
    }
}
