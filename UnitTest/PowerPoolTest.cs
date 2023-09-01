using PowerThreadPool;
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
    }
}