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
        string t4Guid = "";
        string t2Guid = "";
        System.Timers.Timer timer = new System.Timers.Timer(100);
        string msg = "";
        public MainWindow()
        {
            InitializeComponent();

            timer.Elapsed += (s, e) =>
            {
                OutputCount();
            };
            timer.Start();

            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 8,
                DefaultCallback = (res) =>
                {
                    OutputMsg("DefaultCallback");
                },
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 3000 }
            };

            powerPool.ThreadPoolStart += (s, e) =>
            {
                log.Text = "";
                OutputMsg("ThreadPoolStart");
            };
            powerPool.ThreadPoolIdle += (s, e) =>
            {
                OutputMsg("ThreadPoolIdle");
            };
            powerPool.ThreadStart += (s, e) =>
            {
                OutputMsg("ThreadStart");
            };
            powerPool.ThreadEnd += (s, e) =>
            {
                OutputMsg("ThreadEnd");
            };
            powerPool.ThreadTimeout += (s, e) =>
            {
                OutputMsg("Thread" + e.ID + ": Timeout");
            };
        }

        private void Sleep(int ms)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < ms)
            {
            }

            stopwatch.Stop();
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
            powerPool.Stop();
        }

        private void forceStop_Click(object sender, RoutedEventArgs e)
        {
            powerPool.Stop(true);
        }

        private void stopThread2_Click(object sender, RoutedEventArgs e)
        {
            if (powerPool.Stop(t2Guid))
            {
                OutputMsg("stopThread2 succees");
            }
            else 
            {
                OutputMsg("stopThread2 failed");
            }
        }

        private async void wait_Click(object sender, RoutedEventArgs e)
        {
            wait.IsEnabled = false;
            await powerPool.WaitAsync();
            OutputMsg("ALL Thread End");
            wait.IsEnabled = true;
        }

        private void pause_Click(object sender, RoutedEventArgs e)
        {
            powerPool.Pause();
        }

        private void resume_Click(object sender, RoutedEventArgs e)
        {
            powerPool.Resume(true);
        }

        private void pauseThread2_Click(object sender, RoutedEventArgs e)
        {
            powerPool.Pause(t2Guid);
        }

        private void resumeThread2_Click(object sender, RoutedEventArgs e)
        {
            powerPool.Resume(t2Guid);
        }

        private void cancelThread4_Click(object sender, RoutedEventArgs e)
        {
            if (powerPool.Cancel(t4Guid))
            {
                OutputMsg("Cancel succeed");
            }
            else
            {
                OutputMsg("Cancel failed");
            }
        }

        private void cancelAllThread_Click(object sender, RoutedEventArgs e)
        {
            powerPool.Cancel();
        }

        private void OutputMsg(string msg)
        {
            this.msg += msg + "\n";
        }

        private void OutputCount()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (msg != "")
                {
                    log.Text += msg;
                    msg = "";
                    sv.ScrollToEnd();
                }

                string countTxt = "waiting: " + powerPool.WaitingWorkCount + "\n" + "running: " + powerPool.RunningWorkerCount +"\n" + "Idle: " + powerPool.IdleThreadCount;
                count.Text = countTxt;
            });
        }







        private void start_Click(object sender, RoutedEventArgs e)
        {
            powerPool.QueueWorkItem(() =>
            {
                OutputMsg("Thread0: START");
                Sleep(10000);
                powerPool.PauseIfRequested();
                powerPool.StopIfRequested();
                OutputMsg("Thread0: END");

                return new Exception("11223344");
            });

            powerPool.QueueWorkItem(() => 
            {
                for (int i = 0; i < 20; ++i)
                {
                    powerPool.PauseIfRequested();
                    powerPool.StopIfRequested();
                    OutputMsg("Thread1: " + i.ToString());
                    Sleep(1000);
                }
                OutputMsg("Thread1: END");
                return true;
            }, new ThreadOption()
            {
                Callback = (res) =>
                {
                    OutputMsg("Thread1: Callback");
                },
                Timeout = new TimeoutOption() { Duration = 2000 }
            });

            t2Guid = powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 20; ++i)
                {
                    powerPool.PauseIfRequested();
                    powerPool.StopIfRequested();
                    OutputMsg("Thread2: " + i.ToString());
                    Sleep(700);
                }
                OutputMsg("Thread2: END");
            });

            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 20; ++i)
                {
                    powerPool.PauseIfRequested();
                    powerPool.StopIfRequested();
                    OutputMsg("Thread3: " + i.ToString());
                    Sleep(500);
                }
                OutputMsg("Thread3: END");
                return new ThreadPoolOption();
            }, (res) =>
            {
                OutputMsg("Thread3: Callback");
            });

            t4Guid = powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 20; ++i)
                {
                    powerPool.PauseIfRequested();
                    powerPool.StopIfRequested();
                    OutputMsg("Thread4: " + i.ToString());
                    Sleep(500);
                }
                OutputMsg("Thread4: END");
            }, new ThreadOption()
            {
                Callback = (res) =>
                {
                    OutputMsg("Thread4: Callback");
                },
                Priority = 10
            });

            powerPool.QueueWorkItem<int, int, Random>(T5Func, 10, 10, T5Callback);
            powerPool.QueueWorkItem<int, int>(T6Action, 10, 10);
        }

        private Random T5Func(int x, int y)
        {
            OutputMsg("T5Func: x + y :" + (x + y).ToString());
            return new Random();
        }

        private void T5Callback(ExecuteResult<Random> res)
        {
            OutputMsg("Random :" + res.Result.Next());
        }

        private void T6Action(int x, int y)
        {
            OutputMsg("T6Action: x + y :" + (x + y).ToString());
        }
    }
}
