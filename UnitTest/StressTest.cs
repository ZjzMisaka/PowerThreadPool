using PowerThreadPool;
using PowerThreadPool.Option;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    public class StressTest
    {
        private const int totalTasks = 10000;

        private int doneCount = 0;
        private object doneCountLock = new object();

        [Fact]
         public async Task StressTestAsync()
        {
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { });

            var tasks = Enumerable.Range(0, totalTasks).Select(i =>
                Task.Run(() =>
                {
                    string workId = powerPool.QueueWorkItem(() =>
                    {
                    }, (res) =>
                    {
                        lock (doneCountLock)
                        {
                            ++doneCount;
                        }
                    });

                    Assert.NotNull(workId);
                }
                )
            ).ToArray();

            await Task.WhenAll(tasks);

            await Task.Delay(100);

            Assert.Equal(totalTasks, powerPool.RunningWorkerCount + powerPool.WaitingWorkCount + doneCount);

            await powerPool.WaitAsync();
            powerPool.Wait();
            Assert.Equal(totalTasks, powerPool.RunningWorkerCount + powerPool.WaitingWorkCount + doneCount);
            Assert.Equal(0, powerPool.RunningWorkerCount);
            Assert.Equal(0, powerPool.WaitingWorkCount);

            Assert.True(powerPool.IdleThreadCount > 0);
        }
    }
}
