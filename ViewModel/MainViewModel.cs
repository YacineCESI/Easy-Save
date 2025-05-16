using System;
using System.Collections.Generic;
using EasySave.Model;
using EasySave.Model.Enums;

namespace EasySave.ViewModel
{
    /// <summary>
    /// Main view model that coordinates the application logic
    /// </summary>
    public class MainViewModel
    {
        private BackupManager _backupManager;
       // private LanguageManager _languageManager;
    //    private ConfigManager _configManager;
    //    private Logger _logger;

        /// <summary>
        /// Constructor for MainViewModel
        /// </summary>
        public MainViewModel()
        { 
            _backupManager = new BackupManager();
        }
        /*
        public new string GetString(string key)
        {
            return key;
        }
        
        public List<string> GetAvailableLanguages()
        { }

        public string GetCurrentLanguage()
        { }

        public bool ChangeLanguage(string language)
        { }
        */
        public bool CreateBackupJob(string name, string sourceDir, string targetDir, BackupType type)
        {
            try
            {
                if (!System.IO.Directory.Exists(sourceDir))
                {
                    return false;
                }   
                var job = new BackupJob(name, sourceDir, targetDir, type);
                bool result = _backupManager.AddBackupJob(job);
                return result;
            }
            catch
            {
                return false;
            }
        }

        public List<BackupJob> GetAllJobs()
        {
            var jobs = _backupManager.GetAllJobs();
            return jobs;
        }
        public bool ExecuteBackupJob(string jobName)
        {
            return _backupManager.ExecuteBackupJob(jobName);
        }

       
        public bool ExecuteAllBackupJobs()
        {
            return _backupManager.ExecuteAllBackupJobs();
        }

        public bool RemoveJob(string name)
        {
            return _backupManager.RemoveBackupJob(name);
        }


        /*

        public void PauseJob(string name)
        { }

        public void ResumeJob(string name)
        { }

        public void StopJob(string name)
        { }

        public BackupJob GetJob(string name)
        { }

       
        public string GetDailyLogPath()
        { }

        public string GetStatusLogPath()
        { }

        */
    }
}