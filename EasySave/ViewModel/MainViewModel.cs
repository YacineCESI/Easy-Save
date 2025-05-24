using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using EasySave.Model;

namespace EasySave.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly BackupManager _backupManager;
        private readonly LanguageManager _languageManager;
        private readonly BusinessSoftwareManager _businessSoftwareManager;
        private readonly ConfigManager _configManager;
        private string _newBlockedProcess;
        private BackupJobViewModel _selectedJob;
        private string _currentLanguage;

        private RelayCommand _addBlockedProcessCommand;
        private RelayCommand<string> _removeBlockedProcessCommand;

        // Event that will be raised when the language changes
        public event EventHandler LanguageChanged;

        public ObservableCollection<BackupJobViewModel> BackupJobs { get; }
        public ObservableCollection<string> BlockedProcesses { get; }
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

        public string NewBlockedProcess
        {
            get => _newBlockedProcess;
            set
            {
                if (_newBlockedProcess != value)
                {
                    _newBlockedProcess = value;
                    OnPropertyChanged(nameof(NewBlockedProcess));
                    _addBlockedProcessCommand?.RaiseCanExecuteChanged();
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
        public ICommand AddBlockedProcessCommand { get; }
        public ICommand RemoveBlockedProcessCommand { get; }
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
            BlockedProcesses = new ObservableCollection<string>();
            var blocked = _configManager.GetExtensionsToEncrypt();
            if (blocked != null)
            {
                foreach (var p in blocked)
                    BlockedProcesses.Add(p);
            }
            AvailableLanguages = new ObservableCollection<string>(_languageManager.GetAvailableLanguages());
            _currentLanguage = _languageManager.GetCurrentLanguage();

            // Initialize the language command to update the CurrentLanguage property
            SetLanguageCommand = new RelayCommand<string>(lang => CurrentLanguage = lang);

            _addBlockedProcessCommand = new RelayCommand(AddBlockedProcess, () => !string.IsNullOrWhiteSpace(NewBlockedProcess));
            AddBlockedProcessCommand = _addBlockedProcessCommand;

            _removeBlockedProcessCommand = new RelayCommand<string>(RemoveBlockedProcess);
            RemoveBlockedProcessCommand = _removeBlockedProcessCommand;

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

            RunJobCommand = new RelayCommand<BackupJobViewModel>(job =>
            {
                if (job != null && !_businessSoftwareManager.IsBusinessSoftwareRunning(BlockedProcesses.ToList()))
                {
                    _backupManager.ExecuteBackupJob(job.Name);
                }
            });

            RunAllJobsCommand = new RelayCommand(ExecuteAllJobs, () =>
                BackupJobs.Count > 0 &&
                !_businessSoftwareManager.IsBusinessSoftwareRunning(BlockedProcesses.ToList()));

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

        private void AddBlockedProcess()
        {
            if (!string.IsNullOrWhiteSpace(NewBlockedProcess) && !BlockedProcesses.Contains(NewBlockedProcess))
            {
                BlockedProcesses.Add(NewBlockedProcess);
                NewBlockedProcess = string.Empty;
                OnPropertyChanged(nameof(BlockedProcesses));
                _addBlockedProcessCommand?.RaiseCanExecuteChanged();
            }
        }

        private void RemoveBlockedProcess(string process)
        {
            if (BlockedProcesses.Contains(process))
            {
                BlockedProcesses.Remove(process);
                OnPropertyChanged(nameof(BlockedProcesses));
                _addBlockedProcessCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Executes the currently selected job, if possible.
        /// </summary>
        public void ExecuteSelectedJob()
        {
            if (SelectedJob != null && RunJobCommand.CanExecute(SelectedJob))
            {
                RunJobCommand.Execute(SelectedJob);
            }
        }

        /// <summary>
        /// Executes all backup jobs and updates their status in real-time
        /// </summary>
        public void ExecuteAllJobs()
        {
            if (!_businessSoftwareManager.IsBusinessSoftwareRunning(BlockedProcesses.ToList()))
            {
                // Start the backup process for all jobs
                _backupManager.ExecuteAllBackupJobs();

                // Update the RunAllJobsCommand if needed
                (RunAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
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
        protected virtual void OnPropertyChanged(string propertyName)
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