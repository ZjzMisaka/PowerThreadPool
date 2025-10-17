using Amib.Threading;
using BenchmarkDotNet.Attributes;
using PowerThreadPool;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class BenchmarkCPUWork
    {
        private SmartThreadPool _smartThreadPool;
        private PowerPool _powerPool;

        [GlobalSetup]
        public void Setup()
        {
            _smartThreadPool = new SmartThreadPool();
            _smartThreadPool.MinThreads = 8;
            _smartThreadPool.MaxThreads = 8;
            _powerPool = new PowerPool(new PowerThreadPool.Options.PowerPoolOption { MaxThreads = 8 });
            ThreadPool.SetMinThreads(8, 8);
            ThreadPool.SetMaxThreads(8, 8);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _smartThreadPool.Shutdown();
            _smartThreadPool.Dispose();
            _powerPool.Stop();
            _powerPool.Dispose();
        }

        [Benchmark]
        public void TestDotnetThreadPool()
        {
            try
            {
                int threadPoolRunCount = 0;
                using (CountdownEvent countdown = new CountdownEvent(1000))
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
                                Interlocked.Increment(ref threadPoolRunCount);
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

                int count = threadPoolRunCount;
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
                int smartThreadPoolRunCount = 0;

                for (int i = 0; i < 1000; ++i)
                {
                    _smartThreadPool.QueueWorkItem(() =>
                    {
                        Interlocked.Increment(ref smartThreadPoolRunCount);
                        DoWork();
                    });
                }
                _smartThreadPool.WaitForIdle();

                int count = smartThreadPoolRunCount;
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
                int powerThreadPoolRunCount = 0;
                for (int i = 0; i < 1000; ++i)
                {
                    _powerPool.QueueWorkItem(() =>
                    {
                        Interlocked.Increment(ref powerThreadPoolRunCount);
                        DoWork();
                    });
                }
                _powerPool.Wait();
                int count = powerThreadPoolRunCount;
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
