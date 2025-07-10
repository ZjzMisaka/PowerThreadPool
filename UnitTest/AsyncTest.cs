using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using Xunit.Abstractions;

namespace UnitTest
{
    public class AsyncTest
    {
        private readonly ITestOutputHelper _output;

        public AsyncTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestErrorWhenRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            Exception e = null;

            int z = 0;

            PowerPool powerPool = new PowerPool();
            object eventObj = null;
            object eventObj1 = null;
            powerPool.ErrorOccurred += (s, e) =>
            {
                eventObj = e.Exception;
                eventObj1 = e.ID;
            };
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                await Task.Delay(100);
                int a = 0 / z;
                await Task.Delay(100);
                return "100";
            }, (res) =>
            {
                e = res.Exception;
            });

            powerPool.Wait();

            Assert.IsType<DivideByZeroException>(e.InnerException);
            Assert.IsType<DivideByZeroException>((eventObj as Exception).InnerException);
            Assert.Equal(id, eventObj1);
        }

        [Fact]
        public void TestNestingAwait()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            Exception e = null;
            object r = null;

            PowerPool powerPool = new PowerPool();
            object eventObj = null;
            object eventObj1 = null;
            powerPool.WorkEnded += (s, e) =>
            {
                eventObj = e.Result;
                eventObj1 = e.ID;
            };
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                return await OuterAsync();
            }, (res) =>
            {
                e = res.Exception;
                r = res.Result;
            });

            powerPool.Wait();

            Assert.Null(e);
            Assert.Equal("123", r);
            Assert.Equal("123", eventObj);
            Assert.Equal(id, eventObj1);
        }

        [Fact]
        public void TestNestingAwaitError()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            Exception e = null;
            object r = null;

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItemAsync<string>(async () =>
            {
                return await OuterAsyncE();
            }, (res) =>
            {
                e = res.Exception;
                r = res.Result;
            });

            powerPool.Wait();

            Assert.Equal("1", e.InnerException.Message);
        }

        [Fact]
        public void TestStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            object eventObj = null;
            powerPool.WorkStopped += (s, e) =>
            {
                eventObj = e.ID;
            };
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(1000);
                l = "2";
                powerPool.StopIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
            powerPool.Stop();
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Null(l);
            Assert.Null(c);
            Assert.Null(r);
            Assert.Equal(id, eventObj);
        }

        [Fact]
        public void TestStopSleep()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            object r = null;
            Status s = Status.Succeed;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                Thread.Sleep(1000);
                l = "2";
                powerPool.StopIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                r = res.Result;
                s = res.Status;
            });
            powerPool.Stop();
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
            Assert.Null(c);
            Assert.Null(r);
            Assert.Equal(Status.Stopped, s);
        }

        [Fact]
        public void TestStopByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(1000);
                l = "2";
                powerPool.StopIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
            powerPool.Stop(id);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Null(l);
            Assert.Null(c);
            Assert.Null(r);
        }

        [Fact]
        public void TestStopAndWaitByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(1000);
                l = "2";
                powerPool.StopIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
            powerPool.Stop(id);
            powerPool.Wait(id);
            Assert.Equal("1", p);
            Assert.Null(l);
            Assert.Null(c);
            Assert.Null(r);
        }

        [Fact]
        public void TestStopByIDRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });
            string stoppedID = null;
            string nsID = null;
            powerPool.WorkStopped += (s, e) =>
            {
                if (nsID == e.ID)
                {
                    return;
                }
                stoppedID = e.ID;
            };
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1000);
                powerPool.StopIfRequested();
                await Task.Delay(100);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                await Task.Delay(1);
                return "100";
            });
            Thread.Sleep(2);
            nsID = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    powerPool.StopIfRequested();
                    Thread.Sleep(10);
                }
            });
            Thread.Sleep(10);
            powerPool.Stop(id);
            Thread.Sleep(10);
            powerPool.Stop(nsID);
            powerPool.Wait();
            Assert.Equal(id, stoppedID);
        }

        [Fact]
        public void TestStopByIDNoResult()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItemAsync(async () =>
            {
                p = "1";
                await Task.Delay(1000);
                l = "2";
                powerPool.StopIfRequested();
                await Task.Delay(100);
                c = "3";
            }, (res) =>
            {
                r = res.Result;
            });
            powerPool.Stop(id);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Null(l);
            Assert.Null(c);
            Assert.Null(r);
        }

        [Fact]
        public void TestForceStopByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            object eventObj = null;
            bool eventObj1 = false;
            powerPool.WorkStopped += (s, e) =>
            {
                eventObj = e.ID;
                eventObj1 = e.ForceStop;
            };
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(1000);
                l = "2";
                powerPool.StopIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
            powerPool.Stop(id, true);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Null(l);
            Assert.Null(c);
            Assert.Null(r);
            Assert.Equal(id, eventObj);
            Assert.True(eventObj1);
        }

        [Fact]
        public void TestStopByIDHaveSubID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;
            object r = null;
            Status s = Status.Succeed;
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(50);
                await Task.Delay(50);
                await Task.Delay(50);
                await Task.Delay(50);
                await Task.Delay(200);
                await Task.Delay(200);
                powerPool.StopIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                r = res.Result;
                s = res.Status;
            });
            Thread.Sleep(400);
            powerPool.Stop(id);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Null(c);
            Assert.Null(r);
            Assert.Equal(Status.Stopped, s);
        }

        [Fact]
        public void TestPerformance()
        {
            PowerPool powerPool = new PowerPool { PowerPoolOption = new PowerPoolOption { MaxThreads = 1 } };

            long s1 = GetNowSs();
            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(500);
                await Task.Delay(500);
                await Task.Delay(500);
                await Task.Delay(500);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(500);
                await Task.Delay(500);
                await Task.Delay(500);
                await Task.Delay(500);
            });
            powerPool.Wait();
            long e1 = GetNowSs();

            long s2 = GetNowSs();
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Thread.Sleep(500);
                Thread.Sleep(500);
                Thread.Sleep(500);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Thread.Sleep(500);
                Thread.Sleep(500);
                Thread.Sleep(500);
            });
            powerPool.Wait();
            long e2 = GetNowSs();

            long d1 = e1 - s1;
            long d2 = e2 - s2;

            Assert.InRange(d1, 1500, 2500);
            Assert.InRange(d2, 3500, 4500);
        }

        [Fact]
        public void TestPauseAndResumeByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long s = GetNowSs();

            object p = null;
            object l = null;
            object c = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(50);
                l = "2";
                powerPool.PauseIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
            powerPool.Pause(id);
            Thread.Sleep(1000);
            powerPool.Resume(id);
            powerPool.Wait();

            long e = GetNowSs();

            Assert.InRange(e - s, 1000, 1500);
        }

        [Fact]
        public void TestPauseAndResumeByIDHaveSubIDNotAttach()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long st = GetNowSs();

            object p = null;
            object c = null;
            object r = null;
            Status s = Status.Succeed;
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(50);
                await Task.Delay(50);
                await Task.Delay(50);
                await Task.Delay(50);
                powerPool.PauseIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                r = res.Result;
                s = res.Status;
            });
            Thread.Sleep(200);
            powerPool.Pause(id);
            Thread.Sleep(1000);
            powerPool.Resume(id);
            powerPool.Wait();

            long e = GetNowSs();

            Assert.InRange(e - st, 1000, 2000);
        }

        [Fact]
        public void TestPauseAndResumeByIDHaveSubIDAttach()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            long st = GetNowSs();

            object p = null;
            object c = null;
            object r = null;
            Status s = Status.Succeed;
            PowerPool powerPool = new PowerPool();
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                await Task.Delay(100);
                powerPool.PauseIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                r = res.Result;
                s = res.Status;
            });
            Thread.Sleep(150);
            powerPool.Pause(id);
            Thread.Sleep(1000);
            powerPool.Resume(id);
            powerPool.Wait();

            long e = GetNowSs();

            Assert.InRange(e - st, 1000, 2000);
        }

        [Fact]
        public void TestCancelByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            PowerPool powerPool = new PowerPool { PowerPoolOption = new PowerPoolOption { MaxThreads = 1 } };
            object eventObj = null;
            powerPool.WorkCanceled += (s, e) =>
            {
                eventObj = e.ID;
            };
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                c = "2";
                return "100";
            });
            powerPool.Cancel(id);
            powerPool.Wait();

            Assert.Null(p);
            Assert.Null(c);
            Assert.Equal(id, eventObj);
        }

        [Fact]
        public void TestWaitByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            PowerPool powerPool = new PowerPool { PowerPoolOption = new PowerPoolOption { MaxThreads = 1 } };
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            string id = powerPool.QueueWorkItemAsync(async () =>
            {
                p = "1";
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                c = "2";
            });
            powerPool.Wait(id);

            Assert.Equal("1", p);
            Assert.Equal("2", c);
        }

        [Fact]
        public void TestWaitByIDWhenRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            PowerPool powerPool = new PowerPool { PowerPoolOption = new PowerPoolOption { MaxThreads = 1 } };
            string id = powerPool.QueueWorkItemAsync(async () =>
            {
                p = "1";
                await Task.Delay(200);
                await Task.Delay(200);
                await Task.Delay(200);
                c = "2";
            });
            Thread.Sleep(100);

            powerPool.Wait(id);

            Assert.Equal("1", p);
            Assert.Equal("2", c);
        }

        [Fact]
        public void TestFetchByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            PowerPool powerPool = new PowerPool { PowerPoolOption = new PowerPoolOption { MaxThreads = 1 } };
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                c = "2";
                return "100";
            }, new WorkOption<string> { ShouldStoreResult = true });
            var res = powerPool.Fetch<string>(id, true);
            powerPool.Wait();

            Assert.Equal("1", p);
            Assert.Equal("2", c);
            Assert.Equal("100", res.Result);
        }

        [Fact]
        public void TestFetchByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            PowerPool powerPool = new PowerPool { PowerPoolOption = new PowerPoolOption { MaxThreads = 1 } };
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                c = "2";
                return "100";
            }, new WorkOption<string> { ShouldStoreResult = true });
            var res = powerPool.Fetch<string>(new List<string> { id }, true);
            powerPool.Wait();

            Assert.Equal("1", p);
            Assert.Equal("2", c);
            Assert.Equal("100", res[0].Result);
        }

        [Fact]
        public void TestStartSuspend()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            object r = null;
            PowerPool powerPool = new PowerPool(new PowerPoolOption { StartSuspended = true });
            powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(1000);
                l = "2";
                powerPool.StopIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
            powerPool.Start();
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
            Assert.Equal("100", r);
        }

        [Fact]
        public void TestStartSuspendCancel()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            object r = null;
            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1, StartSuspended = true });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            string id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(1000);
                l = "2";
                powerPool.StopIfRequested();
                await Task.Delay(100);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
            powerPool.Cancel(id);
        }

        [Fact]
        public void TestDependents()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            object r1 = null;
            object r2 = null;

            List<string> log = new List<string>();

            string id = powerPool.QueueWorkItemAsync(async () =>
            {
                log.Add("0");
                await Task.Delay(100);
                log.Add("1");
                await Task.Delay(100);
                log.Add("2");
                await Task.Delay(100);
                log.Add("3");
                await Task.Delay(100);
                log.Add("4");
                await Task.Delay(100);
                log.Add("5");
                r1 = "100";
            });
            powerPool.QueueWorkItemAsync<string>(async () =>
            {
                log.Add("6");
                await Task.Delay(100);
                log.Add("7");
                await Task.Delay(100);
                log.Add("8");
                await Task.Delay(100);
                log.Add("9");
                await Task.Delay(100);
                log.Add("10");
                await Task.Delay(100);
                log.Add("11");
                return "200";
            }, new WorkOption<string>
            {
                Dependents = new PowerThreadPool.Collections.ConcurrentSet<string> { id },
                Callback = (res) =>
                {
                    r2 = res.Result;
                }
            });
            powerPool.Wait();
            Assert.Equal("100", r1);
            Assert.Equal("200", r2);

            Assert.Collection(log,
                item => Assert.Equal("0", item),
                item => Assert.Equal("1", item),
                item => Assert.Equal("2", item),
                item => Assert.Equal("3", item),
                item => Assert.Equal("4", item),
                item => Assert.Equal("5", item),
                item => Assert.Equal("6", item),
                item => Assert.Equal("7", item),
                item => Assert.Equal("8", item),
                item => Assert.Equal("9", item),
                item => Assert.Equal("10", item),
                item => Assert.Equal("11", item));
        }

        [Fact]
        public void TestEnablePoolIdleCheck()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int count = 0;

            PowerPool powerPool = new PowerPool();

            powerPool.EnablePoolIdleCheck = false;
            for (int i = 0; i < 100; ++i)
            {
                powerPool.QueueWorkItemAsync(async () =>
                {
                    await Task.Delay(10);
                    await Task.Delay(10);
                    await Task.Delay(10);
                    Interlocked.Increment(ref count);
                });
            }
            powerPool.EnablePoolIdleCheck = true;

            powerPool.Wait();

            Assert.Equal(100, count);
        }

        [Fact]
        public void TestGroupMember()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int count = 0;

            PowerPool powerPool = new PowerPool();

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                Interlocked.Increment(ref count);
            }, new WorkOption { Group = "AAA" });
            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                Interlocked.Increment(ref count);
            }, new WorkOption { Group = "AAA" });
            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                Interlocked.Increment(ref count);
            }, new WorkOption { Group = "AAA" });

            powerPool.Wait();

            Assert.Empty(powerPool.GetGroupMemberList("AAA"));
        }

        private async Task<string> OuterAsync()
        {
            string result = await InnerAsync();
            await Task.Delay(100);
            return result;
        }

        private async Task<string> InnerAsync()
        {
            await Task.Delay(100);
            return "123";
        }

        private async Task<string> OuterAsyncE()
        {
            string result = await InnerAsyncE();
            await Task.Delay(100);
            return result;
        }

        private async Task<string> InnerAsyncE()
        {
            await Task.Delay(100);
            throw new Exception("1");
        }

        private long GetNowSs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
