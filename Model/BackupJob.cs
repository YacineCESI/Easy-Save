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
            Progress = 0.0f;
            LastRunTime = DateTime.MinValue;

            _fileManager = new FileManager();
        }

        public bool Execute()
        {
            try
            {
                // Set state to running
                State = JobState.RUNNING;
                _isPaused = false;
                _isStopped = false;
                Progress = 0.0f;

                // Log job start
                // _logger.UpdateJobStatus(Name, State, Progress);

                // Make sure target directory exists
                if (!Directory.Exists(TargetDirectory))
                {
                    Directory.CreateDirectory(TargetDirectory);
                }

                // Execute the backup operation
                bool isFullBackup = (Type == BackupType.FULL);
                long result = _fileManager.CopyDirectory(SourceDirectory, TargetDirectory, isFullBackup,
                    onProgressUpdate: (progress) => {
                        Progress = progress;
                        //   _logger.UpdateJobStatus(Name, State, Progress);

                        // Handle pause and stop
                        if (_isPaused)
                        {
                            while (_isPaused && !_isStopped)
                            {
                                System.Threading.Thread.Sleep(500);
                            }
                        }
                        return !_isStopped; // Continue if not stopped
                    });

                // Update job completion status
                LastRunTime = DateTime.Now;

                if (_isStopped)
                {
                    State = JobState.PENDING;
                }
                else
                {
                    State = (result >= 0) ? JobState.COMPLETED : JobState.FAILED;
                    Progress = (result >= 0) ? 100.0f : Progress;
                }

                // Log job completion
                //   _logger.UpdateJobStatus(Name, State, Progress);

                return State == JobState.COMPLETED;
            }
            catch (Exception ex)
            {
                State = JobState.FAILED;
                Progress = 0.0f;
                //     _logger.UpdateJobStatus(Name, State, Progress);
                //      _logger.LogError(Name, ex.Message);
                return false;
            }
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
            
            return Progress;
        }

        public JobState GetState()
        {
            
            return State;
        }
    }
}