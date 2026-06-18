using BenchmarkDotNet.Attributes;
using PowerThreadPool;
using PowerThreadPool.Options;

namespace Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class BenchmarkAsyncWork
    {
        private PowerPool _powerPool;

        [IterationSetup]
        public void Setup()
        {
            _powerPool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = Environment.ProcessorCount
            });
            ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);
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
                    _powerPool.QueueWorkItem(async () =>
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
