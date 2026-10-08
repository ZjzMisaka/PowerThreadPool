using System.Reflection;
using PowerThreadPool;
using PowerThreadPool.Options;
using Xunit.Abstractions;

namespace UnitTest
{
    /// <summary>
    /// A realistic business sample that can benefit from merged continuation
    /// scheduling in PowerPoolSynchronizationContext.
    ///
    /// Shape: one work fans out parallel async I/O (fetch several endpoints and
    /// aggregate). Every child async method is started on the worker, so they all
    /// capture the SAME work synchronization context. As the remote calls complete
    /// on network/timer threads, each child posts its continuation back to that
    /// context. When completions arrive close together - the usual case for a
    /// burst of parallel requests - the first post wakes the worker and starts
    /// the drain, and the remaining posts are absorbed by the still-running
    /// drain instead of each paying a full pool scheduling round.
    ///
    /// The benefit is opportunistic, not guaranteed: widely spread completions
    /// still get their own rounds. The test only verifies correctness and
    /// reports how many worker wake-ups actually occurred.
    /// </summary>
    public class ContinuationMergingDemoTest
    {
        private readonly ITestOutputHelper _output;

        public ContinuationMergingDemoTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DemoParallelIoFanOut()
        {
            _output.WriteLine($"Testing {GetType().Name}.{MethodBase.GetCurrentMethod().Name}");

            const int urlCount = 8;

            PowerPool powerPool = new PowerPool(new PowerPoolOption { MaxThreads = 1 });

            // One 0 -> 1 transition per scheduling round that had to wake the
            // idle worker; continuations absorbed by a running drain cost none.
            int workerWakeUps = 0;
            powerPool.RunningWorkerCountChanged += (s, e) =>
            {
                if (e.PreviousCount == 0 && e.NowCount == 1)
                {
                    Interlocked.Increment(ref workerWakeUps);
                }
            };

            // Simulated remote endpoints with spread-out latencies, like a real
            // fan-out: some respond fast, some slow, all within a bursty window.
            List<(string Url, int LatencyMs)> requests = Enumerable
                .Range(1, urlCount)
                .Select(i => (Url: $"https://api.example.com/items/{i}", LatencyMs: 30 + (i % 4) * 10))
                .ToList();

            Dictionary<string, string> fetched = null;

            powerPool.QueueWorkItem(async () =>
            {
                // Fan out: each FetchAsync started here captures this work's
                // synchronization context, not a private one.
                Task<(string Url, string Body)>[] fetches =
                    requests.Select(r => FetchAsync(r.Url, r.LatencyMs)).ToArray();

                (string Url, string Body)[] bodies = await Task.WhenAll(fetches);

                return bodies.ToDictionary(b => b.Url, b => b.Body);
            }, out _, res => fetched = res.Result);

            powerPool.Wait();

            Assert.Equal(urlCount, fetched.Count);
            Assert.All(requests, r => Assert.Equal($"payload of {r.Url}", fetched[r.Url]));
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.WaitingWorkCount);
            Assert.Equal(0, powerPool.AsyncWorkCount);

            // Worst case without any merging: work start + one wake-up per child
            // continuation + one for the aggregation continuation = urlCount + 2.
            _output.WriteLine(
                $"{urlCount} parallel fetches, worker woken {workerWakeUps} time(s) (unmerged worst case: {urlCount + 2})");
        }

        private static async Task<(string Url, string Body)> FetchAsync(string url, int latencyMs)
        {
            // In real code this would be e.g. await httpClient.GetStringAsync(url);
            await Task.Delay(latencyMs);
            return (url, $"payload of {url}");
        }
    }
}
