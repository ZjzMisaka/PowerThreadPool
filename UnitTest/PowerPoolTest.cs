using PowerThreadPool;
using PowerThreadPool.Option;

namespace UnitTest
{
    public class PowerPoolTest
    {
        PowerPool powerPool;
        [Fact]
        public void TestOrder()
        {
            List<string> logList = new List<string>();
            powerPool = new PowerPool();
            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    logList.Add("DefaultCallback");
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 },
                Timeout = new TimeoutOption() { Duration = 60000, ForceStop = false },
                DefaultThreadTimeout = new TimeoutOption() { Duration = 10000, ForceStop = false },
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

            powerPool.QueueWorkItem(() => { logList.Add("RUNNING"); });

            powerPool.Wait();

            Assert.Collection<string>(logList,
                item => Assert.Equal("ThreadPoolStart", item),
                item => Assert.Equal("ThreadStart", item),
                item => Assert.Equal("RUNNING", item),
                item => Assert.Equal("ThreadEnd", item),
                item => Assert.Equal("DefaultCallback", item),
                item => Assert.Equal("ThreadPoolIdle", item)
                );
        }
    }
}