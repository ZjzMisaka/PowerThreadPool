using BenchmarkDotNet.Attributes;
using PowerThreadPool;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class BenchmarkAsync
    {
        private PowerPool _powerPool;

        [GlobalSetup]
        public void Setup()
        {
            _powerPool = new PowerPool(new PowerThreadPool.Options.PowerPoolOption { MaxThreads = 8 });
            ThreadPool.SetMinThreads(8, 8);
            ThreadPool.SetMaxThreads(8, 8);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _powerPool.Stop();
            _powerPool.Dispose();
        }

        [Benchmark]
        public void TestTask()
        {
            try
            {
                int threadPoolRunCount = 0;

                Task[] tasks = new Task[50];

                for (int i = 0; i < 50; ++i)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        await Task.Delay(10);
                        await Task.Delay(10);
                        await Task.Delay(10);
                        Interlocked.Increment(ref threadPoolRunCount);
                    });
                }

                Task.WhenAll(tasks).Wait();

                int count = threadPoolRunCount;
                if (count != 50)
                {
                    throw new InvalidOperationException($"Task: {count} -> 50");
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
                for (int i = 0; i < 50; ++i)
                {
                    _powerPool.QueueWorkItemAsync(async () =>
                    {
                        await Task.Delay(10);
                        await Task.Delay(10);
                        await Task.Delay(10);
                        Interlocked.Increment(ref powerThreadPoolRunCount);
                        return true;
                    });
                }
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

        [Benchmark]
        public void TestPowerThreadPoolSync()
        {
            try
            {
                int powerThreadPoolRunCount = 0;
                for (int i = 0; i < 50; ++i)
                {
                    _powerPool.QueueWorkItem(() =>
                    {
                        Task.Delay(10).Wait();
                        Task.Delay(10).Wait();
                        Task.Delay(10).Wait();
                        Interlocked.Increment(ref powerThreadPoolRunCount);
                        return true;
                    });
                }
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
