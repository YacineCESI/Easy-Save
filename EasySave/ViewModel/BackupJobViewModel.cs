using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using EasySave.Model;
using EasySave.Model.Enums;

namespace EasySave.ViewModel
{
    public class BackupJobViewModel : INotifyPropertyChanged
    {
        // Store properties directly in the ViewModel
        private string _name;
        private string _sourceDirectory;
        private string _targetDirectory;
        private BackupType _type;
        private JobState _state;
        private DateTime _lastRunTime;
        private float _progress;
        private bool _encryptFiles;

        public BackupJobViewModel()
        {
            ExtensionsToEncrypt = new ObservableCollection<string>();
            BlockedProcesses = new ObservableCollection<string>();
        }

        public BackupJobViewModel(BackupJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            _name = job.Name;
            _sourceDirectory = job.SourceDirectory;
            _targetDirectory = job.TargetDirectory;
            _type = job.Type;
            _state = job.State;
            _lastRunTime = job.LastRunTime;
            _progress = job.Progress;
            _encryptFiles = job.EncryptFiles;
            ExtensionsToEncrypt = new ObservableCollection<string>(job.ExtensionsToEncrypt ?? new System.Collections.Generic.List<string>());
            BlockedProcesses = new ObservableCollection<string>(job.BlockedProcesses ?? new System.Collections.Generic.List<string>());
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string SourceDirectory
        {
            get => _sourceDirectory;
            set
            {
                if (_sourceDirectory != value)
                {
                    _sourceDirectory = value;
                    OnPropertyChanged(nameof(SourceDirectory));
                }
            }
        }

        public string TargetDirectory
        {
            get => _targetDirectory;
            set
            {
                if (_targetDirectory != value)
                {
                    _targetDirectory = value;
                    OnPropertyChanged(nameof(TargetDirectory));
                }
            }
        }

        public BackupType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        public JobState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        public DateTime LastRunTime
        {
            get => _lastRunTime;
            set
            {
                if (_lastRunTime != value)
                {
                    _lastRunTime = value;
                    OnPropertyChanged(nameof(LastRunTime));
                }
            }
        }

        public float Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        public bool EncryptFiles
        {
            get => _encryptFiles;
            set
            {
                if (_encryptFiles != value)
                {
                    _encryptFiles = value;
                    OnPropertyChanged(nameof(EncryptFiles));
                }
            }
        }

        public ObservableCollection<string> ExtensionsToEncrypt { get; set; }

        public ObservableCollection<string> BlockedProcesses { get; set; }

        // Commands
        public ICommand SaveCommand { get; set; }
        public ICommand BrowseSourceCommand { get; set; }
        public ICommand BrowseTargetCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Helper to create a BackupJob from the ViewModel's properties
        public BackupJob ToBackupJob()
        {
            return new BackupJob(
                name: Name,
                sourceDirectory: SourceDirectory,
                targetDirectory: TargetDirectory,
                type: Type,
                encryptFiles: EncryptFiles,
                extensionsToEncrypt: new System.Collections.Generic.List<string>(ExtensionsToEncrypt),
                blockedProcesses: new System.Collections.Generic.List<string>(BlockedProcesses)
            );
        }

        /// <summary>
        /// Creates a BackupJob from the ViewModel's properties and adds it to the BackupManager.
        /// Returns true if the job was added successfully, false otherwise.
        /// </summary>
        public bool CreateAndSaveBackupJob(BackupManager backupManager)
        {
            if (backupManager == null)
                throw new ArgumentNullException(nameof(backupManager));

            var job = new BackupJob(
                name: Name,
                sourceDirectory: SourceDirectory,
                targetDirectory: TargetDirectory,
                type: Type,
                encryptFiles: EncryptFiles,
                extensionsToEncrypt: new System.Collections.Generic.List<string>(ExtensionsToEncrypt),
                blockedProcesses: new System.Collections.Generic.List<string>(BlockedProcesses)
            );

            return backupManager.AddBackupJob(job);
        }
    }
}
