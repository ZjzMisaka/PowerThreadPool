using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using PowerThreadPool;
using PowerThreadPool.Options;

namespace PowerThreadPoolTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PowerPool _powerPool = new PowerPool();
        Random _random = new Random();
        bool _run = false;
        int _doneCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            _powerPool.PowerPoolOption = new PowerPoolOption()
            {
                MaxThreads = 8,
                DestroyThreadOption = new DestroyThreadOption() { MinThreads = 4, KeepAliveTime = 1000 },
                StartSuspended = true,
                DefaultCallback = (res) =>
                {
                    Interlocked.Increment(ref _doneCount);
                },
            };
            _powerPool.PoolStarted += (s, e) => { OutputMsg("PoolStart"); };
            _powerPool.PoolIdled += (s, e) => { OutputMsg("PoolIdle"); };
        }

        private async void start_Click(object sender, RoutedEventArgs e)
        {
            start.IsEnabled = false;

            _run = true;
            while (_run)
            {
                int runCount = _random.Next(10, 200);
                _doneCount = 0;
                for (int i = 0; i < runCount; ++i)
                {
                    int r = _random.Next(0, 101);
                    if (r == 100)
                    {
                        _powerPool.QueueWorkItem(() => { throw new Exception(); });
                    }
                    else if (r >= 95 && r <= 99)
                    {
                        _powerPool.QueueWorkItem(() =>
                        {
                            Sleep(10000);
                            int r1 = _random.Next(0, 101);
                            if (r1 >= 100 && r1 <= 100)
                            {
                                Thread.Sleep(1);
                            }
                        });
                    }
                    else if (r >= 94 && r <= 94)
                    {
                        _powerPool.QueueWorkItem(() =>
                        {
                            Sleep(30000);
                            int r1 = _random.Next(0, 101);
                            if (r1 >= 100 && r1 <= 100)
                            {
                                Thread.Sleep(1);
                            }
                        });
                    }
                    else
                    {
                        _powerPool.QueueWorkItem(() =>
                        {
                            Sleep(_random.Next(500, 1000));
                            int r1 = _random.Next(0, 101);
                            if (r1 >= 100 && r1 <= 100)
                            {
                                Thread.Sleep(1);
                            }
                        });
                    }
                }
                OutputMsg("Run count: " + runCount);
                OutputMsg("WaitingWorkCount: " + _powerPool.WaitingWorkCount);
                if (runCount != _powerPool.WaitingWorkCount)
                {
                    if (runCount != _powerPool.WaitingWorkCount)
                    {
                        OutputMsg("error0: runCount != powerPool.WaitingWorkCount");
                    }
                    break;
                }

                _powerPool.Start();
                OutputMsg("Running... AliveWorkerCount: " + _powerPool.AliveWorkerCount + " | RunningWorkerCount: " + _powerPool.RunningWorkerCount + " | IdleWorkerCount: " + _powerPool.IdleWorkerCount);

                int r1 = _random.Next(0, 101);
                if (r1 >= 81 && r1 <= 100)
                {
                    OutputMsg("Stopping...");
                    _powerPool.Stop();
                    await _powerPool.WaitAsync();
                    OutputMsg("AliveWorkerCount: " + _powerPool.AliveWorkerCount + " | RunningWorkerCount: " + _powerPool.RunningWorkerCount);
                    OutputMsg("IdleWorkerCount: " + _powerPool.IdleWorkerCount);
                    OutputMsg("WaitingWorkCount: " + _powerPool.WaitingWorkCount);
                    OutputMsg("FailedWorkCount: " + _powerPool.FailedWorkCount);
                    OutputMsg("DoneCount: " + _doneCount);
                    if (_powerPool.RunningWorkerCount > 0 || _powerPool.WaitingWorkCount > 0)
                    {
                        if (_powerPool.RunningWorkerCount > 0)
                        {
                            OutputMsg("error1: powerPool.RunningWorkerCount > 0" + " - " + _powerPool.RunningWorkerCount);
                        }
                        if (_powerPool.WaitingWorkCount > 0)
                        {
                            OutputMsg("error1: powerPool.WaitingWorkCount > 0" + " - " + _powerPool.WaitingWorkCount);
                        }
                        break;
                    }
                }
                else if (r1 >= 61 && r1 <= 80)
                {
                    OutputMsg("Force Stopping...");
                    _powerPool.ForceStop();
                    await _powerPool.WaitAsync();
                    OutputMsg("AliveWorkerCount: " + _powerPool.AliveWorkerCount + " | RunningWorkerCount: " + _powerPool.RunningWorkerCount);
                    OutputMsg("IdleWorkerCount: " + _powerPool.IdleWorkerCount);
                    OutputMsg("WaitingWorkCount: " + _powerPool.WaitingWorkCount);
                    OutputMsg("FailedWorkCount: " + _powerPool.FailedWorkCount);
                    OutputMsg("DoneCount: " + _doneCount);
                    if (_powerPool.RunningWorkerCount > 0 || _powerPool.WaitingWorkCount > 0)
                    {
                        if (_powerPool.RunningWorkerCount > 0)
                        {
                            OutputMsg("error2: powerPool.RunningWorkerCount > 0" + " - " + _powerPool.RunningWorkerCount);
                        }
                        if (_powerPool.WaitingWorkCount > 0)
                        {
                            OutputMsg("error2: powerPool.WaitingWorkCount > 0" + " - " + _powerPool.WaitingWorkCount);
                        }
                        break;
                    }
                }
                else
                {
                    OutputMsg("Waiting...");
                    await _powerPool.WaitAsync();
                    OutputMsg("AliveWorkerCount: " + _powerPool.AliveWorkerCount + " | RunningWorkerCount: " + _powerPool.RunningWorkerCount);
                    OutputMsg("IdleWorkerCount: " + _powerPool.IdleWorkerCount);
                    OutputMsg("WaitingWorkCount: " + _powerPool.WaitingWorkCount);
                    OutputMsg("FailedWorkCount: " + _powerPool.FailedWorkCount);
                    OutputMsg("DoneCount: " + _doneCount);
                    if (_powerPool.RunningWorkerCount > 0 || _powerPool.WaitingWorkCount > 0 || runCount != _doneCount)
                    {
                        if (_powerPool.RunningWorkerCount > 0)
                        {
                            OutputMsg("error3: powerPool.RunningWorkerCount > 0" + " - " + _powerPool.RunningWorkerCount);
                        }
                        if (_powerPool.WaitingWorkCount > 0)
                        {
                            OutputMsg("error3: powerPool.WaitingWorkCount > 0" + " - " + _powerPool.WaitingWorkCount);
                        }
                        if (runCount != _doneCount)
                        {
                            OutputMsg("error3: runCount != doneCount");
                        }
                        break;
                    }
                }
                OutputMsg("---------------");
                int r2 = _random.Next(0, 101);
                if (r1 >= 81 && r1 <= 100)
                {
                }
                else
                {
                    Sleep(_random.Next(0, 1500));
                }
            }
            OutputMsg("done");

            start.IsEnabled = true;
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
            _run = false;
            _powerPool.ForceStop();
        }

        private void Sleep(int ms)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < ms)
            {
                _powerPool.StopIfRequested();
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
