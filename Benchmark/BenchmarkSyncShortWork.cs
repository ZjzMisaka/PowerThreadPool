using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using PowerThreadPool;
using PowerThreadPool.Options;

namespace Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    [MemoryDiagnoser]
    public class BenchmarkSyncShortWork
    {
        private PowerPool _powerPool;

        private readonly Consumer _consumer = new Consumer();

        private int _tpErrorCount = -1;
        private int _ptpErrorCount = -1;

        private readonly int _maxCount = 1000000;

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
        public void TestDotnetThreadPool()
        {
            int threadPoolRunCount = 0;
            using (CountdownEvent countdown = new CountdownEvent(_maxCount))
            {
                for (int i = 0; i < _maxCount; ++i)
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
                _powerPool.QueueWorkItem(() =>
                {
                    Interlocked.Increment(ref powerThreadPoolRunCount);
                    DoWork();
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
        public void TestPowerThreadPoolSetEnablePoolIdleCheck()
        {
            int powerThreadPoolRunCount = 0;
            _powerPool.EnablePoolIdleCheck = false;
            for (int i = 0; i < _maxCount; ++i)
            {
                _powerPool.QueueWorkItem(() =>
                {
                    Interlocked.Increment(ref powerThreadPoolRunCount);
                    DoWork();
                });
            }
            _powerPool.EnablePoolIdleCheck = true;
            _powerPool.Wait();
            int count = powerThreadPoolRunCount;
            if (count != _maxCount)
            {
                _ptpErrorCount = count;
            }
        }

        private void DoWork()
        {
            double sum = 0;
            for (int i = 0; i < 10; ++i)
            {
                sum += Math.Sqrt(i);
            }
            _consumer.Consume(sum);
        }
    }
}
