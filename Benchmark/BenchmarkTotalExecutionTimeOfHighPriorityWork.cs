using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using PowerThreadPool;
using PowerThreadPool.Options;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class BenchmarkTotalExecutionTimeOfHighPriorityWork
    {
        private PowerPool _powerPool;
        private ThreadStaticPrioritySeedScheduler _threadStaticPrioritySeedScheduler;
        private PerPriorityLaneScheduler _perPriorityLaneScheduler;
        private PriorityDispatcherContext _priorityDispatcherContext;

        [IterationSetup]
        public void Setup()
        {
            _powerPool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = Environment.ProcessorCount
            });
            _threadStaticPrioritySeedScheduler = new ThreadStaticPrioritySeedScheduler(Environment.ProcessorCount);
            _perPriorityLaneScheduler = new PerPriorityLaneScheduler(Environment.ProcessorCount);
            _priorityDispatcherContext = new PriorityDispatcherContext(Environment.ProcessorCount);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _powerPool.Wait();
            _powerPool.Stop();
            _powerPool.Dispose();
            _threadStaticPrioritySeedScheduler.Dispose();
            _perPriorityLaneScheduler.Dispose();
            _priorityDispatcherContext.Dispose();
        }

        [Benchmark]
        public void TestPriorityNotPropagatedThreadStaticSeed()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 50; i++)
            {
                _threadStaticPrioritySeedScheduler.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    2);
            }
            Thread.Sleep(1);
            for (int i = 0; i < 200; i++)
            {
                _threadStaticPrioritySeedScheduler.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    2);
                _threadStaticPrioritySeedScheduler.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    1);
                var task = _threadStaticPrioritySeedScheduler.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    0);
                tasks.Add(task);
            }

            Task.WhenAll(tasks).Wait();

            cts.Cancel();
        }

        [Benchmark]
        public void TestPriorityPropagatedViaPerPriorityLane()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 50; i++)
            {
                _perPriorityLaneScheduler.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    2);
            }
            Thread.Sleep(1);
            for (int i = 0; i < 200; i++)
            {
                _perPriorityLaneScheduler.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    2);
                _perPriorityLaneScheduler.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    1);
                var task = _perPriorityLaneScheduler.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    0);
                tasks.Add(task);
            }

            Task.WhenAll(tasks).Wait();

            cts.Cancel();
        }

        [Benchmark]
        public void TestPriorityPropagatedViaSyncContext()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 50; i++)
            {
                _priorityDispatcherContext.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    2);
            }
            Thread.Sleep(1);
            for (int i = 0; i < 200; i++)
            {
                _priorityDispatcherContext.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    2);
                _priorityDispatcherContext.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    1);
                var task = _priorityDispatcherContext.Run(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    0);
                tasks.Add(task);
            }

            Task.WhenAll(tasks).Wait();

            cts.Cancel();
        }

        [Benchmark]
        public void TestPowerThreadPool()
        {
            WorkOption workOptionLow = new WorkOption { WorkPriority = 0 };
            WorkOption workOptionNormal = new WorkOption { WorkPriority = 1 };
            WorkOption workOptionHighest = new WorkOption { WorkPriority = 2 };

            CancellationTokenSource cts = new CancellationTokenSource();

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 50; i++)
            {
                _powerPool.QueueWorkItem(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    workOptionLow);
            }
            Thread.Sleep(1);
            for (int i = 0; i < 200; i++)
            {
                _powerPool.QueueWorkItem(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    workOptionLow);
                _powerPool.QueueWorkItem(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    workOptionNormal);
                _powerPool.QueueWorkItem(
                    async () =>
                    {
                        await DoWorkAsync(cts.Token);
                    },
                    out Task task,
                    workOptionHighest);
                tasks.Add(task);
            }

            Task.WhenAll(tasks).Wait();

            cts.Cancel();
        }

        private async Task DoWorkAsync(CancellationToken token)
        {
            SpinFor(2, token);
            await InnerWorkAsync(token);
            SpinFor(2, token);
            await InnerWorkAsync(token);
            SpinFor(2, token);
        }

        private async Task InnerWorkAsync(CancellationToken token)
        {
            await Task.Delay(3, token);
        }

        void SpinFor(int ms, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < ms)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                Thread.SpinWait(1000);
            }
        }
    }
}
