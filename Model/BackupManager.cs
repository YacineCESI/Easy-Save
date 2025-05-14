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
        private List<BackupJob> _backupJobs;
        private const int MaxJobs = 5;
        private readonly ConfigManager _configManager;
        private readonly string _jobsConfigpath;

        public BackupManager()
        {
            _backupJobs = new List<BackupJob>();
            _configManager = new ConfigManager();
            _jobsConfigpath = Path.Combine(Directory.GetCurrentDirectory(), "jobs.json");

            Directory.CreateDirectory(Path.GetDirectoryName(_jobsConfigpath));
            LoadJobs();
            Console.WriteLine($"[DEBUG] BackupManager initialized. Jobs loaded: {_backupJobs.Count}");
        }

        public bool AddBackupJob(BackupJob job)
        {
            // Check if max jobs limit reached
            if (_backupJobs.Count >= MaxJobs)
            {
                Console.WriteLine($"[DEBUG] AddBackupJob: Job '{job.Name}' added: false. Total jobs: {_backupJobs.Count}");
                return false;
            }

            // Check if job with same name already exists
            if (_backupJobs.Any(j => j.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"[DEBUG] AddBackupJob: Job '{job.Name}' added: false. Total jobs: {_backupJobs.Count}");
                return false;
            }

            _backupJobs.Add(job);
            SaveJobs();
            Console.WriteLine($"[DEBUG] AddBackupJob: Job '{job.Name}' added: true. Total jobs: {_backupJobs.Count}");
            return true;
        }

        public bool RemoveBackupJob(string name)
        {
            return false;
        }

        public BackupJob GetBackupJob(string name)
        {
            throw new NotImplementedException();
        }

        public List<BackupJob> GetAllJobs()
        {
            // Debug: Log the count of jobs
            Console.WriteLine($"[DEBUG] BackupManager: Returning {_backupJobs.Count} jobs.");
            return _backupJobs.ToList();
        }

        public bool ExecuteBackupJob(string name)
        {
            return false;
        }

        public bool ExecuteAllBackupJobs()
        {
            return false;
        }

        public void PauseJob(string name)
        { }

        public void ResumeJob(string name)
        { }

        public void StopJob(string name)
        { }

        public void SaveJobs()
        {
            try
            {
                // Create a serializable format for jobs
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

                File.WriteAllText(_jobsConfigpath, jsonString);
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
                if (!File.Exists(_jobsConfigpath))
                {
                    return;
                }
                string jsonString = File.ReadAllText(_jobsConfigpath);
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
                // Handle exceptions (e.g., log them)
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