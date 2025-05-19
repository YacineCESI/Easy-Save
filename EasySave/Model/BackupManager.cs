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
        private List<BackupJob> _backupJobs = new();
        private Queue<BackupJob> _jobQueue = new();
        private readonly ConfigManager _configManager;
        private readonly Logger _logger;
        private readonly BusinessSoftwareManager _businessSoftwareManager;
        private readonly string _jobsConfigPath;

        public BackupManager()
        {
            _configManager = new ConfigManager();
            _logger = new Logger();
            _businessSoftwareManager = new BusinessSoftwareManager(_configManager);
            _jobsConfigPath = Path.Combine(@"C:\EasySave\Config", "jobs.json");

            Directory.CreateDirectory(Path.GetDirectoryName(_jobsConfigPath));
            LoadJobs();
        }

        public bool AddBackupJob(BackupJob job)
        {
            if (_backupJobs.Any(j => j.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase)))
                return false;

            _backupJobs.Add(job);
            SaveJobs();
            _logger.UpdateJobStatus(job.Name, Enums.JobState.PENDING, 0.0f);
            return true;
        }

        public bool RemoveBackupJob(string name)
        {
            var job = _backupJobs.FirstOrDefault(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (job == null) return false;

            _backupJobs.Remove(job);
            SaveJobs();
            return true;
        }

        public BackupJob GetBackupJob(string name) => _backupJobs.FirstOrDefault(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public List<BackupJob> GetAllJobs() => new List<BackupJob>(_backupJobs);

        public bool ExecuteBackupJob(string name)
        {
            if (_businessSoftwareManager.IsBusinessSoftwareRunning())
                return false;

            var job = GetBackupJob(name);
            if (job == null) return false;

            return job.Execute();
        }

        public bool ExecuteAllBackupJobs()
        {
            if (_businessSoftwareManager.IsBusinessSoftwareRunning())
                return false;

            foreach (var job in _backupJobs)
            {
                if (_businessSoftwareManager.IsBusinessSoftwareRunning())
                    break;

                job.Execute();
            }
            return true;
        }

        public void PauseJob(string name)
        {
            var job = GetBackupJob(name);
            job?.Pause();
        }

        public void ResumeJob(string name)
        {
            var job = GetBackupJob(name);
            job?.Resume();
        }

        public void StopJob(string name)
        {
            var job = GetBackupJob(name);
            job?.Stop();
        }

        public bool CheckBusinessSoftwareRunning() => _businessSoftwareManager.IsBusinessSoftwareRunning();

        public void SaveJobs()
        {
            try
            {
                var jobsData = _backupJobs.Select(j => new
                {
                    Name = j.Name,
                    SourceDirectory = j.SourceDirectory,
                    TargetDirectory = j.TargetDirectory,
                    Type = j.Type.ToString()
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

                _backupJobs.Clear();

                foreach (var data in jobsData)
                {
                    Enum.TryParse(data.Type, out Enums.BackupType type);

                    _backupJobs.Add(new BackupJob(
                        data.Name,
                        data.SourceDirectory,
                        data.TargetDirectory,
                        type
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading jobs: {ex.Message}");
            }
        }

        private class JobData
        {
            public string Name { get; set; }
            public string SourceDirectory { get; set; }
            public string TargetDirectory { get; set; }
            public string Type { get; set; }
        }
    }
}
