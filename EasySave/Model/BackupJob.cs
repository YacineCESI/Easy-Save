using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasySave.Model.Enums;

namespace EasySave.Model
{
    public class BackupJob
    {
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public BackupType Type { get; set; }
        public DateTime LastRunTime { get; set; }
        public List<string> ExtensionsToEncrypt { get; set; }
        public bool EncryptFiles { get; set; }
        public List<string> BlockedProcesses { get; set; }
        public LogFormat LogFormat { get; set; }

        private FileManager _fileManager;
        private bool _isPaused;
        private bool _isStopped;
        private readonly Logger _logger;

        // Add a synchronization object for thread safety
        private readonly object _lockObject = new object();

        // Ensure that state and progress are not static/shared and are per-instance
        private JobState _state;
        public JobState State
        {
            get => _state;
            set => _state = value;
        }

        private float _progress;
        public float Progress
        {
            get => _progress;
            set => _progress = value;
        }

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
            lock (_lockObject)
            {
                if (_fileManager == null)
                    throw new InvalidOperationException("FileManager must be set before executing the job.");

                // Only reset progress and state if not resuming from PAUSED
                if (State != JobState.PAUSED)  // this will make sure we don't reset progress if resuming because of the confusion with the PAUSED state and the RUNNING state
                {
                    Progress = 0;
                    State = JobState.RUNNING;
                    LastRunTime = DateTime.Now;
                }
                else
                {
                    State = JobState.RUNNING;
                }

                _isPaused = false;
                _isStopped = false;
            }
            
            // Log job launch with the selected format - thread-safe via logger
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
            
            // Define thread-safe progress update callback
            bool progressCallback(float progress)
            {
                lock (_lockObject)
                {
                    Progress = progress;
                    if (_isPaused || _isStopped)
                    {
                        return false;
                    }
                }
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                return true;
            }

            try
            {
                long result = _fileManager.CopyDirectory(
                    SourceDirectory,
                    TargetDirectory,
                    ExtensionsToEncrypt,
                    EncryptFiles,
                    progressCallback
                );
                
                lock (_lockObject)
                {
                    if (result >= 0 && !_isStopped)
                    {
                        State = JobState.COMPLETED;
                        Progress = 100.0f;
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
                }
                
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                _logger.LogError(Name, $"Job execution completed - Final state: {State}, Progress: {Progress}%", LogFormat);
                
                return result >= 0;
            }
            catch (Exception ex)
            {
                lock (_lockObject)
                {
                    State = JobState.FAILED;
                }
                _logger.LogError(Name, $"Error executing job: {ex.Message}", LogFormat);
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                return false;
            }
        }

        // Add an overload to Execute to accept blockedProcesses for resuming
        public bool Execute(List<string> blockedProcesses)
        {
            lock (_lockObject)
            {
                if (_fileManager == null)
                    throw new InvalidOperationException("FileManager must be set before executing the job.");

                // Only reset progress and state if not resuming from PAUSED
                if (State != JobState.PAUSED)
                {
                    Progress = 0;
                    State = JobState.RUNNING;
                    LastRunTime = DateTime.Now;
                }
                else
                {
                    State = JobState.RUNNING;
                }

                _isPaused = false;
                _isStopped = false;
            }

            _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
            _logger.LogAction(Name, SourceDirectory, TargetDirectory, 0, 0, 0, LogFormat);
            _logger.LogError(Name, $"Job launched - Type: {Type}, Source: {SourceDirectory}, Target: {TargetDirectory}", LogFormat);

            bool progressCallback(float progress)
            {
                lock (_lockObject)
                {
                    Progress = progress;
                    if (_isPaused || _isStopped)
                    {
                        return false;
                    }
                }
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                return true;
            }

            try
            {
                // Pass blockedProcesses to FileManager.CopyDirectory
                long result = _fileManager.CopyDirectory(
                    SourceDirectory,
                    TargetDirectory,
                    ExtensionsToEncrypt,
                    EncryptFiles,
                    progressCallback,
                    blockedProcesses
                );

                lock (_lockObject)
                {
                    if (result >= 0 && !_isStopped)
                    {
                        State = JobState.COMPLETED;
                        Progress = 100.0f;
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
                }

                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                _logger.LogError(Name, $"Job execution completed - Final state: {State}, Progress: {Progress}%", LogFormat);

                return result >= 0;
            }
            catch (Exception ex)
            {
                lock (_lockObject)
                {
                    State = JobState.FAILED;
                }
                _logger.LogError(Name, $"Error executing job: {ex.Message}", LogFormat);
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                return false;
            }
        }

        public void Pause()
        {
            lock (_lockObject)
            {
                if (State == JobState.RUNNING)
                {
                    _isPaused = true;
                    State = JobState.PAUSED;
                    _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                    _logger.LogError(Name, "Job paused", LogFormat);
                }
            }
        }

        public void Resume()
        {
            lock (_lockObject)
            {
                if (State == JobState.PAUSED)
                {
                    _isPaused = false;
                    State = JobState.RUNNING;
                    _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                    _logger.LogError(Name, "Job resumed", LogFormat);
                    System.Diagnostics.Debug.WriteLine($"[BackupJOb] Job resumed successfully.");
                    // Do NOT call Execute() or start a new Task here!
                }
            }
        }

        public void Stop()
        {
            lock (_lockObject)
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
        }

        public float GetProgress()
        {
            lock (_lockObject)
            {
                return Progress;
            }
        }

        public JobState GetState()
        {
            lock (_lockObject)
            {
                return State;
            }
        }

        public List<string> GetAllFilesToBackup()
        {
            // Return a flat list of all files this job will back up (full paths)
            // Implement this based on your job's source directory and filters
            // Example:
            var files = Directory.GetFiles(SourceDirectory, "*.*", SearchOption.AllDirectories).ToList();
            return files;
        }

        /// <summary>
        /// Returns the list of files that will be transferred for this job,
        /// depending on the backup type (FULL or DIFFERENTIAL).
        /// </summary>
        public List<string> GetFilesToTransfer()
        {
            // For FULL backup, return all files in the source directory (recursively)
            if (Type == BackupType.FULL)
            {
                if (Directory.Exists(SourceDirectory))
                    return Directory.GetFiles(SourceDirectory, "*", SearchOption.AllDirectories).ToList();
                else
                    return new List<string>();
            }
            // For DIFFERENTIAL backup, return only files that are new or changed compared to the target
            else if (Type == BackupType.DIFFERENTIAL)
            {
                var filesToTransfer = new List<string>();
                if (!Directory.Exists(SourceDirectory))
                    return filesToTransfer;

                var sourceFiles = Directory.GetFiles(SourceDirectory, "*", SearchOption.AllDirectories);
                foreach (var sourceFile in sourceFiles)
                {
                    var relativePath = Path.GetRelativePath(SourceDirectory, sourceFile);
                    var targetFile = Path.Combine(TargetDirectory, relativePath);

                    if (!File.Exists(targetFile) ||
                        File.GetLastWriteTimeUtc(sourceFile) > File.GetLastWriteTimeUtc(targetFile) ||
                        new FileInfo(sourceFile).Length != new FileInfo(targetFile).Length)
                    {
                        filesToTransfer.Add(sourceFile);
                    }
                }
                return filesToTransfer;
            }
            else
            {
                // Default: return all files
                if (Directory.Exists(SourceDirectory))
                    return Directory.GetFiles(SourceDirectory, "*", SearchOption.AllDirectories).ToList();
                else
                    return new List<string>();
            }
        }

        // we have been obligated to add this method that handels the Resume (play) for our job backups to avoid the confusion in the Excute function because it leads to reset the backup 

        /// <summary>
        /// Resumes the job by setting state/flags only. Actual copy operation must be started by BackupManager.
        /// </summary>
        public void PrepareResume()
        {
            lock (_lockObject)
            {
                if (_fileManager == null)
                    throw new InvalidOperationException("FileManager must be set before resuming the job.");

                if (State != JobState.PAUSED)
                    return; // Only resume if paused

                _isPaused = false;
                _isStopped = false;
                State = JobState.RUNNING;
                _logger.UpdateJobStatus(Name, State, Progress, LogFormat);
                _logger.LogError(Name, "Job resumed (PrepareResume)", LogFormat);
            }
        }

        /// <summary>
        /// (No longer used externally) Resumes the execution of a paused backup job, continuing from its last state/progress.
        /// </summary>
        /// <returns>True if resumed and completed, false otherwise.</returns>
        [Obsolete("Use PrepareResume() and then call Execute() in a new Task from BackupManager.")]
        public bool ResumeExecution()
        {
            PrepareResume();
            return Execute();
        }
    }
}