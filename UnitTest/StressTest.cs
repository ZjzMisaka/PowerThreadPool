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

                Assert.Equal(totalTasks, doneCount);
                Assert.Equal(0, failedCount);
                Assert.Equal(0, powerPool.RunningWorkerCount);
                Assert.Equal(0, powerPool.WaitingWorkCount);

                Assert.True(powerPool.IdleWorkerCount > 0);
            }
        }

        [Fact]
        public async void StressTest2()
        {
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { });
            int doneCount = 0;

            for (int i = 0; i < 100; ++i)
            {
                powerPool.QueueWorkItem(() =>
                {
                    for (int j = 0; j < 100; ++j)
                    {
                        powerPool.QueueWorkItem(() =>
                        {
                            for (int k = 0; k < 100; ++k)
                            {
                                powerPool.QueueWorkItem(() =>
                                {
                                }, (res) =>
                                {
                                    for (int j = 0; j < 5; ++j)
                                    {
                                        powerPool.QueueWorkItem(() =>
                                        {

                                        }, (res) =>
                                        {
                                            Interlocked.Increment(ref doneCount);
                                        });
                                    }
                                    Interlocked.Increment(ref doneCount);
                                });
                            }
                        }, (res) =>
                        {
                            Interlocked.Increment(ref doneCount);
                        });
                    }
                }, (res) =>
                {
                    Interlocked.Increment(ref doneCount);
                });
            }

            await powerPool.WaitAsync();

            Assert.Equal(6010100, doneCount);
        }
    }
}
