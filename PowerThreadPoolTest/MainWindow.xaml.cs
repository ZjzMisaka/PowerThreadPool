using PowerThreadPool;
using PowerThreadPool.Option;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public MainWindow()
        {
            InitializeComponent();

            powerPool.ThreadPoolOption = new ThreadPoolOption()
            {
                MaxThreads = 3,
                DefaultCallback = (res) =>
                {
                    OutputMsg("DefaultCallback");
                }
            };

            powerPool.ThreadPoolStart += (s, e) =>
            {
                OutputMsg("ThreadPoolStart");
            };
            powerPool.Idle += (s, e) =>
            {
                OutputMsg("Idle");
            };
            powerPool.ThreadStart += (s, e) =>
            {
                OutputMsg("ThreadStart");
            };
            powerPool.ThreadEnd += (s, e) =>
            {
                OutputMsg("ThreadEnd");
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

        private void OutputMsg(string msg)
        {
            this.Dispatcher.Invoke(() =>
            {
                log.Text += msg + "\n";
                sv.ScrollToEnd();
            });
            OutputCount();
        }

        private void OutputCount()
        {
            this.Dispatcher.Invoke(() =>
            {
                string countTxt = "waiting: " + powerPool.WaitingThreadCount + "\n" + "running: " + powerPool.RunningThreadCount;
                count.Text = countTxt;
            });
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            log.Text = "";

            powerPool.QueueWorkItem(() =>
            {
                OutputMsg("Thread0: START");
                Sleep(10000);
                powerPool.PauseIfRequested();
                powerPool.StopIfRequested();
                OutputMsg("Thread0: END");
            }, (res) =>
            {
                // OutputMsg("Thread4: End");
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
            }, (res) => 
            {
                OutputMsg("Thread1: Callback");
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
            }, (res) =>
            {
                OutputMsg("Thread4: Callback");
            });

            powerPool.QueueWorkItem<int, int, int>(T5Func, 10, 10);
            powerPool.QueueWorkItem<int, int>(T6Action, 10, 10);
        }

        private int T5Func(int x, int y)
        {
            OutputMsg("T5Func: x + y :" + (x + y).ToString());
            return x + y;
        }

        private void T6Action(int x, int y)
        {
            OutputMsg("T6Action: x + y :" + (x + y).ToString());
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
            powerPool.Resume();
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
    }
}
