using System.Text;
using PowerThreadPool;
using Xunit.Abstractions;

namespace UnitTest
{
    public class YieldDiagnosticTest
    {
        private readonly ITestOutputHelper _output;

        public YieldDiagnosticTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public async void TestYieldDiagnostic()
        {
            const int cap = 3000;
            string[] ring = new string[cap];
            long seq = 0;

            void Record(string msg)
            {
                ring[(int)(System.Threading.Interlocked.Increment(ref seq) % cap)] =
                    $"[seq{seq} {msg}";
            }

            PowerPool.DiagnosticSink = msg => Record(msg);

            PowerPool powerPool = new PowerPool();

            powerPool.RunningWorkerCountChanged += (s, e) =>
                Record($"t{Environment.TickCount64} th{Thread.CurrentThread.ManagedThreadId}] RWC {e.PreviousCount}->{e.NowCount}");

            powerPool.PoolIdled += (s, e) =>
                Record($"t{Environment.TickCount64} th{Thread.CurrentThread.ManagedThreadId}] POOL-IDLED");

            int failRound = -1;
            int failRwc = -1;
            int failWwc = -1;
            int failAwc = -1;

            for (int round = 0; round < 50000; ++round)
            {
                powerPool.QueueWorkItem(async () =>
                {
                    await Task.Yield();
                    await Task.Yield();
                    await Task.Yield();
                    await Task.Yield();
                });

                var waitTask = powerPool.WaitAsync();
                Record($"t{Environment.TickCount64} th{Thread.CurrentThread.ManagedThreadId}] WAIT-CALLED r{round}");

                await waitTask;

                Record($"t{Environment.TickCount64} th{Thread.CurrentThread.ManagedThreadId}] WAIT-RETURNED r{round}");

                if (powerPool.RunningWorkerCount != 0 || powerPool.WaitingWorkCount != 0)
                {
                    failRound = round;
                    failRwc = powerPool.RunningWorkerCount;
                    failWwc = powerPool.WaitingWorkCount;
                    failAwc = powerPool.AsyncWorkCount;
                    break;
                }
            }

            PowerPool.DiagnosticSink = null;

            var sb = new StringBuilder();
            sb.AppendLine($"RESULT failRound={failRound} RWC={failRwc} WWC={failWwc} AWC={failAwc} totalSeq={seq}");
            long start = Math.Max(1, seq - cap + 1);
            for (long i = start; i <= seq; ++i)
            {
                sb.AppendLine(ring[(int)(i % cap)]);
            }
            string dump = sb.ToString();
            _output.WriteLine(dump);

            Assert.True(failRound == -1, dump);
        }
    }
}
