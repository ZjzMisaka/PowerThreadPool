using PowerThreadPool;
using PowerThreadPool.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;

namespace PowerThreadPoolTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PowerPool powerPool = new PowerPool();
        Random random = new Random();
        bool run = false;
        int doneCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 1000 },
                StartSuspended = true,
                DefaultCallback = (res) =>
                {
                    Interlocked.Increment(ref doneCount);
                },
            };
            powerPool.PoolStarted += (s, e) => { OutputMsg("PoolStart"); };
            powerPool.PoolIdled += (s, e) => { OutputMsg("PoolIdle"); };
        }

        private async void start_Click(object sender, RoutedEventArgs e)
        {
            start.IsEnabled = false;

            run = true;
            while (run)
            {
                int runCount = random.Next(10, 200);
                doneCount = 0;
                for (int i = 0; i < runCount; ++i)
                {
                    int r =random.Next(0, 101);
                    if (r == 100)
                    {
                        powerPool.QueueWorkItem(() => { throw new Exception(); });
                    }
                    else if(r >= 95 && r <= 99)
                    {
                        powerPool.QueueWorkItem(() => 
                        { 
                            Sleep(10000);
                            int r1 = random.Next(0, 101);
                            if (r1 >= 100 && r1 <= 100)
                            {
                                Thread.Sleep(1);
                            }
                        });
                    }
                    else if (r >= 94 && r <= 94)
                    {
                        powerPool.QueueWorkItem(() => 
                        { 
                            Sleep(30000);
                            int r1 = random.Next(0, 101);
                            if (r1 >= 100 && r1 <= 100)
                            {
                                Thread.Sleep(1);
                            }
                        });
                    }
                    else
                    {
                        powerPool.QueueWorkItem(() => 
                        { 
                            Sleep(random.Next(500, 1000));
                            int r1 = random.Next(0, 101);
                            if (r1 >= 100 && r1 <= 100)
                            {
                                Thread.Sleep(1);
                            }
                        });
                    }
                }
                OutputMsg("Run count: " + runCount);
                OutputMsg("WaitingWorkCount: " + powerPool.WaitingWorkCount);
                if (runCount != powerPool.WaitingWorkCount)
                {
                    if (runCount != powerPool.WaitingWorkCount)
                    {
                        OutputMsg("error0: runCount != powerPool.WaitingWorkCount");
                    }
                    break;
                }

                powerPool.Start();
                OutputMsg("Running... AliveWorkerCount: " + powerPool.AliveWorkerCount + " | RunningWorkerCount: " + powerPool.RunningWorkerCount + " | IdleWorkerCount: " + powerPool.IdleWorkerCount);

                int r1 = random.Next(0, 101);
                if (r1 >= 81 && r1 <= 100)
                {
                    OutputMsg("Stopping...");
                    await powerPool.StopAsync();
                    await powerPool.WaitAsync();
                    OutputMsg("AliveWorkerCount: " + powerPool.AliveWorkerCount + " | RunningWorkerCount: " + powerPool.RunningWorkerCount);
                    OutputMsg("IdleWorkerCount: " + powerPool.IdleWorkerCount);
                    OutputMsg("WaitingWorkCount: " + powerPool.WaitingWorkCount);
                    OutputMsg("FailedWorkCount: " + powerPool.FailedWorkCount);
                    OutputMsg("DoneCount: " + doneCount);
                    if (powerPool.RunningWorkerCount > 0 || powerPool.WaitingWorkCount > 0)
                    {
                        if (powerPool.RunningWorkerCount > 0)
                        {
                            OutputMsg("error1: powerPool.RunningWorkerCount > 0" + " - " + powerPool.RunningWorkerCount);
                        }
                        if (powerPool.WaitingWorkCount > 0)
                        {
                            OutputMsg("error1: powerPool.WaitingWorkCount > 0" + " - " + powerPool.WaitingWorkCount);
                        }
                        break;
                    }
                }
                else if (r1 >= 61 && r1 <= 80)
                {
                    OutputMsg("Force Stopping...");
                    await powerPool.StopAsync(true);
                    await powerPool.WaitAsync();
                    OutputMsg("AliveWorkerCount: " + powerPool.AliveWorkerCount + " | RunningWorkerCount: " + powerPool.RunningWorkerCount);
                    OutputMsg("IdleWorkerCount: " + powerPool.IdleWorkerCount);
                    OutputMsg("WaitingWorkCount: " + powerPool.WaitingWorkCount);
                    OutputMsg("FailedWorkCount: " + powerPool.FailedWorkCount);
                    OutputMsg("DoneCount: " + doneCount);
                    if (powerPool.RunningWorkerCount > 0 || powerPool.WaitingWorkCount > 0)
                    {
                        if (powerPool.RunningWorkerCount > 0)
                        {
                            OutputMsg("error2: powerPool.RunningWorkerCount > 0" + " - " + powerPool.RunningWorkerCount);
                        }
                        if (powerPool.WaitingWorkCount > 0)
                        {
                            OutputMsg("error2: powerPool.WaitingWorkCount > 0" + " - " + powerPool.WaitingWorkCount);
                        }
                        break;
                    }
                }
                else
                {
                    OutputMsg("Waiting...");
                    await powerPool.WaitAsync();
                    OutputMsg("AliveWorkerCount: " + powerPool.AliveWorkerCount + " | RunningWorkerCount: " + powerPool.RunningWorkerCount);
                    OutputMsg("IdleWorkerCount: " + powerPool.IdleWorkerCount);
                    OutputMsg("WaitingWorkCount: " + powerPool.WaitingWorkCount);
                    OutputMsg("FailedWorkCount: " + powerPool.FailedWorkCount);
                    OutputMsg("DoneCount: " + doneCount);
                    if (powerPool.RunningWorkerCount > 0 || powerPool.WaitingWorkCount > 0 || runCount != doneCount)
                    {
                        if (powerPool.RunningWorkerCount > 0)
                        {
                            OutputMsg("error3: powerPool.RunningWorkerCount > 0" + " - " + powerPool.RunningWorkerCount);
                        }
                        if (powerPool.WaitingWorkCount > 0)
                        {
                            OutputMsg("error3: powerPool.WaitingWorkCount > 0" + " - " + powerPool.WaitingWorkCount);
                        }
                        if (runCount != doneCount)
                        {
                            OutputMsg("error3: runCount != doneCount");
                        }
                        break;
                    }
                }
                OutputMsg("---------------");
                int r2 = random.Next(0, 101);
                if (r1 >= 81 && r1 <= 100)
                {
                }
                else
                {
                    Sleep(random.Next(0, 1500));
                }
            }
            OutputMsg("done");

            start.IsEnabled = true;
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
            run = false;
            powerPool.Stop(true);
        }





        private void Sleep(int ms)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < ms)
            {
                powerPool.StopIfRequested();
            }

            stopwatch.Stop();
        }

        private void OutputMsg(string msg)
        {
            this.Dispatcher.Invoke(() =>
            {
                log.Text += msg + "\n";
                log.Text = GetLastHundredLines(log.Text);
                sv.ScrollToEnd();
            });
        }

        private string GetLastHundredLines(string str)
        {
            string[] lines = str.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            string[] lastHundredLines = lines.Skip(Math.Max(0, lines.Length - 100)).ToArray();

            string result = string.Join(Environment.NewLine, lastHundredLines);

            return result;
        }
    }
}
