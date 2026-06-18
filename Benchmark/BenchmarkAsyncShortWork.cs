using BenchmarkDotNet.Attributes;
using PowerThreadPool;
using PowerThreadPool.Options;

namespace Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class BenchmarkAsyncShortWork
    {
        private PowerPool _powerPool;

        [IterationSetup]
        public void Setup()
        {
            _powerPool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = 8
            });
            ThreadPool.SetMinThreads(8, 8);
            ThreadPool.SetMaxThreads(8, 8);
        }

        [IterationCleanup]
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

                Task[] tasks = new Task[10000];

                for (int i = 0; i < 10000; ++i)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        Interlocked.Increment(ref threadPoolRunCount);
                    });
                }

                Task.WhenAll(tasks).Wait();

                int count = threadPoolRunCount;
                if (count != 10000)
                {
                    throw new InvalidOperationException($"Task: {count} -> 10000");
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
                for (int i = 0; i < 10000; ++i)
                {
                    _powerPool.QueueWorkItem(async () =>
                    {
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        await Task.Delay(0);
                        Interlocked.Increment(ref powerThreadPoolRunCount);
                        return true;
                    });
                }
                _powerPool.Wait();
                int count = powerThreadPoolRunCount;
                if (count != 10000)
                {
                    throw new InvalidOperationException($"TestPowerThreadPool: {count} -> 10000");
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
                for (int i = 0; i < 10000; ++i)
                {
                    _powerPool.QueueWorkItem(() =>
                    {
                        Task.Delay(0).Wait();
                        Task.Delay(0).Wait();
                        Task.Delay(0).Wait();
                        Task.Delay(0).Wait();
                        Task.Delay(0).Wait();
                        Task.Delay(0).Wait();
                        Task.Delay(0).Wait();
                        Task.Delay(0).Wait();
                        Task.Delay(0).Wait();
                        Task.Delay(0).Wait();
                        Interlocked.Increment(ref powerThreadPoolRunCount);
                        return true;
                    });
                }
                _powerPool.Wait();
                int count = powerThreadPoolRunCount;
                if (count != 10000)
                {
                    throw new InvalidOperationException($"TestPowerThreadPool: {count} -> 10000");
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
