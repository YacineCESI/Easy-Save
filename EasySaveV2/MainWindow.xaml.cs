using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Globalization;
using EasySave.Model;
using EasySave.Model.Enums;
using EasySave.ViewModel;
using EasySaveV2.Converters;
using System.Windows.Media;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasySaveV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private DispatcherTimer _progressUpdateTimer;
        private readonly Dictionary<string, CancellationTokenSource> _jobMonitoringTokens = new();

        public MainWindow()
        {
            InitializeComponent();

            var configManager = new ConfigManager();
            var languageManager = new LanguageManager();
            var businessSoftwareManager = new BusinessSoftwareManager();
            var backupManager = new BackupManager();

            _viewModel = new MainViewModel(
                backupManager,
                languageManager,
                businessSoftwareManager,
                configManager);

            DataContext = _viewModel;

            JobsListBox.SelectionChanged += JobsListBox_SelectionChanged;

            _viewModel.LanguageChanged += ViewModel_LanguageChanged;

            _viewModel.BlockedProcessesDetected += ViewModel_BlockedProcessesDetected;

            this.Title = _viewModel.GetLocalizedString("appTitle");
        }

        private void ViewModel_LanguageChanged(object sender, EventArgs e)
        {
            // Force refresh the entire UI when language changes
            // This is necessary because some bindings might not update automatically

            // Update window title directly
            this.Title = _viewModel.GetLocalizedString("appTitle");

            // Force all bindings to update
            RefreshAllBindings(this);

            // Show feedback to the user
            string languageChanged = _viewModel.GetLocalizedString("languageChanged");
            string appTitle = _viewModel.GetLocalizedString("appTitle");
            MessageBox.Show(languageChanged, appTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshAllBindings(DependencyObject obj)
        {
            // Process the current object's bindings
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(obj))
            {
                DependencyProperty dependencyProperty = DependencyPropertyDescriptor.FromProperty(property)?.DependencyProperty;
                if (dependencyProperty != null)
                {
                    BindingExpression binding = BindingOperations.GetBindingExpression(obj, dependencyProperty);
                    binding?.UpdateTarget();
                }
            }

            // Process children
            int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                RefreshAllBindings(child);
            }
        }

        private void JobsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // REMOVE any code like:
            // _viewModel.ExecuteSelectedJob();
            // or
            // _viewModel.RunJobCommand.Execute(_viewModel.SelectedJob);
            // or
            // _viewModel.StartJobStatusMonitoring(_viewModel.SelectedJob.Name);
        }

        private void CreateJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the JobCreation window
                var jobCreationWindow = new JobCreation(_viewModel);
                jobCreationWindow.Owner = this;
                bool? result = jobCreationWindow.ShowDialog();

                // Refresh the job list if a job was created
                if (result == true)
                {
                    _viewModel.RefreshBackupJobs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while opening job creation form: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RunAllJobs_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAllJobs();
        }

        private void RunJob_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSelectedJob();
        }

        /// <summary>
        /// Executes the selected job and sets up a timer to update the progress bar and job data in real-time
        /// </summary>
        private void ExecuteSelectedJob()
        {
            StopProgressUpdates();

            if (_viewModel.SelectedJob == null)
                return;

            string jobName = _viewModel.SelectedJob.Name;

            _viewModel.ExecuteSelectedJob();

            // Start monitoring the job's progress in the background
            StartJobStatusMonitoring(jobName);
        }

        /// <summary>
        /// Executes all backup jobs and sets up timers to update their progress and data
        /// </summary>
        private void ExecuteAllJobs()
        {
            StopProgressUpdates();

            _viewModel.ExecuteAllJobs();

            // Start monitoring for all jobs
            foreach (var jobVM in _viewModel.BackupJobs)
            {
                StartJobStatusMonitoring(jobVM.Name);
            }
        }

        /// <summary>
        /// Monitors the status and progress of a job and updates the corresponding ViewModel.
        /// </summary>
        private void StartJobStatusMonitoring(string jobName)
        {
            // Cancel any previous monitoring for this job
            if (_jobMonitoringTokens.TryGetValue(jobName, out var oldCts))
            {
                oldCts.Cancel();
                _jobMonitoringTokens.Remove(jobName);
            }

            var cts = new CancellationTokenSource();
            _jobMonitoringTokens[jobName] = cts;
            var token = cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var job = _viewModel.BackupManager.GetBackupJob(jobName);
                    if (job == null)
                        break;

                    float progress = job.Progress;
                    var state = job.State;
                    var lastRunTime = job.LastRunTime;

                    // Update the ViewModel for the job with the matching name on the UI thread
                    Dispatcher.Invoke(() =>
                    {
                        var jobVm = _viewModel.BackupJobs.FirstOrDefault(j => j.Name == jobName);
                        if (jobVm != null)
                        {
                            if (jobVm.Progress != progress)
                                jobVm.Progress = progress;
                            if (jobVm.State != state)
                                jobVm.State = state;
                            if (jobVm.LastRunTime != lastRunTime)
                                jobVm.LastRunTime = lastRunTime;
                        }
                    });

                    // Stop monitoring if job is no longer running
                    if (state != JobState.RUNNING)
                        break;

                    await Task.Delay(500, token);
                }

                // Remove the token when done
                _jobMonitoringTokens.Remove(jobName);
            }, token);
        }

        /// <summary>
        /// Stops all progress update timers and cancels all job monitoring tasks.
        /// </summary>
        private void StopProgressUpdates()
        {
            if (_progressUpdateTimer != null)
            {
                _progressUpdateTimer.Stop();
                _progressUpdateTimer = null;
            }

            // Cancel all job monitoring tasks
            foreach (var cts in _jobMonitoringTokens.Values)
            {
                cts.Cancel();
            }
            _jobMonitoringTokens.Clear();
        }

        /// <summary>
        /// Show a popup if a backup job is blocked due to running processes
        /// </summary>
        private void ViewModel_BlockedProcessesDetected(object sender, List<string> runningBlockedProcesses)
        {
            if (runningBlockedProcesses != null && runningBlockedProcesses.Count > 0)
            {
                string processes = string.Join(", ", runningBlockedProcesses);
                string message = $"Cannot run backup job(s) because the following blocked processes are running: {processes}. " +
                                 "Please close these applications before running the backup.";
                MessageBox.Show(message, "Blocked Processes Running", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Clean up when the window is closed
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            if (_viewModel != null)
            {
                _viewModel.LanguageChanged -= ViewModel_LanguageChanged;
            }

            StopProgressUpdates();
            base.OnClosed(e);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Ask the user to confirm before exiting
            MessageBoxResult result = MessageBox.Show(
                _viewModel.GetLocalizedString("confirmExit"),
                _viewModel.GetLocalizedString("appTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Stop any running jobs before closing
                _viewModel.StopAllJobs();

                // Close the application
                Application.Current.Shutdown();
            }
        }
    }
}
