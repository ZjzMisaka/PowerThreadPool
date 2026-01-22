using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Options;
using PowerThreadPool.Results;
using PowerThreadPool.Works;
using Xunit.Abstractions;

namespace UnitTest
{
    public class QueueWorkItemTest
    {
        private readonly ITestOutputHelper _output;

        public QueueWorkItemTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Test1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() => { p = "1"; }, (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() => { p = "1"; }, new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test3()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem((object[] param) => { p = param[0]; }, new[] { "1" }, (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test4()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem((object[] param) => { p = param[0]; }, new[] { "1" }, new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test5()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string>((string param) => { p = param; }, "1", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test6()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string>((string param) => { p = param; }, "1", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test7()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string>((string param1, string param2) => { p = param1; }, "1", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test8()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string>((string param1, string param2) => { p = param1; }, "1", "", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test9()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string>((string param1, string param2, string param3) => { p = param1; }, "1", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test10()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string>((string param1, string param2, string param3) => { p = param1; }, "1", "", "", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test11()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string>((string param1, string param2, string param3, string param4) => { p = param1; }, "1", "", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test12()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string>((string param1, string param2, string param3, string param4) => { p = param1; }, "1", "", "", "", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test13()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string>((string param1, string param2, string param3, string param4, string param5) => { p = param1; }, "1", "", "", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test14()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string>((string param1, string param2, string param3, string param4, string param5) => { p = param1; }, "1", "", "", "", "", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test15()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, int>((string param) => { p = param; return int.Parse(param); }, "1", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test16()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, int>((string param) => { p = param; return int.Parse(param); }, "1", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test17()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, int>((string param1, string param2) => { p = param1; return 0; }, "1", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test18()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, int>((string param1, string param2) => { p = param1; return 0; }, "1", "", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test19()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, int>((string param1, string param2, string param3) => { p = param1; return 0; }, "1", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test20()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, int>((string param1, string param2, string param3) => { p = param1; return 0; }, "1", "", "", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test21()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, int>((string param1, string param2, string param3, string param4) => { p = param1; return 0; }, "1", "", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test22()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, int>((string param1, string param2, string param3, string param4) => { p = param1; return 0; }, "1", "", "", "", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test23()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string, int>((string param1, string param2, string param3, string param4, string param5) => { p = param1; return 0; }, "1", "", "", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test24()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string, int>((string param1, string param2, string param3, string param4, string param5) => { p = param1; return 0; }, "1", "", "", "", "", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test25()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>(() => { p = "1"; return 0; }, (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test26()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>(() => { p = "1"; return 0; }, new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test27()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>((param) => { p = param[0]; return 0; }, new[] { "1" }, (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test28()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>((param) => { p = param[0]; return 0; }, new[] { "1" }, new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test29()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test30()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test31()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test32()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, out _, (res) =>
            {
                c = "3";
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
        }

        [Fact]
        public void Test33()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, out _, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test34()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test35()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test36()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.WorkEnded += (s, e) =>
            {
                r = e.Result;
            };
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test37()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            string r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, out _, (res) =>
            {
                c = "3";
                r = res.Result;
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test38()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            var id = powerPool.QueueWorkItem(async () =>
            {
                p = "1";
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, out _, new WorkOption() { ShouldStoreResult = true });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal("1", p);
            Assert.Equal("2", l);
            Assert.Equal("done", powerPool.Fetch<string>(id).Result);
        }

        [Fact]
        public void Test39()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test40()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test41()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test42()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, out _, (res) =>
            {
                c = "3";
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
        }

        [Fact]
        public void Test43()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, out _, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test44()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test45()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test46()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.WorkEnded += (s, e) =>
            {
                r = e.Result;
            };
            powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test47()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            string r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, out _, (res) =>
            {
                c = "3";
                r = res.Result;
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test48()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            var id = powerPool.QueueWorkItem(async (p1) =>
            {
                p = p1;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, out _, new WorkOption() { ShouldStoreResult = true });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(1, p);
            Assert.Equal("2", l);
            Assert.Equal("done", powerPool.Fetch<string>(id).Result);
        }

        [Fact]
        public void Test49()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test50()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test51()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test52()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, out _, (res) =>
            {
                c = "3";
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
        }

        [Fact]
        public void Test53()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, out _, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test54()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test55()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test56()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.WorkEnded += (s, e) =>
            {
                r = e.Result;
            };
            powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test57()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            string r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, out _, (res) =>
            {
                c = "3";
                r = res.Result;
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test58()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            var id = powerPool.QueueWorkItem(async (p1, p2) =>
            {
                p = p1 + p2;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, out _, new WorkOption() { ShouldStoreResult = true });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(3, p);
            Assert.Equal("2", l);
            Assert.Equal("done", powerPool.Fetch<string>(id).Result);
        }

        [Fact]
        public void Test59()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test60()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test61()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test62()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, out _, (res) =>
            {
                c = "3";
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
        }

        [Fact]
        public void Test63()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, out _, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test64()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test65()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test66()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.WorkEnded += (s, e) =>
            {
                r = e.Result;
            };
            powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test67()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            string r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, out _, (res) =>
            {
                c = "3";
                r = res.Result;
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test68()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            var id = powerPool.QueueWorkItem(async (p1, p2, p3) =>
            {
                p = p1 + p2 + p3;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, out _, new WorkOption() { ShouldStoreResult = true });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(6, p);
            Assert.Equal("2", l);
            Assert.Equal("done", powerPool.Fetch<string>(id).Result);
        }

        [Fact]
        public void Test69()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3+ p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test70()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3 + p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test71()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3+ p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test72()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3+ p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4, out _, (res) =>
            {
                c = "3";
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
        }

        [Fact]
        public void Test73()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3+ p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4, out _, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test74()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3+ p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test75()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3 + p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test76()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.WorkEnded += (s, e) =>
            {
                r = e.Result;
            };
            powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3+ p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test77()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            string r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3+ p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4, out _, (res) =>
            {
                c = "3";
                r = res.Result;
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test78()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            var id = powerPool.QueueWorkItem(async (p1, p2, p3, p4) =>
            {
                p = p1 + p2 + p3+ p4;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4, out _, new WorkOption() { ShouldStoreResult = true });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(10, p);
            Assert.Equal("2", l);
            Assert.Equal("done", powerPool.Fetch<string>(id).Result);
        }

        [Fact]
        public void Test79()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4, 5);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test80()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4, 5, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test81()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4, 5, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test82()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4, 5, out _, (res) =>
            {
                c = "3";
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
        }

        [Fact]
        public void Test83()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
            }, 1, 2, 3, 4, 5, out _, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test84()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4, 5);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test85()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4, 5, new WorkOption());
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
        }

        [Fact]
        public void Test86()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.WorkEnded += (s, e) =>
            {
                r = e.Result;
            };
            powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4, 5, out _);
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test87()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            string r = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4, 5, out _, (res) =>
            {
                c = "3";
                r = res.Result;
            });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
            Assert.Equal("3", c);
            Assert.Equal("done", r);
        }

        [Fact]
        public void Test88()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            object p = null;
            object l = null;
            object c = null;
            PowerPool powerPool = new PowerPool();
            var id = powerPool.QueueWorkItem(async (p1, p2, p3, p4, p5) =>
            {
                p = p1 + p2 + p3 + p4 + p5;
                await Task.Delay(100);
                await Task.Delay(100);
                l = "2";
                return "done";
            }, 1, 2, 3, 4, 5, out _, new WorkOption() { ShouldStoreResult = true });
            Thread.Sleep(1000);
            powerPool.Wait();
            Assert.Equal(15, p);
            Assert.Equal("2", l);
            Assert.Equal("done", powerPool.Fetch<string>(id).Result);
        }

        [Fact]
        public void testSugar1()
        {
            PowerPool powerPool = new PowerPool();
            WorkID id = powerPool + (() => { });
            Assert.False(id == null);
        }

        [Fact]
        public void testSugar2()
        {
            int doneCount = 0;
            PowerPool powerPool = new PowerPool();
            _ = powerPool
                | (() => { Interlocked.Increment(ref doneCount); })
                | (() => { Interlocked.Increment(ref doneCount); })
                | (() => { Interlocked.Increment(ref doneCount); });
            powerPool.Wait();
            Assert.Equal(3, doneCount);
        }

        [Fact]
        public void testSugar3()
        {
            int doneCount = 0;
            PowerPool powerPool = new PowerPool();
            powerPool |= () => { Interlocked.Increment(ref doneCount); };
            powerPool.Wait();
            Assert.Equal(1, doneCount);
        }
    }
}
