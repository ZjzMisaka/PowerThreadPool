using BenchmarkDotNet.Engines;
using PowerThreadPool;
using PowerThreadPool.Options;

namespace PowerThreadPoolConsoleTest
{
    internal class Program
    {
        private static PowerPool s_powerPool;
        private static readonly Consumer s_consumer = new Consumer();

        private static readonly int s_maxCount = 100000000;

        static void Main(string[] args)
        {
            s_powerPool = new PowerPool(new PowerPoolOption
            {
                MaxThreads = Environment.ProcessorCount
            });

            TestPowerThreadPool();
        }

        public static void TestPowerThreadPool()
        {
            int powerThreadPoolRunCount = 0;
            for (int i = 0; i < s_maxCount; ++i)
            {
                s_powerPool.QueueWorkItem(() =>
                {
                    Interlocked.Increment(ref powerThreadPoolRunCount);
                    DoWork();
                });
            }
            s_powerPool.Wait();
            int count = powerThreadPoolRunCount;
            
            Console.WriteLine(count);
        }

        private static void DoWork()
        {
            double sum = 0;
            for (int i = 0; i < 10; ++i)
            {
                sum += Math.Sqrt(i);
            }
            s_consumer.Consume(sum);
        }
    }
}
