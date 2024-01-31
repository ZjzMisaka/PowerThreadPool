using PowerThreadPool;
using PowerThreadPool.Option;

namespace UnitTest
{
    public class StressTest
    {
        private const int totalTasks = 100000;

        [Fact]
        public async Task StressTest1()
        {
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { DestroyThreadOption = new DestroyThreadOption() });

            for (int i = 0; i < 10; ++i)
            {
                int doneCount = 0;
                int failedCount = 0;
                
                Task[] tasks = Enumerable.Range(0, totalTasks).Select(i =>
                    Task.Run(() =>
                    {
                        string workId = powerPool.QueueWorkItem(() =>
                        {
                        }, (res) =>
                        {
                            if (res.Status == Status.Failed)
                            {
                                Interlocked.Increment(ref failedCount);
                            }
                            Interlocked.Increment(ref doneCount);
                        });
                        Assert.NotNull(workId);
                    })
                ).ToArray();

                await Task.WhenAll(tasks);

                await powerPool.WaitAsync();

                // TODO REMOVE
                Thread.Sleep(3000);
                await powerPool.WaitAsync();

                Assert.Equal(totalTasks, doneCount);
                Assert.Equal(0, failedCount);
                Assert.Equal(0, powerPool.RunningWorkerCount);
                Assert.Equal(0, powerPool.WaitingWorkCount);

                Assert.True(powerPool.IdleWorkerCount > 0);
            }
        }
    }
}
