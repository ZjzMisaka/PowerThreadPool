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
            powerPool.QueueWorkItemAsync<string>(async () =>
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
        }

        [Fact]
        public void TestNestingAwait()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            Exception e = null;
            object r = null;

            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItemAsync<string>(async () =>
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
            powerPool.Stop();
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Null(l);
            Assert.Null(c);
            Assert.Null(r);
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
