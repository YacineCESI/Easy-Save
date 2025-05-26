using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using EasySave.Model;
using System.Linq;
using System.Threading.Tasks;

namespace EasySave.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly BackupManager _backupManager;
        private readonly LanguageManager _languageManager;
        private readonly BusinessSoftwareManager _businessSoftwareManager;
        private readonly ConfigManager _configManager;
        private BackupJobViewModel _selectedJob;
        private string _currentLanguage;

        // Event that will be raised when the language changes
        public event EventHandler LanguageChanged;

        public ObservableCollection<BackupJobViewModel> BackupJobs { get; }
        public ObservableCollection<string> AvailableLanguages { get; }

        public BackupManager BackupManager => _backupManager;

        public BackupJobViewModel SelectedJob
        {
            get => _selectedJob;
            set
            {
                if (_selectedJob != value)
                {
                    _selectedJob = value;
                    OnPropertyChanged(nameof(SelectedJob));
                    // FIX: Do NOT start job or status monitoring here!
                    // Previously, if StartJobStatusMonitoring or job execution was called here, remove it.
                }
            }
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    _languageManager.SwitchLanguage(value);

                    // Notify all string properties that they need to be updated
                    OnPropertyChanged(nameof(CurrentLanguage));
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(BackupJobsHeader));
                    OnPropertyChanged(nameof(JobDetailsHeader));
                    OnPropertyChanged(nameof(NewJobButtonText));
                    OnPropertyChanged(nameof(RunAllButtonText));
                    OnPropertyChanged(nameof(RunButtonText));
                    OnPropertyChanged(nameof(PauseButtonText));
                    OnPropertyChanged(nameof(ResumeButtonText));
                    OnPropertyChanged(nameof(RemoveButtonText));
                    OnPropertyChanged(nameof(NameLabelText));
                    OnPropertyChanged(nameof(SourceDirectoryLabelText));
                    OnPropertyChanged(nameof(TargetDirectoryLabelText));
                    OnPropertyChanged(nameof(BackupTypeLabelText));
                    OnPropertyChanged(nameof(StatusLabelText));
                    OnPropertyChanged(nameof(LastRunLabelText));
                    OnPropertyChanged(nameof(EncryptionLabelText));
                    OnPropertyChanged(nameof(ProgressLabelText));
                    OnPropertyChanged(nameof(SelectJobMessage));
                    OnPropertyChanged(nameof(BlockedProcessesHeader));
                    OnPropertyChanged(nameof(AddProcessButtonText));

                    // Save the selected language to config
                    _configManager.SetLanguage(value);

                    // Raise the LanguageChanged event
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // Localized strings properties
        public string WindowTitle => _languageManager.GetString("strMainWindowTitle") ?? "EasySave Backup";
        public string BackupJobsHeader => _languageManager.GetString("strBackupJobs") ?? "Backup Jobs";
        public string JobDetailsHeader => _languageManager.GetString("strJobDetails") ?? "Job Details";
        public string NewJobButtonText => _languageManager.GetString("strNewJob") ?? "New Job";
        public string RunAllButtonText => _languageManager.GetString("strRunAll") ?? "Run All";
        public string RunButtonText => _languageManager.GetString("strRun") ?? "Run";
        public string PauseButtonText => _languageManager.GetString("strPause") ?? "Pause";
        public string ResumeButtonText => _languageManager.GetString("strResume") ?? "Resume";
        public string RemoveButtonText => _languageManager.GetString("strRemove") ?? "Remove";
        public string NameLabelText => _languageManager.GetString("strName") ?? "Name:";
        public string SourceDirectoryLabelText => _languageManager.GetString("strSourceDirectory") ?? "Source Directory:";
        public string TargetDirectoryLabelText => _languageManager.GetString("strTargetDirectory") ?? "Target Directory:";
        public string BackupTypeLabelText => _languageManager.GetString("strBackupType") ?? "Backup Type:";
        public string StatusLabelText => _languageManager.GetString("strStatus") ?? "Status:";
        public string LastRunLabelText => _languageManager.GetString("strLastRun") ?? "Last Run:";
        public string EncryptionLabelText => _languageManager.GetString("strEncryption") ?? "Encryption:";
        public string ProgressLabelText => _languageManager.GetString("strProgress") ?? "Progress:";
        public string SelectJobMessage => _languageManager.GetString("strSelectJob") ?? "Select a backup job to view details";
        public string BlockedProcessesHeader => _languageManager.GetString("strBlockedProcesses") ?? "Blocked Processes";
        public string AddProcessButtonText => _languageManager.GetString("strAddProcess") ?? "Add Process";

        // Helper method to get localized string
        public string GetLocalizedString(string key, string defaultValue = null)
        {
            return _languageManager.GetString(key) ?? defaultValue ?? key;
        }

        // Commands
        public ICommand SetLanguageCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand RunJobCommand { get; }
        public ICommand RunAllJobsCommand { get; }
        public ICommand RemoveJobCommand { get; }
        public ICommand PauseJobCommand { get; }
        public ICommand ResumeJobCommand { get; }
        public ICommand StopJobCommand { get; }

        public MainViewModel(BackupManager backupManager, LanguageManager languageManager,
            BusinessSoftwareManager businessSoftwareManager, ConfigManager configManager)
        {
            _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _businessSoftwareManager = businessSoftwareManager ?? throw new ArgumentNullException(nameof(businessSoftwareManager));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));

            BackupJobs = new ObservableCollection<BackupJobViewModel>(_backupManager.GetAllJobs().Select(j => new BackupJobViewModel(j)));
            AvailableLanguages = new ObservableCollection<string>(_languageManager.GetAvailableLanguages());
            _currentLanguage = _languageManager.GetCurrentLanguage();

            // Initialize the language command to update the CurrentLanguage property
            SetLanguageCommand = new RelayCommand<string>(lang => CurrentLanguage = lang);

            CreateJobCommand = new RelayCommand<BackupJobViewModel>(job =>
            {
                if (job != null)
                {
                    var backupJob = new BackupJob(
                        job.Name,
                        job.SourceDirectory,
                        job.TargetDirectory,
                        job.Type,
                        job.EncryptFiles,
                        job.ExtensionsToEncrypt.ToList(),
                        job.BlockedProcesses.ToList()
                    );
                    if (_backupManager.AddBackupJob(backupJob))
                    {
                        BackupJobs.Add(job);
                    }
                }
            });

            // Modified RunJobCommand to check job's own blocked processes
            RunJobCommand = new RelayCommand<BackupJobViewModel>(job =>
            {
                if (job == null)
                    return;

                // Check if any of this job's blocked processes are running
                var blockedProcesses = job.BlockedProcesses?.ToList() ?? new List<string>();
                var runningProcesses = _businessSoftwareManager.GetRunningBlockedProcesses(blockedProcesses);

                if (runningProcesses.Count > 0)
                {
                    // Notify that blocked processes are running
                    BlockedProcessesDetected?.Invoke(this, runningProcesses);

                    // Stop any ongoing job with this name
                    _backupManager.StopJob(job.Name);

                    return;
                }

                // Execute the job in parallel mode (BackupManager now handles this internally)
                _backupManager.ExecuteBackupJob(job.Name);
            },
            job => job != null);

            // Modified RunAllJobsCommand to respect Can Execute condition
            RunAllJobsCommand = new RelayCommand(ExecuteAllJobs, () => BackupJobs.Count > 0);

            RemoveJobCommand = new RelayCommand<string>(name =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var job = BackupJobs.FirstOrDefault(j => j.Name == name);
                    if (job != null && _backupManager.RemoveBackupJob(name))
                    {
                        BackupJobs.Remove(job);
                    }
                }
            });

            PauseJobCommand = new RelayCommand<BackupJobViewModel>(job =>
            {
                if (job != null)
                {
                    _backupManager.PauseJob(job.Name);
                }
            });

            ResumeJobCommand = new RelayCommand<BackupJobViewModel>(job =>
            {
                if (job != null)
                {
                    // Check for blocked processes before resuming
                    var blockedProcesses = job.BlockedProcesses?.ToList() ?? new List<string>();
                    var runningProcesses = _businessSoftwareManager.GetRunningBlockedProcesses(blockedProcesses);

                    if (runningProcesses.Count > 0)
                    {
                        // Notify that blocked processes are running
                        BlockedProcessesDetected?.Invoke(this, runningProcesses);
                        return;
                    }

                    _backupManager.ResumeJob(job.Name);
                }
            });

            StopJobCommand = new RelayCommand<BackupJobViewModel>(job =>
            {
                if (job != null)
                {
                    _backupManager.StopJob(job.Name);
                }
            });
        }

            // Helper method to update the properties
        private void UpdateJobViewModelProperties(BackupJobViewModel jobViewModel, float progress, Model.Enums.JobState state, DateTime lastRunTime)
        {
            jobViewModel.Progress = progress;
            jobViewModel.State = state;
            jobViewModel.LastRunTime = lastRunTime;
        }


        /// <summary>
        /// Executes the currently selected job, if possible.
        /// Shows warning if blocked processes are running.
        /// </summary>
        public List<string> GetRunningBlockedProcessesForSelectedJob()
        {
            if (SelectedJob == null)
                return new List<string>();
            var jobBlockedProcesses = SelectedJob.BlockedProcesses?.ToList() ?? new List<string>();
            return _businessSoftwareManager.GetRunningBlockedProcesses(jobBlockedProcesses);
        }

        public void ExecuteSelectedJob()
        {
            if (SelectedJob == null)
                return;

            var runningBlockedProcesses = GetRunningBlockedProcessesForSelectedJob();
            if (runningBlockedProcesses.Count > 0)
            {
                // Let MainWindow handle the popup
                BlockedProcessesDetected?.Invoke(this, runningBlockedProcesses);

                // Stop the job if it was already running
                _backupManager.StopJob(SelectedJob.Name);

                return;
            }

            // Safe to execute
            RunJobCommand.Execute(SelectedJob);
        }

        /// <summary>
        /// Executes all backup jobs and updates their status in real-time
        /// Shows warning if blocked processes are running.
        /// </summary>
        public List<string> GetRunningBlockedProcessesForAnyJob()
        {
            var allBlocked = BackupJobs
            .SelectMany<BackupJobViewModel, string>(j => j.BlockedProcesses != null ? j.BlockedProcesses : new List<string>())
            .Distinct()
            .ToList();
            return _businessSoftwareManager.GetRunningBlockedProcesses(allBlocked);
        }

        /// <summary>
        /// Stops all running backup jobs
        /// </summary>
        public void StopAllJobs()
        {
            foreach (var job in BackupJobs)
            {
                _backupManager.StopJob(job.Name);
            }
        }

        public void ExecuteAllJobs()
        {
            var runningBlockedProcesses = GetRunningBlockedProcessesForAnyJob();
            if (runningBlockedProcesses.Count > 0)
            {
                // Notify about blocked processes
                BlockedProcessesDetected?.Invoke(this, runningBlockedProcesses);

                // Stop all running jobs
                StopAllJobs();

                return;
            }

            // Safe to execute all jobs in parallel
            _backupManager.ExecuteAllBackupJobs();
        }

        // Add an event to notify MainWindow of blocked processes
        public event EventHandler<List<string>> BlockedProcessesDetected;

        /// <summary>
        /// Continuously checks if any blocked processes start running during job execution
        /// and stops jobs if necessary
        /// </summary>
        public void StartBlockedProcessMonitoring()
        {
            // This would be implemented with a background task that periodically
            // checks for blocked processes and stops jobs if needed
            // For simplicity in this implementation, we'll just check before execution
        }

        /// <summary>
        /// Refreshes the BackupJobs collection from the BackupManager.
        /// </summary>
        public void RefreshBackupJobs()
        {
            // Clear and repopulate the BackupJobs collection from BackupManager
            BackupJobs.Clear();
            foreach (var job in BackupManager.GetAllJobs())
            {
                BackupJobs.Add(new BackupJobViewModel(job));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Updated command implementations that don't rely on CommandManager
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;
        private event EventHandler _canExecuteChanged;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecute != null)
                {
                    _canExecuteChanged += value;
                }
            }
            remove
            {
                if (_canExecute != null)
                {
                    _canExecuteChanged -= value;
                }
            }
        }

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;
        private event EventHandler _canExecuteChanged;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                return false;
            if (parameter == null || parameter is T)
                return _canExecute == null || _canExecute((T)parameter);
            return false;
        }

        public void Execute(object parameter)
        {
            if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                return;
            if (parameter == null || parameter is T)
                _execute((T)parameter);
            // else: do nothing if parameter is not of type T
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecute != null)
                {
                    _canExecuteChanged += value;
                }
            }
            remove
            {
                if (_canExecute != null)
                {
                    _canExecuteChanged -= value;
                }
            }
        }

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
