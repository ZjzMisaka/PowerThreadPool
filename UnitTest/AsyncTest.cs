using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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

            Assert.IsType<DivideByZeroException>(e);
            Assert.IsType<DivideByZeroException>(eventObj);
            Assert.Equal(id, eventObj1);
        }

        [Fact]
        public void TestNoAwait()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int a = 0;
            int b = 0;
            int c = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.WorkEnded += (s, e) =>
            {
                c = 3;
            };
            WorkID id = powerPool.QueueWorkItemAsync(async () =>
            {
                if (a == 100)
                {
                    await Task.Delay(100);
                }
                a = 1;
            }, (res) =>
            {
                b = 2;
            });

            powerPool.Wait();

            Assert.Equal(1, a);
            Assert.Equal(2, b);
            Assert.Equal(3, c);
        }

        [Fact]
        public void TestNoAwaitHasRes()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int a = 0;
            int b = 0;
            int c = 0;
            int r = 0;

            PowerPool powerPool = new PowerPool();
            powerPool.WorkEnded += (s, e) =>
            {
                c = 3;
            };
            WorkID id = powerPool.QueueWorkItemAsync(async () =>
            {
                if (a == 100)
                {
                    await Task.Delay(100);
                }
                a = 1;
                return 5;
            }, (res) =>
            {
                b = 2;
                r = res.Result;
            });

            powerPool.Wait();

            Assert.Equal(1, a);
            Assert.Equal(2, b);
            Assert.Equal(3, c);
            Assert.Equal(5, r);
        }

        [Fact]
        public void TestNoAwaitError()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            Exception e = null;
            object r = null;

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItemAsync<string>(async () =>
            {
                throw new Exception("1");
#pragma warning disable CS0162
                return await OuterAsyncE();
#pragma warning restore CS0162
            }, (res) =>
            {
                e = res.Exception;
                r = res.Result;
            });

            powerPool.Wait();

            Assert.Equal("1", e.Message);
        }

        [Fact]
        public void TestNoAwaitStop()
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
#pragma warning disable CS1998
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                Task.Delay(1000).Wait();
                l = "2";
                powerPool.StopIfRequested();
                Task.Delay(100).Wait();
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
#pragma warning restore CS1998
            powerPool.Stop();
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
            Assert.Null(c);
            Assert.Null(r);
            Assert.Equal(id, eventObj);
        }

        [Fact]
        public void TestNoAwaitStopByID()
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
#pragma warning disable CS1998
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                Task.Delay(1000).Wait();
                l = "2";
                powerPool.StopIfRequested();
                Task.Delay(100).Wait();
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
#pragma warning restore CS1998
            powerPool.Stop(id);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
            Assert.Null(c);
            Assert.Null(r);
            Assert.Equal(id, eventObj);
        }

        [Fact]
        public void TestNoAwaitForceStop()
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
#pragma warning disable CS1998
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                Task.Delay(1000).Wait();
                Thread.Sleep(200);
                l = "2";
                powerPool.StopIfRequested();
                Task.Delay(100).Wait();
                Thread.Sleep(200);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
#pragma warning restore CS1998
            bool res = powerPool.ForceStop();
            powerPool.Wait();
            if (res)
            {
                Assert.Equal("1", p);
                Assert.Null(l);
                Assert.Null(c);
                Assert.Null(r);
                Assert.Equal(id, eventObj);
                Assert.True(eventObj1);
            }
        }

        [Fact]
        public void TestNoAwaitForceStopByID()
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
#pragma warning disable CS1998
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                Task.Delay(1000).Wait();
                Thread.Sleep(200);
                l = "2";
                powerPool.StopIfRequested();
                Task.Delay(100).Wait();
                Thread.Sleep(200);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
#pragma warning restore CS1998
            bool res = powerPool.ForceStop(id);
            powerPool.Wait();
            if (res)
            {
                Assert.Equal("1", p);
                Assert.Null(l);
                Assert.Null(c);
                Assert.Null(r);
                Assert.Equal(id, eventObj);
                Assert.True(eventObj1);
            }
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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

            Assert.Equal("1", e.Message);
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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
            WorkID stoppedID = default;
            WorkID nsID = default;
            powerPool.WorkStopped += (s, e) =>
            {
                if (nsID == e.ID)
                {
                    return;
                }
                stoppedID = e.ID;
            };
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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
            WorkID id = powerPool.QueueWorkItemAsync(async () =>
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(1000);
                Thread.Sleep(200);
                l = "2";
                powerPool.StopIfRequested();
                await Task.Delay(100);
                Thread.Sleep(200);
                c = "3";
                return "100";
            }, (res) =>
            {
                Assert.Equal("2", l);
                r = res.Result;
            });
            bool res = powerPool.ForceStop(id);
            powerPool.Wait();
            if (res)
            {
                Assert.Equal("1", p);
                Assert.Null(l);
                Assert.Null(c);
                Assert.Null(r);
                Assert.Equal(id, eventObj);
                Assert.True(eventObj1);
            }
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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

            int doneCount = 0;

            long s1 = GetNowSs();
            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(500);
                await Task.Delay(500);
                await Task.Delay(500);
                await Task.Delay(500);
                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(500);
                await Task.Delay(500);
                await Task.Delay(500);
                await Task.Delay(500);
                Interlocked.Increment(ref doneCount);
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
                Interlocked.Increment(ref doneCount);
            });
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(500);
                Thread.Sleep(500);
                Thread.Sleep(500);
                Thread.Sleep(500);
                Interlocked.Increment(ref doneCount);
            });
            powerPool.Wait();
            long e2 = GetNowSs();

            Assert.Equal(4, doneCount);

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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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
            WorkID id = powerPool.QueueWorkItemAsync(async () =>
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

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestWaitByIDAsync()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItemAsync(async () =>
            {
                p = "1";
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                c = "2";
            });

            await powerPool.WaitAsync(id);

            Assert.Equal("1", p);
            Assert.Equal("2", c);
            Assert.True(GetNowSs() - start >= 30);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestWaitByIDAsyncDouble()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            long start = GetNowSs();
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItemAsync(async () =>
            {
                p = "1";
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                c = "2";
            });

            Task t1 = powerPool.WaitAsync(id);
            Task t2 = powerPool.WaitAsync(id);
            await Task.WhenAll(t1, t2);

            Assert.Equal("1", p);
            Assert.Equal("2", c);
            Assert.True(GetNowSs() - start >= 30);
        }

        [Fact]
        public void TestWaitByIDWhenRunning()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            PowerPool powerPool = new PowerPool { PowerPoolOption = new PowerPoolOption { MaxThreads = 1 } };
            WorkID id = powerPool.QueueWorkItemAsync(async () =>
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

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestWaitStopped()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            await Task.Run(async () =>
            {
                Random random = new Random();
                int doneCount = 0;

                PowerPool powerPool = new PowerPool();
                powerPool.PowerPoolOption = new PowerPoolOption()
                {
                    MaxThreads = 8,
                    DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 0 },
                    StartSuspended = true,
                    DefaultCallback = (res) =>
                    {
                        Interlocked.Increment(ref doneCount);
                    },
                    QueueType = QueueType.LIFO
                };

                int runCount = 10;
                doneCount = 0;
                for (int i = 0; i < runCount; ++i)
                {
                    WorkID id = powerPool.QueueWorkItemAsync(async () =>
                    {
                        await Task.Delay(200);
                        await Task.Delay(200);
                        await Task.Delay(200);
                        await Task.Delay(200);
                        await Task.Delay(200);
                        await Task.Delay(200);
                    });
                    if (id == null)
                    {
                        Assert.Fail("PoolStopping");
                    }
                }

                Thread.Yield();

                if (runCount != powerPool.WaitingWorkCount)
                {
                    Assert.Fail();
                }

                powerPool.Start();

                powerPool.Stop();
                await powerPool.WaitAsync();
                if (powerPool.RunningWorkerCount != 0 || powerPool.WaitingWorkCount != 0 || powerPool.AsyncWorkCount != 0)
                {
                    Assert.Fail();
                }
            });
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async Task TestWaitForceStopped()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            await Task.Run(async () =>
            {
                Random random = new Random();
                int doneCount = 0;

                PowerPool powerPool = new PowerPool();
                powerPool.PowerPoolOption = new PowerPoolOption()
                {
                    MaxThreads = 8,
                    DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 0 },
                    StartSuspended = true,
                    DefaultCallback = (res) =>
                    {
                        Interlocked.Increment(ref doneCount);
                    },
                    QueueType = QueueType.LIFO
                };

                int runCount = 10;
                doneCount = 0;
                for (int i = 0; i < runCount; ++i)
                {
                    WorkID id = powerPool.QueueWorkItemAsync(async () =>
                    {
                        Thread.Sleep(200);
                        await Task.Delay(200);
                        Thread.Sleep(200);
                        await Task.Delay(200);
                        Thread.Sleep(200);
                        await Task.Delay(200);
                        Thread.Sleep(200);
                        await Task.Delay(200);
                        Thread.Sleep(200);
                        await Task.Delay(200);
                        Thread.Sleep(200);
                        await Task.Delay(200);
                    });
                    if (id == null)
                    {
                        Assert.Fail("PoolStopping");
                    }
                }

                Thread.Yield();

                if (runCount != powerPool.WaitingWorkCount)
                {
                    Assert.Fail();
                }

                powerPool.Start();

                powerPool.ForceStop();
                await powerPool.WaitAsync();
                if (powerPool.RunningWorkerCount != 0 || powerPool.WaitingWorkCount != 0 || powerPool.AsyncWorkCount != 0)
                {
                    Assert.Fail();
                }
            });
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestFetchAsyncByID()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            PowerPool powerPool = new PowerPool { PowerPoolOption = new PowerPoolOption { MaxThreads = 1 } };
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                c = "2";
                return "100";
            }, new WorkOption<string> { ShouldStoreResult = true });
            var res = await powerPool.FetchAsync<string>(id, true);
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                c = "2";
                return "100";
            }, new WorkOption<string> { ShouldStoreResult = true });
            var res = powerPool.Fetch<string>(new List<WorkID> { id }, true);
            powerPool.Wait();

            Assert.Equal("1", p);
            Assert.Equal("2", c);
            Assert.Equal("100", res[0].Result);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestFetchAsyncByIDList()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            PowerPool powerPool = new PowerPool { PowerPoolOption = new PowerPoolOption { MaxThreads = 1 } };
            powerPool.QueueWorkItem(() =>
            {
                Thread.Sleep(1000);
            });
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                p = "1";
                await Task.Delay(10);
                await Task.Delay(10);
                await Task.Delay(10);
                c = "2";
                return "100";
            }, new WorkOption<string> { ShouldStoreResult = true });
            var res = await powerPool.FetchAsync<string>(new List<WorkID> { id }, true);
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
            powerPool.Wait();
            Assert.Null(p);
            Assert.Null(l);
            Assert.Null(c);
            Assert.Null(r);
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
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
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
            bool res = powerPool.Cancel(id);
            Assert.True(res);
            Assert.Null(p);
            Assert.Null(l);
            Assert.Null(c);
            Assert.Null(r);
            powerPool.Start();
            powerPool.Wait();
            Assert.Null(p);
            Assert.Null(l);
            Assert.Null(c);
            Assert.Null(r);
        }

        [Fact]
        public void TestDependents()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();

            object r1 = null;
            object r2 = null;

            List<string> log = new List<string>();

            WorkID id = powerPool.QueueWorkItemAsync(async () =>
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
                Dependents = new PowerThreadPool.Collections.ConcurrentSet<WorkID> { id },
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

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestTaskAwaitDoneeWithResult()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                await Task.Delay(500);
                return "100";
            },
            out Task<ExecuteResult<string>> task);
            ExecuteResult<string> res = await task;

            Assert.Equal("100", res.Result);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestTaskAwaitDoneWithoutResult()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            int res = 0;

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(500);
                res = 100;
            },
            out Task task);
            await task;

            Assert.Equal(100, res);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestTaskAwaitStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                await Task.Delay(500);
                powerPool.StopIfRequested();
                return "100";
            },
            out Task<ExecuteResult<string>> task);
            powerPool.Stop();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => { await task; });
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestTaskAwaitForceStop()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                Thread.Sleep(100);
                await Task.Delay(100);
                Thread.Sleep(100);
                await Task.Delay(100);
                Thread.Sleep(100);
                await Task.Delay(100);
                Thread.Sleep(100);
                await Task.Delay(100);
                return "100";
            },
            out Task<ExecuteResult<string>> task);
            powerPool.ForceStop();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => { await task; });
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestTaskAwaitCancel()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });
            WorkID fid = powerPool.QueueWorkItem(() =>
            {
                while (true)
                {
                    Thread.Sleep(100);
                    powerPool.StopIfRequested();
                }
            });
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                await Task.Delay(500);
                powerPool.StopIfRequested();
                return "100";
            },
            out Task<ExecuteResult<string>> task);
            powerPool.Cancel(id);
            powerPool.Stop(fid);

            await Assert.ThrowsAsync<TaskCanceledException>(async () => { await task; });
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestTaskAwaitError()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool.QueueWorkItemAsync<string>(async () =>
            {
                await Task.Delay(500);
                throw new AbandonedMutexException("AAAA");
            },
            out Task<ExecuteResult<string>> task);

            await Assert.ThrowsAsync<AbandonedMutexException>(async () => { await task; });
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

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(1);
                await Task.Delay(1);
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

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(1);
                await Task.Delay(1);
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

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(1);
                await Task.Delay(1);
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

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(1);
                await Task.Delay(1);
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

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(1);
                await Task.Delay(1);
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

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(1);
                await Task.Delay(1);
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

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(1);
                await Task.Delay(1);
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

            powerPool.QueueWorkItemAsync(async () =>
            {
                await Task.Delay(1);
                await Task.Delay(1);
                throw new Exception();
            }, new WorkOption<object>()
            {
                RetryOption = new RetryOption() { RetryBehavior = RetryBehavior.Requeue, RetryPolicy = RetryPolicy.Unlimited }
            });

            powerPool.Wait();

            Assert.Equal(100, retryCount);
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestDiferentAsyncWorkItemUseSameWorkOption()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object c = null;

            int doneCount = 0;

            PowerPool powerPool = new PowerPool();
            WorkOption workOption = new WorkOption();
            for (int i = 0; i < 50; ++i)
            {
                powerPool.QueueWorkItemAsync(async () =>
                {
                    p = "1";
                    await Task.Delay(10);
                    await Task.Delay(10);
                    await Task.Delay(10);
                    c = "2";
                    Interlocked.Increment(ref doneCount);
                }, workOption);
            }

            await powerPool.WaitAsync();

            Assert.Equal(50, doneCount);
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
