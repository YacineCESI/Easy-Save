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
            // Trigger execution of all backup jobs via BackupManager
            bool allSuccess = _viewModel.BackupManager.ExecuteAllBackupJobs();

            // Optionally, refresh the job list to update their states
            _viewModel.RefreshBackupJobs();

            if (allSuccess)
            {
                MessageBox.Show("All backup jobs executed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Some backup jobs may have failed.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RunJob_Click(object sender, RoutedEventArgs e)
        {
            // Call the modified method to execute the selected job with progress updates
            ExecuteSelectedJob();
        }

        /// <summary>
        /// Executes the selected job and sets up a timer to update the progress bar in real-time
        /// </summary>
        private void ExecuteSelectedJob()
        {
            // Stop any existing timer
            StopProgressUpdates();

            if (_viewModel.SelectedJob == null)
                return;

            // Get the name of the selected job for later reference
            string jobName = _viewModel.SelectedJob.Name;
            
            // Execute the job through the view model (this starts the job asynchronously)
            _viewModel.ExecuteSelectedJob();
            
            // Set up a timer to update the progress
            _progressUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Update every 100ms
            };
            
            _progressUpdateTimer.Tick += (s, e) =>
            {
                // Get the current job from the backup manager to ensure we have the latest progress
                BackupJob job = _viewModel.BackupManager.GetBackupJob(jobName);
                
                if (job != null)
                {
                    // Update the view model's selected job with the latest progress
                    _viewModel.SelectedJob.Progress = job.Progress;
                    
                    // Check if the job has completed or failed
                    if (job.GetState() == JobState.COMPLETED || 
                        job.GetState() == JobState.FAILED || 
                        job.GetState() == JobState.PENDING)
                    {
                        // Update state in view model
                        _viewModel.SelectedJob.State = job.State;
                        
                        // Stop the timer as the job is no longer running
                        StopProgressUpdates();
                    }
                }
                else
                {
                    // If the job no longer exists, stop updates
                    StopProgressUpdates();
                }
            };
            
            // Start the timer
            _progressUpdateTimer.Start();
        }

        /// <summary>
        /// Executes all backup jobs and sets up timers to update their progress
        /// </summary>
        private void ExecuteAllJobs()
        {
            // Stop any existing timer
            StopProgressUpdates();
            
            // Execute all jobs through the view model
            _viewModel.ExecuteAllJobs();
            
            // Set up a timer to update the progress of all jobs
            _progressUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200) // Update every 200ms
            };
            
            _progressUpdateTimer.Tick += (s, e) =>
            {
                // Update all job statuses from the backup manager
                foreach (var jobVM in _viewModel.BackupJobs)
                {
                    BackupJob job = _viewModel.BackupManager.GetBackupJob(jobVM.Name);
                    
                    if (job != null)
                    {
                        // Update the job's progress in the view model
                        jobVM.Progress = job.Progress;
                        jobVM.State = job.State;
                    }
                }
                
                // Check if all jobs are completed or not running
                bool allJobsCompleted = _viewModel.BackupJobs.All(j => 
                    j.State == JobState.COMPLETED || 
                    j.State == JobState.FAILED || 
                    j.State == JobState.PENDING);
                    
                if (allJobsCompleted)
                {
                    // All jobs are done, stop the update timer
                    StopProgressUpdates();
                }
            };
            
            // Start the timer
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
