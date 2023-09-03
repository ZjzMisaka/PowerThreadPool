using PowerThreadPool;
using PowerThreadPool.Option;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    public class QueueWorkItemTest
    {
        [Fact]
        public void Test1()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() => { }, (res) => { });
        }

        [Fact]
        public void Test2()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem(() => { }, new ThreadOption());
        }

        [Fact]
        public void Test3()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem((object[] param) => { }, new object[0], (res) => { });
        }

        [Fact]
        public void Test4()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem((object[] param) => { }, new object[0], new ThreadOption());
        }

        [Fact]
        public void Test5()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string>((string param) => { }, "", (res) => { });
        }

        [Fact]
        public void Test6()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string>((string param) => { }, "", new ThreadOption());
        }

        [Fact]
        public void Test7()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string>((string param1, string param2) => { }, "", "", (res) => { });
        }

        [Fact]
        public void Test8()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string>((string param1, string param2) => { }, "", "", new ThreadOption());
        }

        [Fact]
        public void Test9()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string>((string param1, string param2, string param3) => { }, "", "", "", (res) => { });
        }

        [Fact]
        public void Test10()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string>((string param1, string param2, string param3) => { }, "", "", "", new ThreadOption());
        }

        [Fact]
        public void Test11()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string>((string param1, string param2, string param3, string param4) => { }, "", "", "", "", (res) => { });
        }

        [Fact]
        public void Test12()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string>((string param1, string param2, string param3, string param4) => { }, "", "", "", "", new ThreadOption());
        }

        [Fact]
        public void Test13()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string>((string param1, string param2, string param3, string param4, string param5) => { }, "", "", "", "", "", (res) => { });
        }

        [Fact]
        public void Test14()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string>((string param1, string param2, string param3, string param4, string param5) => { }, "", "", "", "", "", new ThreadOption());
        }

        [Fact]
        public void Test15()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, int>((string param) => { return 0; }, "", (res) => { });
        }

        [Fact]
        public void Test16()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, int>((string param) => { return 0; }, "", new ThreadOption<int>());
        }

        [Fact]
        public void Test17()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, int>((string param1, string param2) => { return 0; }, "", "", (res) => { });
        }

        [Fact]
        public void Test18()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, int>((string param1, string param2) => { return 0; }, "", "", new ThreadOption<int>());
        }

        [Fact]
        public void Test19()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, int>((string param1, string param2, string param3) => { return 0; }, "", "", "", (res) => { });
        }

        [Fact]
        public void Test20()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, int>((string param1, string param2, string param3) => { return 0; }, "", "", "", new ThreadOption<int>());
        }

        [Fact]
        public void Test21()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, int>((string param1, string param2, string param3, string param4) => { return 0; }, "", "", "", "", (res) => { });
        }

        [Fact]
        public void Test22()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, int>((string param1, string param2, string param3, string param4) => { return 0; }, "", "", "", "", new ThreadOption<int>());
        }

        [Fact]
        public void Test23()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string, int>((string param1, string param2, string param3, string param4, string param5) => { return 0; }, "", "", "", "", "", (res) => { });
        }

        [Fact]
        public void Test24()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<string, string, string, string, string, int>((string param1, string param2, string param3, string param4, string param5) => { return 0; }, "", "", "", "", "", new ThreadOption<int>());
        }

        [Fact]
        public void Test25()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>(() => { return 0; }, (res) => { });
        }

        [Fact]
        public void Test26()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>(() => { return 0; }, new ThreadOption<int>());
        }

        [Fact]
        public void Test27()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>((param) => { return 0; }, new object[0], (res) => { });
        }

        [Fact]
        public void Test28()
        {
            PowerPool powerPool = new PowerPool();
            powerPool.QueueWorkItem<int>((param) => { return 0; }, new object[0], new ThreadOption<int>());
        }
    }
}
