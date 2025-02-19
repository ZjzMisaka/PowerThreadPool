using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Options;
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
        public void testSugar1()
        {
            PowerPool powerPool = new PowerPool();
            string id = powerPool + (() => { });
            Assert.NotEmpty(id);
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
