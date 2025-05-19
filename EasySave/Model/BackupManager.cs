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
        private readonly string _jobsConfigPath;
        private readonly Logger _logger; 

       
        public BackupManager()
        {
            _backupJobs = new List<BackupJob>();
            _configManager = new ConfigManager();
            _logger = new Logger(); 

            _jobsConfigPath = Path.Combine(@"C:\EasySave\Config", "jobs.json");

           
            Directory.CreateDirectory(Path.GetDirectoryName(_jobsConfigPath));

          
            LoadJobs();
        }

      
        public bool AddBackupJob(BackupJob job)
        {
            
            if (_backupJobs.Count >= MaxJobs)
            {
                return false;
            }

            
            if (_backupJobs.Any(j => j.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            _backupJobs.Add(job);
            SaveJobs();

           
            _logger.UpdateJobStatus(job.Name, Enums.JobState.PENDING, 0.0f);

            return true;
        }

       
        public bool RemoveBackupJob(string name)
        {
            int initialCount = _backupJobs.Count;
            _backupJobs.RemoveAll(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (_backupJobs.Count < initialCount)
            {
                SaveJobs();
                return true;
            }

            return false;
        }

        
        public BackupJob GetBackupJob(string name)
        {
            return _backupJobs.FirstOrDefault(j =>
                j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

       
        public List<BackupJob> GetAllJobs()
        {
            return _backupJobs.ToList();
        }

      
        public bool ExecuteBackupJob(string name)
        {
            BackupJob job = GetBackupJob(name);
            if (job != null)
            {
                return job.Execute();
            }
            return false;
        }

        public bool ExecuteAllBackupJobs()
        {
            bool allSuccess = true;

            foreach (BackupJob job in _backupJobs)
            {
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
