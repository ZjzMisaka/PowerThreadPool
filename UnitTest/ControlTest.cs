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
                item => Assert.True(item >= 1500 && item < 1600),
                item => Assert.True(item >= 1500 && item < 1600),
                item => Assert.True(item >= 1500 && item < 1600)
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
            Thread.Sleep(50);
            powerPool.Stop(true);
            long end = GetNowSs() - start;

            Assert.True(end > 50 && end < 350);
            Assert.IsType<ThreadInterruptedException>(res0);
            Assert.IsType<ThreadInterruptedException>(res1);
            Assert.IsType<ThreadInterruptedException>(res2);
        }

        [Fact]
        public void TestForceStopAfterExecuteEnd()
        {
            string resId = null;
            PowerPool powerPool = new PowerPool();
            string id = null;
            powerPool.ThreadEnd += (s, e) =>
            {
                powerPool.Stop(true);
            };
            powerPool.ThreadForceStop += (s, e) =>
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
            Assert.Equal(resId, id);
        }

        [Fact]
        public void TestStopAll()
        {
            PowerPool powerPool = new PowerPool();
            List<string> logList = new List<string>();
            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 100; ++i)
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
                for (int i = 0; i < 100; ++i)
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
                for (int i = 0; i < 100; ++i)
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

            Assert.True(end > 50 && end < 150);
        }

        [Fact]
        public void TestStopByID()
        {
            PowerPool powerPool = new PowerPool();
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
            
            Thread.Sleep(150);
            powerPool.Stop(id);
            powerPool.Wait();

            Assert.Collection<long>(logList,
                item => Assert.Equal(0, item),
                item => Assert.NotEqual(0, item),
                item => Assert.NotEqual(0, item)
                );
        }

        [Fact]
        public void TestCancel()
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
    }
}
