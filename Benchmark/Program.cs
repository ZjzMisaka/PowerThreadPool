using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Config config = new Config();
            BenchmarkRunner.Run<BenchmarkSyncWork>(config);
            BenchmarkRunner.Run<BenchmarkAsyncWork>(config);
            BenchmarkRunner.Run<BenchmarkSyncShortWork>(config);
            BenchmarkRunner.Run<BenchmarkAsyncShortWork>(config);
            BenchmarkRunner.Run<BenchmarkTotalExecutionTimeOfHighPriorityWork>(config);
            BenchmarkRunner.Run<BenchmarkTotalExecutionTimeOfAllPriorityWork>(config);
            Console.WriteLine("OK");
            Console.ReadLine();
        }
    }

    public class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddExporter(CsvExporter.Default);
            AddLogger(ConsoleLogger.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            ArtifactsPath = "BenchmarkResults";
        }
    }
}
