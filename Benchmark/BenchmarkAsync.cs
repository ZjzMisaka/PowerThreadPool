using Amib.Threading;
using BenchmarkDotNet.Attributes;
using PowerThreadPool;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class BenchmarkAsync
    {
        private SmartThreadPool _smartThreadPool;
        private PowerPool _powerPool;
        private ManualResetEvent _signal;

        [GlobalSetup]
        public void Setup()
        {
            _signal = new ManualResetEvent(false);
            _smartThreadPool = new SmartThreadPool();
            _smartThreadPool.MaxThreads = 8;
            _powerPool = new PowerPool(new PowerThreadPool.Options.PowerPoolOption { MaxThreads = 8 });
            ThreadPool.SetMaxThreads(8, 8);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _signal.Dispose();
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
                using (CountdownEvent countdown = new CountdownEvent(50))
                {
                    for (int i = 0; i < 50; ++i)
                    {
                        ThreadPool.QueueUserWorkItem(async state =>
                        {
                            try
                            {
                                Interlocked.Increment(ref threadPoolRunCount);
                                await Task.Delay(10);
                                await Task.Delay(10);
                                await Task.Delay(10);
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
                if (count != 50)
                {
                    throw new InvalidOperationException($"TestDotnetThreadPool: {count} -> 100");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        //[Benchmark]
        //public void TestSmartThreadPool()
        //{
        //    try
        //    {
        //        int smartThreadPoolRunCount = 0;

        //        for (int i = 0; i < 50; ++i)
        //        {
        //            _smartThreadPool.QueueWorkItem(async () =>
        //            {
        //                Interlocked.Increment(ref smartThreadPoolRunCount);
        //                await Task.Delay(10);
        //                await Task.Delay(10);
        //                await Task.Delay(10);
        //                if (smartThreadPoolRunCount == 50)
        //                {
        //                    _signal.Set();
        //                }
        //                return true;
        //            });
        //        }
        //        _signal.WaitOne();
        //        _smartThreadPool.WaitForIdle();

        //        int count = smartThreadPoolRunCount;
        //        //if (count != 50)
        //        //{
        //        //    throw new InvalidOperationException($"TestSmartThreadPool: {count} -> 50");
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        Console.ReadLine();
        //    }
        //}

        [Benchmark]
        public void TestPowerThreadPool()
        {
            try
            {
                int powerThreadPoolRunCount = 0;
                _powerPool.EnablePoolIdleCheck = false;
                for (int i = 0; i < 50; ++i)
                {
                    _powerPool.QueueWorkItemAsync(async () =>
                    {
                        Interlocked.Increment(ref powerThreadPoolRunCount);
                        await Task.Delay(10);
                        await Task.Delay(10);
                        await Task.Delay(10);
                        return true;
                    });
                }
                _powerPool.EnablePoolIdleCheck = true;
                _powerPool.Wait();
                int count = powerThreadPoolRunCount;
                if (count != 50)
                {
                    throw new InvalidOperationException($"TestPowerThreadPool: {count} -> 50");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
    }
}
