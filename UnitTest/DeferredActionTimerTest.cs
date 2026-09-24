using System.Diagnostics;
using System.Reflection;
using PowerThreadPool.Helpers.Timers;
using Xunit.Abstractions;

namespace UnitTest
{
    public class DeferredActionTimerTest
    {
        private readonly ITestOutputHelper _output;
        private readonly Stopwatch _stopwatch;

        public DeferredActionTimerTest(ITestOutputHelper output)
        {
            _output = output;
            _stopwatch = new Stopwatch();
        }

        [Fact]
        public void TestDeferredActionTimer1()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            bool isCallbackInvoked = false;

            DeferredActionTimer timer = new DeferredActionTimer();

            timer.Pause();
            timer.Resume();

            _stopwatch.Start();
            timer.Set(2000, () =>
            {
                _stopwatch.Stop();
                isCallbackInvoked = true;
            });
            Thread.Sleep(1000);
            timer.Pause();
            timer.Pause();
            Thread.Sleep(1000);
            timer.Resume();
            timer.Resume();

            while (!isCallbackInvoked)
            {
                Thread.Sleep(100);
            }

            timer.Pause();
            timer.Resume();

            Assert.InRange(_stopwatch.ElapsedMilliseconds, 2799, 3200);

            timer.Dispose();
        }

        [Fact]
        public void TestDeferredActionTimer2()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

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
            Assert.InRange((endTime - startTime).TotalMilliseconds, 799, 1200);

            timer.Dispose();
        }

        [Fact]
        public void TestDeferredActionTimer3()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().ReflectedType.Name}");

            DateTime startTime = DateTime.UtcNow;
            DateTime endTime = DateTime.UtcNow;

            DeferredActionTimer timer = new DeferredActionTimer(() =>
            {
                endTime = DateTime.UtcNow;
            }, true);
            _stopwatch.Start();
            timer.Set(100);
            Thread.Sleep(300);
            timer.Pause();
            Thread.Sleep(1000);
            timer.Resume();
            Thread.Sleep(300);
            timer.Cancel();
            Assert.InRange((endTime - startTime).TotalMilliseconds, 1399, 1800);

            timer.Dispose();
        }
    }
}
