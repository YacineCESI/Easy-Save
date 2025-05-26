using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RemoteConsoleClient _client;
        public ObservableCollection<RemoteConsoleClient.JobStatus> Jobs { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _client = new RemoteConsoleClient();
            _client.JobStatusReceived += jobs =>
            {
                Dispatcher.Invoke(() =>
                {
                    Jobs.Clear();
                    foreach (var job in jobs)
                        Jobs.Add(job);
                });
            };
            try
            {
                // Connect to local address
                await _client.ConnectAsync("127.0.0.1", 9000);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to server: " + ex.Message);
            }
        }

        private RemoteConsoleClient.JobStatus SelectedJob => (RemoteConsoleClient.JobStatus)JobsGrid.SelectedItem;

        private async void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedJob != null)
                await _client.SendCommandAsync("pause", SelectedJob.Name);
        }

        private async void Resume_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedJob != null)
                await _client.SendCommandAsync("resume", SelectedJob.Name);
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedJob != null)
                await _client.SendCommandAsync("stop", SelectedJob.Name);
        }

        private async void RunSelected_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedJob != null)
                await _client.RunJobAsync(SelectedJob.Name);
        }

        private async void RunAll_Click(object sender, RoutedEventArgs e)
        {
            await _client.RunAllJobsAsync();
        }
    }
}