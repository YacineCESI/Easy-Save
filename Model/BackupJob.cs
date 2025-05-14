using System;
using System.IO;
using EasySave.Model.Enums;

namespace EasySave.Model
{
    /// Represents a backup job with source, destination and execution state
    public class BackupJob
    {
        public string Name { get; private set; }
        public string SourceDirectory { get; private set; }
        public string TargetDirectory { get; private set; }
        public BackupType Type { get; private set; }
        public JobState State { get; private set; }
        public DateTime LastRunTime { get; private set; }
        public float Progress { get; private set; }

        private FileManager _fileManager;
        private Logger _logger;

        private bool _isPaused;
        private bool _isStopped;

        public BackupJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Job name cannot be empty", nameof(name));
            if (string.IsNullOrEmpty(sourceDirectory))
                throw new ArgumentException("Source directory cannot be empty", nameof(sourceDirectory));
            if (string.IsNullOrEmpty(targetDirectory))
                throw new ArgumentException("Target directory cannot be empty", nameof(targetDirectory));

            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
            State = JobState.PENDING;
            LastRunTime = DateTime.MinValue;
        }

        public bool Excute()
        {
            // TODO: Implement backup execution logic
            return false;
        }

        public void Pause()
        {
            // TODO: Implement pause logic
        }

        public void Resume()
        {
            // TODO: Implement resume logic
        }

        public void Stop()
        {
            // TODO: Implement stop logic
        }

        public float GetProgress()
        {
            // TODO: Implement progress calculation
            return 0f;
        }

        public JobState GetState()
        {
            // TODO: Implement state retrieval
            return JobState.PENDING;
        }
    }
}