using PowerThreadPool;
using Xunit.Abstractions;

namespace UnitTest
{
    /// <summary>
    /// Regression test for the _waitAllSignal lifecycle race found on CI (Linux):
    /// a WaitAsync() registered right after a new round started could be satisfied
    /// by a leftover signal Set from the previous round's IdleSetting (the Reset in
    /// CheckPoolStart is skipped while the pool state is still IdleChecked), letting
    /// the wait return while RunningWorkerCount/AsyncWorkCount are still non-zero.
    /// </summary>
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
            PowerPool powerPool = new PowerPool();

            for (int round = 0; round < 50000; ++round)
            {
                powerPool.QueueWorkItem(async () =>
                {
                    await Task.Yield();
                    await Task.Yield();
                    await Task.Yield();
                    await Task.Yield();
                });

                await powerPool.WaitAsync();

                Assert.True(powerPool.RunningWorkerCount == 0,
                    $"round {round}: RunningWorkerCount={powerPool.RunningWorkerCount}");
                Assert.True(powerPool.WaitingWorkCount == 0,
                    $"round {round}: WaitingWorkCount={powerPool.WaitingWorkCount}");
                Assert.True(powerPool.AsyncWorkCount == 0,
                    $"round {round}: AsyncWorkCount={powerPool.AsyncWorkCount}");
            }
        }
    }
}
