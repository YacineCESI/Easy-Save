using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasySave.Model
{
    public class BackupManager
    {
        private List<BackupJob> backupJobs = new();
        private Queue<BackupJob> jobQueue = new();
        private readonly ConfigManager _configManager;
        private readonly string _jobsConfigPath;
        private readonly Logger _logger;
        private readonly FileManager _fileManager;

        // Default constructor
        public BackupManager()
        {
            _configManager = new ConfigManager();
            _logger = new Logger();

            // Initialize FileManager with a new CryptoSoftManager
            _fileManager = new FileManager(
                new CryptoSoftManager(_configManager.GetCryptoSoftPath()),
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

        public BackupJob GetBackupJob(string name)
        {
            return backupJobs.FirstOrDefault(j =>
                j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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
                job.SetFileManager(_fileManager);
                return job.Execute();
            }
            return false;
        }

        public bool ExecuteAllBackupJobs()
        {
            bool allSuccess = true;

            foreach (BackupJob job in backupJobs)
            {
                job.SetFileManager(_fileManager);
                allSuccess &= job.Execute();
            }

            return allSuccess;
        }

        public void PauseJob(string name)
        {
            BackupJob job = GetBackupJob(name);
            job?.Pause();
        }

        public void ResumeJob(string name)
        {
            BackupJob job = GetBackupJob(name);
            job?.Resume();
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
}
