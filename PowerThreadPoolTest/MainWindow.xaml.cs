using PowerThreadPool;
using PowerThreadPool.Option;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            powerPool.PoolStart += (s, e) => { OutputMsg("PoolStart"); };
            powerPool.PoolIdle += (s, e) => { OutputMsg("PoolIdle"); };
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
                        powerPool.QueueWorkItem(() => { Sleep(10000); });
                    }
                    else if (r >= 94 && r <= 94)
                    {
                        powerPool.QueueWorkItem(() => { Sleep(30000); });
                    }
                    else
                    {
                        powerPool.QueueWorkItem(() => { Sleep(random.Next(500, 1000)); });
                    }
                }
                OutputMsg("Run count: " + runCount);
                OutputMsg("WaitingWorkCount: " + powerPool.WaitingWorkCount);
                if (runCount != powerPool.WaitingWorkCount)
                {
                    break;
                }

                powerPool.Start();

                await powerPool.WaitAsync();
                OutputMsg("RunningWorkerCount: " + powerPool.RunningWorkerCount);
                OutputMsg("WaitingWorkCount: " + powerPool.WaitingWorkCount);
                OutputMsg("FailedWorkCount: " + powerPool.FailedWorkCount);
                OutputMsg("DoneCount: " + doneCount);
                OutputMsg("---------------");
                if (powerPool.RunningWorkerCount > 0 || powerPool.WaitingWorkCount > 0 || runCount != doneCount)
                {
                    break;
                }
                Sleep(random.Next(0, 1500));
            }
            OutputMsg("done");

            start.IsEnabled = true;
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
            run = false;
            powerPool.Stop();
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
                log.Text = GetLastThousandLines(log.Text);
                sv.ScrollToEnd();
            });
        }

        private string GetLastThousandLines(string input)
        {
            string[] lines = input.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            if (lines.Length <= 1000)
            {
                return input;
            }
            else
            {
                string[] lastThousandLines = new string[1000];
                Array.Copy(lines, lines.Length - 1000, lastThousandLines, 0, 1000);
                return string.Join(Environment.NewLine, lastThousandLines);
            }
        }
    }
}
