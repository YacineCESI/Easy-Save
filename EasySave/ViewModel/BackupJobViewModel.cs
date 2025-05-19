using System;
using System.Windows.Input;
using EasySave.Model;
using System.ComponentModel;

namespace EasySave.ViewModel
{
    public class BackupJobViewModel : INotifyPropertyChanged
    {
        private BackupJob backupJob;

        // Parameterless constructor for serialization/binding frameworks
        public BackupJobViewModel()
        {
            // Initialize with default values to avoid null reference errors
            backupJob = new BackupJob(
                name: string.Empty,
                sourceDirectory: string.Empty,
                targetDirectory: string.Empty,
                type: BackupType.FULL
            );
        }

        public BackupJobViewModel(BackupJob job)
        {
            backupJob = job ?? throw new ArgumentNullException(nameof(job));
        }

        public string Name
        {
            get => backupJob.Name;
            set
            {
                if (backupJob.Name != value)
                {
                    backupJob.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string SourceDirectory
        {
            get => backupJob.SourceDirectory;
            set
            {
                if (backupJob.SourceDirectory != value)
                {
                    backupJob.SourceDirectory = value;
                    OnPropertyChanged(nameof(SourceDirectory));
                }
            }
        }

        public string TargetDirectory
        {
            get => backupJob.TargetDirectory;
            set
            {
                if (backupJob.TargetDirectory != value)
                {
                    backupJob.TargetDirectory = value;
                    OnPropertyChanged(nameof(TargetDirectory));
                }
            }
        }

        public BackupType Type
        {
            get => backupJob.Type;
            set
            {
                if (backupJob.Type != value)
                {
                    backupJob.Type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        public JobState State
        {
            get => backupJob.State;
            set
            {
                if (backupJob.State != value)
                {
                    backupJob.State = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        public DateTime LastRunTime
        {
            get => backupJob.LastRunTime;
            set
            {
                if (backupJob.LastRunTime != value)
                {
                    backupJob.LastRunTime = value;
                    OnPropertyChanged(nameof(LastRunTime));
                }
            }
        }

        public float Progress
        {
            get => backupJob.Progress;
            set
            {
                if (Math.Abs(backupJob.Progress - value) > 0.001f)
                {
                    backupJob.Progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        public ICommand SaveCommand { get; set; }
        public ICommand BrowseSourceCommand { get; set; }
        public ICommand BrowseTargetCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
