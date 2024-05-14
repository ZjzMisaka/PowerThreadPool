using PowerThreadPool;
using PowerThreadPool.Options;

namespace UnitTest
{
    public class QueueWorkItemTest
    {
        [Fact]
        public void Test1()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() => { p = "1"; }, (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test2()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() => { p = "1"; }, new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test3()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem((object[] param) => { p = param[0]; }, new[] { "1" } , (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test4()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem((object[] param) => { p = param[0]; }, new[] { "1" }, new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test5()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string>((string param) => { p = param; }, "1", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test6()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string>((string param) => { p = param; }, "1", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test7()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string>((string param1, string param2) => { p = param1; }, "1", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test8()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string>((string param1, string param2) => { p = param1; }, "1", "", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test9()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string>((string param1, string param2, string param3) => { p = param1; }, "1", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test10()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string>((string param1, string param2, string param3) => { p = param1; }, "1", "", "", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test11()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string>((string param1, string param2, string param3, string param4) => { p = param1; }, "1", "", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test12()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string>((string param1, string param2, string param3, string param4) => { p = param1; }, "1", "", "", "", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test13()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string>((string param1, string param2, string param3, string param4, string param5) => { p = param1; }, "1", "", "", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test14()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string>((string param1, string param2, string param3, string param4, string param5) => { p = param1; }, "1", "", "", "", "", new WorkOption());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test15()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, int>((string param) => { p = param; return int.Parse(param); }, "1", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test16()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, int>((string param) => { p = param; return int.Parse(param); }, "1", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test17()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, int>((string param1, string param2) => { p = param1; return 0; }, "1", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test18()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, int>((string param1, string param2) => { p = param1; return 0; }, "1", "", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test19()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, int>((string param1, string param2, string param3) => { p = param1; return 0; }, "1", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test20()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, int>((string param1, string param2, string param3) => { p = param1; return 0; }, "1", "", "", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test21()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, int>((string param1, string param2, string param3, string param4) => { p = param1; return 0; }, "1", "", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test22()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, int>((string param1, string param2, string param3, string param4) => { p = param1; return 0; }, "1", "", "", "", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test23()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string, int>((string param1, string param2, string param3, string param4, string param5) => { p = param1; return 0; }, "1", "", "", "", "", (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test24()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string, int>((string param1, string param2, string param3, string param4, string param5) => { p = param1; return 0; }, "1", "", "", "", "", new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test25()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>(() => { p = "1"; return 0; }, (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test26()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>(() => { p = "1"; return 0; }, new WorkOption<int>());
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test27()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>((param) => { p = param[0]; return 0; }, new[] { "1" }, (res) => { });
            powerPool.Wait();
            Assert.Equal("1", p);
        }

        [Fact]
        public void Test28()
        {
            object p = null;
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>((param) => { p = param[0]; return 0; }, new[] { "1" }, new WorkOption<int>()); 
            powerPool.Wait();
            Assert.Equal("1", p);
        }
    }
}
