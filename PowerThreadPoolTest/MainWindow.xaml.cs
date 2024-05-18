using PowerThreadPool;
using PowerThreadPool.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                DefaultCallback = (res) =>
                {
                    Interlocked.Increment(ref doneCount);
                },
            };
        }

        private async void start_Click(object sender, RoutedEventArgs e)
        {
            start.IsEnabled = false;

            run = true;
            int totalTasks = 1000;
            for (int i = 0; i < 10000; ++i)
            {
                Task[] tasks = Enumerable.Range(0, totalTasks).Select(i =>
                    Task.Run(() =>
                    {
                        powerPool.QueueWorkItem(() =>
                        {
                            Interlocked.Increment(ref doneCount);
                        });
                    })
                ).ToArray();

                await Task.WhenAll(tasks);

                await powerPool.WaitAsync();
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
