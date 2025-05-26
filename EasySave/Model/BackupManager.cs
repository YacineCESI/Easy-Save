using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

// ...existing code...

// Ensure that jobs are only started by explicit calls to ExecuteBackupJob or ExecuteAllBackupJobs.
// Do NOT start jobs in GetBackupJob, GetAllJobs, or any property getter/setter.

// ...existing code...
namespace EasySave.Model
{
    public class BackupManager
    {
        private List<BackupJob> backupJobs = new();
        private readonly ConfigManager _configManager;
        private readonly string _jobsConfigPath;
        private readonly Logger _logger;
        private readonly FileManager _fileManager;
        // Track running tasks for each job
        private readonly ConcurrentDictionary<string, Task> _runningTasks = new();

        private readonly object _largeFileLock = new();
        private volatile bool _largeFileTransferring = false;

        public int BandwidthLimitKB => _configManager.BandwidthLimitKB;

        // Default constructor
        public BackupManager()
        {
            _configManager = new ConfigManager();
            _logger = new Logger();

            // Initialize FileManager with a new CryptoSoftManager
            _fileManager = new FileManager(
                new CryptoSoftManager(), // Removed the argument as CryptoSoftManager does not accept any parameters
                 _logger
);

            _jobsConfigPath = Path.Combine(@"C:\EasySave\Config", "jobs.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_jobsConfigPath));
            LoadJobs();
        }

        // Constructor with FileManager dependency
        public BackupManager(FileManager fileManager, Logger logger = null)
        {
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _configManager = new ConfigManager();
            _logger = logger ?? new Logger();

            _jobsConfigPath = Path.Combine(@"C:\EasySave\Config", "jobs.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_jobsConfigPath));
            LoadJobs();
        }

        // Access the CryptoSoftManager from the FileManager
        public CryptoSoftManager GetCryptoSoftManager()
        {
            return _fileManager?.GetCryptoSoftManager();
        }

        public bool AddBackupJob(BackupJob job)
        {
            if (job == null) return false;

            if (backupJobs.Any(j => j.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            backupJobs.Add(job);
            SaveJobs();
            _logger.UpdateJobStatus(job.Name, Enums.JobState.PENDING, 0.0f);

            return true;
        }

        public bool RemoveBackupJob(string name)
        {
            var job = backupJobs.FirstOrDefault(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (job != null)
            {
                backupJobs.Remove(job);
                SaveJobs();
                return true;
            }

            return false;
        }

        // Ensure that GetBackupJob returns the correct instance and does not share state between jobs
        public BackupJob GetBackupJob(string name)
        {
            // This should return the job instance with the given name
            return backupJobs.FirstOrDefault(j => j.Name == name);
        }

        public List<BackupJob> GetAllJobs()
        {
            return backupJobs.ToList();
        }

        public bool ExecuteBackupJob(string name)
        {
            BackupJob job = GetBackupJob(name);
            if (job != null)
            {
                // Get the list of files to transfer for this job
                var filesToTransfer = job.GetFilesToTransfer(); // You may need to implement this method

                job.SetFileManager(_fileManager);
                
                // Launch job in a background task
                var jobTask = Task.Run(() => job.Execute());
                
                // Store the task in the concurrent dictionary
                _runningTasks[name] = jobTask;
                
                // Return true to indicate the job was launched successfully
                return true;
            }
            return false;
        }

        public bool ExecuteAllBackupJobs()
        {
            // Gather all priority files from all jobs, using the current (ordered) priority extensions
            var priorityExtensions = _configManager.GetPriorityExtensions();
            var allPriorityFiles = new List<string>();
            foreach (var job in backupJobs)
            {
                var files = job.GetAllFilesToBackup();
                foreach (var ext in priorityExtensions)
                    allPriorityFiles.AddRange(files.Where(f => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
            }
            FileManager.RegisterPriorityFiles(allPriorityFiles);

            // Create a list to track all tasks
            List<Task<bool>> jobTasks = new List<Task<bool>>();

            foreach (BackupJob job in backupJobs)
            {
                job.SetFileManager(_fileManager);
                
                // Create a task for each job
                var jobTask = Task.Run(() => job.Execute());
                
                // Store the task reference
                _runningTasks[job.Name] = jobTask;
                
                // Add to our tracking list
                jobTasks.Add(jobTask);
            }

            // Return true immediately since jobs are now running in parallel
            // The actual results will be available in jobTasks when they complete
            return true;
        }

        public void PauseJob(string name)
        {
            BackupJob job = GetBackupJob(name);
            job?.Pause();
        }

        public void ResumeJob(string name)
        {
            BackupJob job = GetBackupJob(name);
            if (job != null)
            {
                // Ensure the FileManager is set before resuming
                job.SetFileManager(_fileManager);
                
                // First change the job state with Resume()
                job.Resume();
                
                System.Diagnostics.Debug.WriteLine($"[BackupManager] Job '{job.Name}' resumed successfully.");
                
                // After resuming, create a new task to continue execution
                // This keeps the execution model consistent with ExecuteBackupJob
                var jobTask = Task.Run(() => job.Execute());
                
                // Store the task in the concurrent dictionary to track it
                _runningTasks[name] = jobTask;
            }
        }

        /// <summary>
        /// Resumes a paused backup job using the new ResumeExecution method.
        /// </summary>
        /// <param name="name">The name of the job to resume.</param>
        public void ResumeBackupJob(string name)
        {
            BackupJob job = GetBackupJob(name);
            if (job != null)
            {
                // Ensure the FileManager is set before resuming
                job.SetFileManager(_fileManager);

                // Set state/flags for resume
                job.PrepareResume();

                System.Diagnostics.Debug.WriteLine($"[BackupManager] Job '{job.Name}' resume requested.");

                // Start a new Task to continue execution using Execute (not ResumeExecution)
                var jobTask = Task.Run(() => job.Execute());

                // Store the task in the concurrent dictionary to track it
                _runningTasks[name] = jobTask;
            }
        }

        public void StopJob(string name)
        {
            BackupJob job = GetBackupJob(name);
            job?.Stop();
        }

        public void SaveJobs()
        {
            try
            {
                //if (job.State == Enums.JobState.PAUSED) { }
                    var jobsData = backupJobs.Select(j => new

                {
                    j.Name,
                    j.SourceDirectory,
                    j.TargetDirectory,
                    Type = j.Type.ToString(),
                    j.EncryptFiles,
                    ExtensionsToEncrypt = j.ExtensionsToEncrypt,
                    BlockedProcesses = j.BlockedProcesses,
                    State = j.State.ToString(),
                    j.LastRunTime,
                    j.Progress
                }).ToList();

                string jsonString = JsonSerializer.Serialize(jobsData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_jobsConfigPath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving jobs: {ex.Message}");
                _logger.LogError("BackupManager", $"Error saving jobs: {ex.Message}");
            }
        }

        public void LoadJobs()
        {
            try
            {
                if (!File.Exists(_jobsConfigPath))
                {
                    return;
                }

                string jsonString = File.ReadAllText(_jobsConfigPath);
                var jobsData = JsonSerializer.Deserialize<List<JobData>>(jsonString);

                backupJobs.Clear();

                foreach (var data in jobsData)
                {
                    Enum.TryParse(data.Type, out Enums.BackupType type);
                    Enum.TryParse(data.State, out Enums.JobState state);

                    var job = new BackupJob(
                        data.Name,
                        data.SourceDirectory,
                        data.TargetDirectory,
                        type,
                        data.EncryptFiles,
                        data.ExtensionsToEncrypt,
                        data.BlockedProcesses
                    );
                    
                    job.State = state;
                    job.LastRunTime = data.LastRunTime;
                    job.Progress = data.Progress;

                    backupJobs.Add(job);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading jobs: {ex.Message}");
                _logger.LogError("BackupManager", $"Error loading jobs: {ex.Message}");
            }
        }

        public void WaitForLargeFileSlot(long fileSizeBytes)
        {
            if (fileSizeBytes > BandwidthLimitKB * 1024)
            {
                lock (_largeFileLock)
                {
                    _largeFileTransferring = false;
                }
            }
        }

        private void TransferFileWithBandwidthLimit(string source, string dest)
        {
            var fileInfo = new FileInfo(source);
            long fileSize = fileInfo.Length;
            bool isLarge = fileSize > BandwidthLimitKB * 1024;

            if (isLarge)
            {
                lock (_largeFileLock)
                {
                    while (_largeFileTransferring)
                    {
                        Monitor.Wait(_largeFileLock);
                    }
                    _largeFileTransferring = true;
                }
            }

            try
            {
                // ...perform file transfer...
            }
            finally
            {
                if (isLarge)
                {
                    lock (_largeFileLock)
                    {
                        _largeFileTransferring = false;
                        Monitor.PulseAll(_largeFileLock);
                    }
                }
            }
        }

        public bool IsLargeFileTransferInProgress()
        {
            lock (_largeFileLock)
            {
                return _largeFileTransferring;
            }
        }

        public void EnsureCanStartBackupJob(IEnumerable<string> filesToTransfer)
        {
            // If any file in the job is larger than the limit, check if a large file is already being transferred
            long limitBytes = BandwidthLimitKB * 1024;
            bool hasLargeFile = filesToTransfer.Any(f => new FileInfo(f).Length > limitBytes);
            if (hasLargeFile)
            {
                lock (_largeFileLock)
                {
                    if (_largeFileTransferring)
                        throw new BandwidthLimitExceededException("A large file transfer is already in progress. Please wait until it finishes before starting another backup job with large files.");
                }
            }
        }

        private class JobData
        {
            public string Name { get; set; }
            public string SourceDirectory { get; set; }
            public string TargetDirectory { get; set; }
            public string Type { get; set; }
            public string State { get; set; }
            public DateTime LastRunTime { get; set; }
            public float Progress { get; set; }
            public bool EncryptFiles { get; set; }
            public List<string> ExtensionsToEncrypt { get; set; }
            public List<string> BlockedProcesses { get; set; }
        }
    }

    // Add a new exception for bandwidth violation
    public class BandwidthLimitExceededException : Exception
    {
        public BandwidthLimitExceededException(string message) : base(message) { }
    }
}
