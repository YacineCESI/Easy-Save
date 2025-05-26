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
        public LogFormat LogFormat { get; set; }

        private FileManager _fileManager;
        private bool _isPaused;
        private bool _isStopped;
        private readonly Logger _logger;

        public BackupJob(string name, string sourceDirectory, string targetDirectory, BackupType type, bool encryptFiles = false, List<string> extensionsToEncrypt = null, List<string> blockedProcesses = null, LogFormat logFormat = LogFormat.JSON)
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
            LogFormat = logFormat;

            _isPaused = false;
            _isStopped = false;
            _logger = new Logger();
            
            // Log job creation with the selected format
            _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
            _logger.LogAction(Name, 
                              SourceDirectory, 
                              TargetDirectory, 
                              0, // No files transferred yet
                              0, // No transfer time yet
                               0, // No encryption time yet
                              LogFormat
            );
            _logger.LogError(Name, $"Job created - Type: {Type}, EncryptFiles: {EncryptFiles}, Extensions to encrypt: {ExtensionsToEncrypt.Count}, Blocked processes: {BlockedProcesses.Count}, Format: {LogFormat}", LogFormat);
        }

        public void SetFileManager(FileManager fileManager)
        {
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        }

        public bool Execute()
        {
            if (_fileManager == null)
                throw new InvalidOperationException("FileManager must be set before executing the job.");

            // Reset progress and update state
            Progress = 0;
            State = JobState.RUNNING;
            LastRunTime = DateTime.Now;
            
            // Log job launch with the selected format
            _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
            _logger.LogAction(Name, 
                              SourceDirectory, 
                              TargetDirectory, 
                              0, // No files transferred yet
                              0, // No transfer time yet
                              0, // No encryption time yet
                              LogFormat
            );
            _logger.LogError(Name, $"Job launched - Type: {Type}, Source: {SourceDirectory}, Target: {TargetDirectory}", LogFormat);
            
            // Define progress update callback
            bool progressCallback(float progress)
            {
                // Update the job's progress
                Progress = progress;
                
                // Log the progress update with the selected format
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                
                // Check if the operation should be cancelled
                if (_isPaused || _isStopped)
                {
                    return false;
                }
                
                return true;
            }

            try
            {
                // Use EncryptFiles and ExtensionsToEncrypt for this job
                long result = _fileManager.CopyDirectory(
                    SourceDirectory,
                    TargetDirectory,
                    ExtensionsToEncrypt,
                    EncryptFiles,
                    progressCallback
                );
                
                // Update state based on result
                if (result >= 0 && !_isStopped)
                {
                    State = JobState.COMPLETED;
                    Progress = 100.0f; // Ensure 100% on completion
                }
                else if (_isPaused)
                {
                    State = JobState.PAUSED;
                }
                else if (_isStopped)
                {
                    State = JobState.PENDING;
                    Progress = 0.0f;
                }
                else
                {
                    State = JobState.FAILED;
                }
                
                // Final status update with the selected format
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                _logger.LogError(Name, $"Job execution completed - Final state: {State}, Progress: {Progress}%", LogFormat);
                
                return result >= 0;
            }
            catch (Exception ex)
            {
                State = JobState.FAILED;
                _logger.LogError(Name, $"Error executing job: {ex.Message}", LogFormat);
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                return false;
            }
        }

        public void Pause()
        {
            if (State == JobState.RUNNING)
            {
                _isPaused = true;
                State = JobState.PAUSED;
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                _logger.LogError(Name, "Job paused", LogFormat);
            }
        }

        public void Resume()
        {
            if (State == JobState.PAUSED)
            {
                _isPaused = false;
                State = JobState.RUNNING;
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                _logger.LogError(Name, "Job resumed", LogFormat);
            }
        }

        public void Stop()
        {
            if (State == JobState.RUNNING || State == JobState.PAUSED)
            {
                _isStopped = true;
                _isPaused = false;
                State = JobState.PENDING;
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                _logger.LogError(Name, "Job stopped", LogFormat);
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