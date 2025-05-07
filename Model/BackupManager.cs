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

        public BackupManager()
        { }

        public bool AddBackupJob(BackupJob job)
        { }

        public bool RemoveBackupJob(string name)
        { }

        public BackupJob GetBackupJob(string name)
        { }
        public List<BackupJob> GetAllJobs()
        { }

        public bool ExecuteBackupJob(string name)
        { }

        public bool ExecuteAllBackupJobs()
        { }

        public void PauseJob(string name)
        { }

        public void ResumeJob(string name)
        { }

        public void StopJob(string name)
        { }

        public void SaveJobs()
        { }

        public void LoadJobs()
        { }

        private class JobData
        { }

        }
}