// MainWindow.xaml.cs
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

namespace EasySaveV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private DispatcherTimer _progressUpdateTimer;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize required managers for the application
            var configManager = new ConfigManager();
            var languageManager = new LanguageManager();
            var businessSoftwareManager = new BusinessSoftwareManager();
            var backupManager = new BackupManager();

            // Create the view model
            _viewModel = new MainViewModel(
                backupManager,
                languageManager,
                businessSoftwareManager,
                configManager);

            // Set the DataContext for data binding
            DataContext = _viewModel;

            // Subscribe to the SelectionChanged event of the JobsListBox
            JobsListBox.SelectionChanged += JobsListBox_SelectionChanged;

            // Subscribe to the LanguageChanged event
            _viewModel.LanguageChanged += ViewModel_LanguageChanged;

            // Subscribe to blocked process event
            _viewModel.BlockedProcessesDetected += ViewModel_BlockedProcessesDetected;

            // Set initial window title
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
            // If a job is selected, automatically run it
            if (_viewModel.SelectedJob != null)
            {
                // Execute the RunJobCommand to start the selected job
                _viewModel.RunJobCommand.Execute(_viewModel.SelectedJob);
            }
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
            // Use the enhanced ViewModel logic, which will raise the event if blocked
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
            // Stop any existing timer
            StopProgressUpdates();

            if (_viewModel.SelectedJob == null)
                return;

            string jobName = _viewModel.SelectedJob.Name;

            // Execute the job
            _viewModel.ExecuteSelectedJob();

            // Set up the timer to update the UI
            _progressUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            _progressUpdateTimer.Tick += (s, e) =>
            {
                try
                {
                    // Get the current job from the backup manager to ensure we have the latest progress
                    BackupJob job = _viewModel.BackupManager.GetBackupJob(jobName);

                    if (job != null)
                    {
                        // Update the view model's selected job with the latest data
                        _viewModel.SelectedJob.Progress = job.Progress;
                        _viewModel.SelectedJob.State = job.State;
                        _viewModel.SelectedJob.LastRunTime = job.LastRunTime;

                        // Trigger property changed notifications to update bindings
                        _viewModel.SelectedJob.OnPropertyChanged(nameof(_viewModel.SelectedJob.Progress));
                        _viewModel.SelectedJob.OnPropertyChanged(nameof(_viewModel.SelectedJob.State));
                        _viewModel.SelectedJob.OnPropertyChanged(nameof(_viewModel.SelectedJob.LastRunTime));

                        // Force UI update for the selected job
                        _viewModel.OnPropertyChanged("SelectedJob");

                        // Check if the job has completed or failed
                        if (job.State == JobState.COMPLETED ||
                            job.State == JobState.FAILED ||
                            job.State == JobState.PENDING)
                        {
                            StopProgressUpdates();
                        }
                    }
                    else
                    {
                        StopProgressUpdates();
                    }
                }
                catch (Exception ex)
                {
                    // Log any exceptions that occur during UI updates
                    Console.WriteLine($"Error updating job progress: {ex.Message}");
                    StopProgressUpdates();
                }
            };

            _progressUpdateTimer.Start();
        }

        /// <summary>
        /// Executes all backup jobs and sets up timers to update their progress and data
        /// </summary>
        private void ExecuteAllJobs()
        {
            StopProgressUpdates();

            _viewModel.ExecuteAllJobs();

            _progressUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };

            _progressUpdateTimer.Tick += (s, e) =>
            {
                try
                {
                    bool allJobsCompleted = true;
                    
                    foreach (var jobVM in _viewModel.BackupJobs)
                    {
                        BackupJob job = _viewModel.BackupManager.GetBackupJob(jobVM.Name);

                        if (job != null)
                        {
                            jobVM.Progress = job.Progress;
                            jobVM.State = job.State;
                            jobVM.LastRunTime = job.LastRunTime;

                            // Force property changed for Progress to ensure UI updates
                            jobVM.OnPropertyChanged(nameof(jobVM.Progress));
                            jobVM.OnPropertyChanged(nameof(jobVM.State));
                            jobVM.OnPropertyChanged(nameof(jobVM.LastRunTime));
                            
                            // Check if this job is still running
                            if (job.State == JobState.RUNNING || job.State == JobState.PAUSED)
                            {
                                allJobsCompleted = false;
                            }
                        }
                    }

                    // Force UI refresh if needed
                    _viewModel.OnPropertyChanged("BackupJobs");
                    
                    // If SelectedJob is one of the currently running jobs, also update its specific UI
                    if (_viewModel.SelectedJob != null)
                    {
                        _viewModel.OnPropertyChanged("SelectedJob");
                    }

                    if (allJobsCompleted)
                    {
                        StopProgressUpdates();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating job progress: {ex.Message}");
                    StopProgressUpdates();
                }
            };

            _progressUpdateTimer.Start();
        }

        /// <summary>
        /// Stops the progress update timer if it's running
        /// </summary>
        private void StopProgressUpdates()
        {
            if (_progressUpdateTimer != null)
            {
                _progressUpdateTimer.Stop();
                _progressUpdateTimer = null;
            }
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
    }
}
