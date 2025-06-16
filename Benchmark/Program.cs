using BenchmarkDotNet.Running;

namespace Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmark>();
            BenchmarkRunner.Run<BenchmarkAsync>();
        }
    }
}
