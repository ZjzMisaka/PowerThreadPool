using BenchmarkDotNet.Running;

namespace Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BenchmarkSyncWork>();
            BenchmarkRunner.Run<BenchmarkAsyncWork>();
            BenchmarkRunner.Run<BenchmarkSyncShortWork>();
            BenchmarkRunner.Run<BenchmarkAsyncShortWork>();
            Console.WriteLine("OK");
            Console.ReadLine();
        }
    }
}
