using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace EasySaveV2.Model
{
    public class ParallelBackupManager
    {
        private readonly ConfigManager _configManager;
        private readonly ConcurrentDictionary<string, BackupJob> _activeJobs;
        private readonly SemaphoreSlim _largeFileSemaphore;
        private readonly SemaphoreSlim _parallelJobsSemaphore;
        private readonly object _priorityLock = new object();
        private readonly NetworkMonitor _networkMonitor;

        public event EventHandler<BackupProgressEventArgs>? ProgressUpdated;
        public event EventHandler<BackupStateEventArgs>? StateChanged;
        public event EventHandler<string>? ErrorOccurred;

        public ParallelBackupManager(ConfigManager configManager)
        {
            _configManager = configManager;
            _activeJobs = new ConcurrentDictionary<string, BackupJob>();
            _largeFileSemaphore = new SemaphoreSlim(1, 1); // Only one large file at a time
            _parallelJobsSemaphore = new SemaphoreSlim(_configManager.GetMaxParallelJobs(), _configManager.GetMaxParallelJobs());
            _networkMonitor = new NetworkMonitor(_configManager.GetNetworkLoadThreshold());
            _networkMonitor.NetworkLoadChanged += OnNetworkLoadChanged;
        }

        public async Task StartBackupJob(BackupJob job)
        {
            if (_activeJobs.ContainsKey(job.Name))
            {
                throw new InvalidOperationException($"Job {job.Name} is already running");
            }

            _activeJobs.TryAdd(job.Name, job);
            job.State = JobState.RUNNING;

            try
            {
                await _parallelJobsSemaphore.WaitAsync();
                await ProcessBackupJob(job);
            }
            finally
            {
                _parallelJobsSemaphore.Release();
                _activeJobs.TryRemove(job.Name, out _);
            }
        }

        private async Task ProcessBackupJob(BackupJob job)
        {
            var files = GetFilesToBackup(job.SourceDirectory);
            var priorityFiles = files.Where(f => IsPriorityFile(f)).ToList();
            var nonPriorityFiles = files.Where(f => !IsPriorityFile(f)).ToList();

            // Process priority files first
            foreach (var file in priorityFiles)
            {
                if (job.State == JobState.PAUSED)
                {
                    await WaitForResume(job);
                }
                if (job.State == JobState.STOPPED)
                {
                    return;
                }

                await ProcessFile(job, file);
            }

            // Then process non-priority files
            foreach (var file in nonPriorityFiles)
            {
                if (job.State == JobState.PAUSED)
                {
                    await WaitForResume(job);
                }
                if (job.State == JobState.STOPPED)
                {
                    return;
                }

                await ProcessFile(job, file);
            }
        }

        private async Task ProcessFile(BackupJob job, string file)
        {
            var fileInfo = new FileInfo(file);
            bool isLargeFile = fileInfo.Length > _configManager.GetBandwidthLimit() * 1024;

            if (isLargeFile)
            {
                await _largeFileSemaphore.WaitAsync();
                try
                {
                    await CopyFile(job, file);
                }
                finally
                {
                    _largeFileSemaphore.Release();
                }
            }
            else
            {
                await CopyFile(job, file);
            }
        }

        private async Task CopyFile(BackupJob job, string sourceFile)
        {
            string relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
            string targetFile = Path.Combine(job.TargetDirectory, relativePath);
            string targetDir = Path.GetDirectoryName(targetFile)!;
            Directory.CreateDirectory(targetDir);

            try
            {
                using var source = File.OpenRead(sourceFile);
                using var target = File.Create(targetFile);
                await source.CopyToAsync(target);

                job.ProcessedSize += new FileInfo(sourceFile).Length;
                job.FilesProcessed++;
                UpdateProgress(job);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Error copying file {sourceFile}: {ex.Message}");
            }
        }

        private bool IsPriorityFile(string file)
        {
            string extension = Path.GetExtension(file).ToLower();
            return _configManager.GetPriorityExtensions().Contains(extension);
        }

        private IEnumerable<string> GetFilesToBackup(string sourceDir)
        {
            return Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
        }

        private void UpdateProgress(BackupJob job)
        {
            var args = new BackupProgressEventArgs
            {
                JobName = job.Name,
                Progress = job.GetProgress()
            };
            ProgressUpdated?.Invoke(this, args);
        }

        private async Task WaitForResume(BackupJob job)
        {
            while (job.State == JobState.PAUSED)
            {
                await Task.Delay(100);
            }
        }

        private void OnNetworkLoadChanged(object? sender, int networkLoad)
        {
            if (networkLoad > _configManager.GetNetworkLoadThreshold())
            {
                // Reduce number of parallel jobs
                _parallelJobsSemaphore.Release();
            }
            else
            {
                // Restore original number of parallel jobs
                _parallelJobsSemaphore.Release();
            }
        }

        public void PauseJob(string jobName)
        {
            if (_activeJobs.TryGetValue(jobName, out var job))
            {
                job.State = JobState.PAUSED;
                StateChanged?.Invoke(this, new BackupStateEventArgs { JobName = jobName, State = JobState.PAUSED });
            }
        }

        public void ResumeJob(string jobName)
        {
            if (_activeJobs.TryGetValue(jobName, out var job))
            {
                job.State = JobState.RUNNING;
                StateChanged?.Invoke(this, new BackupStateEventArgs { JobName = jobName, State = JobState.RUNNING });
            }
        }

        public void StopJob(string jobName)
        {
            if (_activeJobs.TryGetValue(jobName, out var job))
            {
                job.State = JobState.STOPPED;
                StateChanged?.Invoke(this, new BackupStateEventArgs { JobName = jobName, State = JobState.STOPPED });
            }
        }
    }

    public class BackupProgressEventArgs : EventArgs
    {
        public string JobName { get; set; } = string.Empty;
        public float Progress { get; set; }
    }

    public class BackupStateEventArgs : EventArgs
    {
        public string JobName { get; set; } = string.Empty;
        public JobState State { get; set; }
    }
} 