using PowerThreadPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
        PowerPool powerPool = new PowerPool(new ThreadPoolOption() { MaxThreads = 3 });
        public MainWindow()
        {
            InitializeComponent();
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
            powerPool.StopAllThread();
        }

        private void Output(string msg)
        {
            this.Dispatcher.Invoke(() =>
            {
                tb.Text += msg + "\n";
                sv.ScrollToEnd();
            });
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            powerPool.QueueWorkItem(() => 
            {
                for (int i = 0; i < 20; ++i)
                {
                    Output("Thread1: " + i.ToString());
                    Thread.Sleep(1000);
                }
                Output("Thread1: END");
                return true;
            }, (res) => 
            {
                // Output("Thread1: End");
            });

            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 20; ++i)
                {
                    Output("Thread2: " + i.ToString());
                    Thread.Sleep(700);
                }
                Output("Thread2: END");
            }, (res) =>
            {
                // Output("Thread2: End");
            });

            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 20; ++i)
                {
                    Output("Thread3: " + i.ToString());
                    Thread.Sleep(500);
                }
                Output("Thread3: END");
                return new ThreadPoolOption();
            }, (res) =>
            {
                // Output("Thread3: End");
            });

            powerPool.QueueWorkItem(() =>
            {
                for (int i = 0; i < 20; ++i)
                {
                    Output("Thread4: " + i.ToString());
                    Thread.Sleep(500);
                }
                Output("Thread4: END");
            }, (res) =>
            {
                // Output("Thread4: End");
            });
        }
    }
}
