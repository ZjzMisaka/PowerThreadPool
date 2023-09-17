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
        private const int totalTasks = 100000;

        private object lockObj = new object();

        [Fact]
         public async Task StressTest1()
        {
            for (int i = 0; i < 10; ++i)
            {
                int doneCount = 0;
                PowerPool powerPool = new PowerPool(new PowerPoolOption() { });

                Task[] tasks = Enumerable.Range(0, totalTasks).Select(i =>
                    Task.Run(() =>
                    {
                        string workId = powerPool.QueueWorkItem(() =>
                        {
                        }, (res) =>
                        {
                            lock (lockObj)
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
                Assert.Equal(totalTasks, powerPool.RunningWorkerCount + powerPool.WaitingWorkCount + doneCount);
                Assert.Equal(0, powerPool.RunningWorkerCount);
                Assert.Equal(0, powerPool.WaitingWorkCount);

                Assert.True(powerPool.IdleThreadCount > 0);
            }
        }

        [Fact]
        public void StressTest2()
        {
            PowerPool powerPool = new PowerPool(new PowerPoolOption() { });
            int startCount = 0;
            int idleCount = 0;

            int doneCount = 0;

            powerPool.ThreadPoolStart += (s, e) => 
            { 
                lock (lockObj) 
                { 
                    ++startCount; 
                    doneCount = 0; 
                } 
            };
            powerPool.ThreadPoolIdle += (s, e) => 
            { 
                lock (lockObj) 
                {
                    ++idleCount;
                    Assert.Equal(6010100, doneCount);
                } 
            };
            
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
                                            lock (lockObj)
                                            {
                                                ++doneCount;
                                            }
                                        });
                                    }
                                    lock (lockObj)
                                    {
                                        ++doneCount;
                                    }
                                });
                            }
                        }, (res) =>
                        {
                            lock (lockObj)
                            {
                                ++doneCount;
                            }
                        });
                    }
                }, (res) =>
                {
                    lock (lockObj) 
                    {
                        ++doneCount;
                    }
                });
            }

            powerPool.Wait();

            Assert.Equal(1, startCount);
            Assert.Equal(1, idleCount);
        }
    }
}
