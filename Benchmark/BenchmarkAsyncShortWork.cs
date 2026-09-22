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

        private int _tpErrorCount = -1;
        private int _ptpErrorCount = -1;

        private readonly int _maxCount = 500000;

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
                    for (int j = 0; j < 10; ++j)
                    {
                        await Task.Yield();
                    }
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
                    for (int j = 0; j < 10; ++j)
                    {
                        await Task.Yield();
                    }
                    Interlocked.Increment(ref powerThreadPoolRunCount);
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
        public async Task TestPowerThreadPoolWaitAsync()
        {
            int powerThreadPoolRunCount = 0;
            for (int i = 0; i < _maxCount; ++i)
            {
                _powerPool.QueueWorkItem(async () =>
                {
                    for (int j = 0; j < 10; ++j)
                    {
                        await Task.Yield();
                    }
                    Interlocked.Increment(ref powerThreadPoolRunCount);
                });
            }

            await _powerPool.WaitAsync();

            int count = powerThreadPoolRunCount;
            if (count != _maxCount)
            {
                _ptpErrorCount = count;
            }
        }

        [Benchmark]
        public async Task TestPowerThreadPoolWaitAsyncDisableWorkTracking()
        {
            int powerThreadPoolRunCount = 0;
            _powerPool.PowerPoolOption.EnableWorkTracking = false;
            for (int i = 0; i < _maxCount; ++i)
            {
                _powerPool.QueueWorkItem(async () =>
                {
                    for (int j = 0; j < 10; ++j)
                    {
                        await Task.Yield();
                    }
                    Interlocked.Increment(ref powerThreadPoolRunCount);
                });
            }

            await _powerPool.WaitAsync();

            int count = powerThreadPoolRunCount;
            if (count != _maxCount)
            {
                _ptpErrorCount = count;
            }
        }
    }
}
