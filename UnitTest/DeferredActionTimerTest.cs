using System.Diagnostics;
using System.Reflection;
using Xunit.Abstractions;

namespace UnitTest
{
    public class DeferredActionTimerTest
    {
        private readonly ITestOutputHelper _output;
        Stopwatch _stopwatch = new Stopwatch();

        public DeferredActionTimerTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestDeferredActionTimer1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            bool isCallbackInvoked = false;

            DeferredActionTimer timer = new DeferredActionTimer();
            _stopwatch.Start();
            timer.Set(2000, () =>
            {
                _stopwatch.Stop();
                isCallbackInvoked = true;
            });
            Thread.Sleep(1000);
            timer.Pause();
            Thread.Sleep(1000);
            timer.Resume();

            while (!isCallbackInvoked)
            {
                Thread.Sleep(100);
            }

            Assert.InRange(_stopwatch.ElapsedMilliseconds, 2900, 3100);

            timer.Dispose();
        }

        [Fact]
        public void TestDeferredActionTimer2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            DateTime startTime = DateTime.UtcNow;
            DateTime endTime = DateTime.UtcNow;

            DeferredActionTimer timer = new DeferredActionTimer(() =>
            {
                endTime = DateTime.UtcNow;
            }, true);
            _stopwatch.Start();
            timer.Set(100);
            Thread.Sleep(1000);
            timer.Cancel();

            Thread.Sleep(1000);
            Assert.InRange((endTime - startTime).TotalMilliseconds, 900, 1100);

            timer.Dispose();
        }
    }
}
