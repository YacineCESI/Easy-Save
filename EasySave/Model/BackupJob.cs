using System;
using System.Collections.Generic;
using System.IO;
using EasySave.Model.Enums;

namespace EasySave.Model
{
    public class BackupJob
    {
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public BackupType Type { get; set; }
        public JobState State { get; set; }
        public DateTime LastRunTime { get; set; }
        public float Progress { get; set; }
        public List<string> ExtensionsToEncrypt { get; set; }
        public bool EncryptFiles { get; set; }
        public List<string> BlockedProcesses { get; set; }

        private FileManager _fileManager;
        private bool _isPaused;
        private bool _isStopped;

        public BackupJob(string name, string sourceDirectory, string targetDirectory, BackupType type, bool encryptFiles = false, List<string> extensionsToEncrypt = null, List<string> blockedProcesses = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Job name cannot be empty", nameof(name));

            if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
                throw new ArgumentException("Source directory is invalid or does not exist", nameof(sourceDirectory));

            if (string.IsNullOrEmpty(targetDirectory))
                throw new ArgumentException("Target directory cannot be empty", nameof(targetDirectory));

            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
            State = JobState.PENDING;
            LastRunTime = DateTime.MinValue;
            Progress = 0.0f;
            EncryptFiles = encryptFiles;
            ExtensionsToEncrypt = extensionsToEncrypt ?? new List<string>();
            BlockedProcesses = blockedProcesses ?? new List<string>();

            _isPaused = false;
            _isStopped = false;
        }

        public void SetFileManager(FileManager fileManager)
        {
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        }

        public bool Execute()
        {
            if (_fileManager == null)
                throw new InvalidOperationException("FileManager must be set before executing the job.");

            // Use EncryptFiles and ExtensionsToEncrypt for this job
            return _fileManager.CopyDirectory(
                SourceDirectory,
                TargetDirectory,
                ExtensionsToEncrypt,
                EncryptFiles
            ) >= 0;
        }

        public void Pause()
        {
            if (State == JobState.RUNNING)
            {
                _isPaused = true;
                State = JobState.PAUSED;
                var statusLogger = new Logger();
                statusLogger.UpdateJobStatus(Name, State, Progress);
            }
        }

        public void Resume()
        {
            if (State == JobState.PAUSED)
            {
                _isPaused = false;
                State = JobState.RUNNING;
                var statusLogger = new Logger();
                statusLogger.UpdateJobStatus(Name, State, Progress);
            }
        }

        public void Stop()
        {
            if (State == JobState.RUNNING || State == JobState.PAUSED)
            {
                _isStopped = true;
                _isPaused = false;
                State = JobState.PENDING;
                var statusLogger = new Logger();
                statusLogger.UpdateJobStatus(Name, State, Progress);
            }
        }

        public float GetProgress()
        {
            return Progress;
        }

        public JobState GetState()
        {
            return State;
        }
    }
}