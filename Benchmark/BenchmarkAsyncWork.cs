using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using PowerThreadPool;
using PowerThreadPool.Options;

namespace Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    [EventPipeProfiler(EventPipeProfile.CpuSampling)]
    public class BenchmarkAsyncWork
    {
        private PowerPool _powerPool;

        private int _tpErrorCount = -1;
        private int _ptpErrorCount = -1;

        private readonly int _maxCount = 50;

        [IterationSetup]
        public void Setup()
        {
            _powerPool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = Environment.ProcessorCount
            });
            ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);

            _tpErrorCount = -1;
            _ptpErrorCount = -1;
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _powerPool.Stop();
            _powerPool.Dispose();

            if (_tpErrorCount > 0)
            {
                Console.WriteLine($"TestDotnetThreadPool: {_tpErrorCount} -> {_maxCount}");
            }
            if (_ptpErrorCount > 0)
            {
                Console.WriteLine($"TestPowerThreadPool: {_ptpErrorCount} -> {_maxCount}");
            }
        }

        [Benchmark(Baseline = true)]
        public void TestTask()
        {
            int threadPoolRunCount = 0;

            Task[] tasks = new Task[_maxCount];

            for (int i = 0; i < _maxCount; ++i)
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
            if (count != _maxCount)
            {
                _tpErrorCount = count;
            }
        }

        [Benchmark]
        public void TestPowerThreadPool()
        {
            int powerThreadPoolRunCount = 0;
            for (int i = 0; i < _maxCount; ++i)
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
            if (count != _maxCount)
            {
                _ptpErrorCount = count;
            }
        }

        [Benchmark]
        public void TestPowerThreadPoolSync()
        {
            int powerThreadPoolRunCount = 0;
            for (int i = 0; i < _maxCount; ++i)
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
            if (count != _maxCount)
            {
                _ptpErrorCount = count;
            }
        }
    }
}
