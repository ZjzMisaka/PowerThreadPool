using Amib.Threading;
using BenchmarkDotNet.Attributes;
using PowerThreadPool;

namespace Benchmark
{
    public class Benchmark
    {
        private SmartThreadPool _smartThreadPool;
        private PowerPool _powerPool;

        private int _smartThreadPoolRunCount;
        private int _powerThreadPoolRunCount;
        private int _threadPoolRunCount;

        [GlobalSetup]
        public void Setup()
        {
            _smartThreadPool = new SmartThreadPool();
            _smartThreadPool.MaxThreads = 8;
            _powerPool = new PowerPool(new PowerThreadPool.Options.PowerPoolOption() { MaxThreads = 8 });
            ThreadPool.SetMaxThreads(8, 8);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _smartThreadPool.Shutdown();
            _powerPool.Dispose();
        }

        [Benchmark]
        public void TestDotnetThreadPool()
        {
            try
            {
                _threadPoolRunCount = 0;
                using (CountdownEvent countdown = new CountdownEvent(1000))
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
                                Interlocked.Increment(ref _threadPoolRunCount);
                                DoWork();
                            }
                            finally
                            {
                                countdown.Signal();
                            }
                        });
                    }

                    countdown.Wait();
                }

                int count = _threadPoolRunCount;
                if (count != 1000)
                {
                    throw new InvalidOperationException($"TestDotnetThreadPool: {count} -> 1000");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        [Benchmark]
        public void TestSmartThreadPool()
        {
            try
            {
                _smartThreadPoolRunCount = 0;
                for (int i = 0; i < 1000; ++i)
                {
                    _smartThreadPool.QueueWorkItem(() =>
                    {
                        Interlocked.Increment(ref _smartThreadPoolRunCount);
                        DoWork();
                    });
                }
                while (_smartThreadPoolRunCount != 1000)
                {
                    Thread.Yield();
                    _smartThreadPool.WaitForIdle();
                }
                
                int count = _smartThreadPoolRunCount;
                if (count != 1000)
                {
                    throw new InvalidOperationException($"TestSmartThreadPool: {count} -> 1000");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        [Benchmark]
        public void TestPowerThreadPool()
        {
            try
            {
                _powerThreadPoolRunCount = 0;
                _powerPool.EnablePoolIdleCheck = false;
                for (int i = 0; i < 1000; ++i)
                {
                    _powerPool.QueueWorkItem(() =>
                    {
                        Interlocked.Increment(ref _powerThreadPoolRunCount);
                        DoWork();
                    });
                }
                _powerPool.EnablePoolIdleCheck = true;
                _powerPool.Wait();
                int count = _powerThreadPoolRunCount;
                if (count != 1000)
                {
                    throw new InvalidOperationException($"TestPowerThreadPool: {count} -> 1000");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        private void DoWork()
        {
            for (int i = 0; i < 100000; ++i)
            {
                Math.Sqrt(i);
            }
        }
    }
}
