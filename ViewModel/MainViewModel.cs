using System;
using System.Collections.Generic;
using EasySave.Model;
using EasySave.Model.Enums;

namespace EasySave.ViewModel
{
    public class MainViewModel
    {
        private BackupManager _backupManager;

        private ConfigManager _configManager;
        private Logger _logger;


        public MainViewModel()
        {
            _backupManager = new BackupManager();

            _configManager = new ConfigManager();
            _logger = new Logger();
        }


       /* public string GetString(string key)
        {
     
        }
  
        public List<string> GetAvailableLanguages()
        {
            return _languageManager.GetAvailableLanguages();
        }

        public string GetCurrentLanguage()
        {
            return _languageManager.GetCurrentLanguage();
        }

 
        public bool ChangeLanguage(string language)
        {
            return _languageManager.SwitchLanguage(language);
        }     */

      
        public bool CreateBackupJob(string name, string sourceDir, string targetDir, BackupType type)
        {
            try
            {
                if (!System.IO.Directory.Exists(sourceDir))
                {
                    return false;
                }

                var job = new BackupJob(name, sourceDir, targetDir, type);
                return _backupManager.AddBackupJob(job);
            }
            catch
            {
                return false;
            }
        }

  
        public List<BackupJob> GetAllJobs()
        {
            return _backupManager.GetAllJobs();
        }

        public bool ExecuteBackupJob(string jobName)
        {
            return _backupManager.ExecuteBackupJob(jobName);
        }

        public bool ExecuteAllBackupJobs()
        {
            return _backupManager.ExecuteAllBackupJobs();
        }


        public void PauseJob(string name)
        {
            _backupManager.PauseJob(name);
        }


        public void ResumeJob(string name)
        {
            _backupManager.ResumeJob(name);
        }

>
        public void StopJob(string name)
        {
            _backupManager.StopJob(name);
        }

 
        public BackupJob GetJob(string name)
        {
            return _backupManager.GetBackupJob(name);
        }


        public bool RemoveJob(string name)
        {
            return _backupManager.RemoveBackupJob(name);
        }

    
  
        public string GetDailyLogPath()
        {
            return _logger.GetDailyLogPath();
        }

 
        public string GetStatusLogPath()
        {
            return _logger.GetStatusLogPath();
        }
    }
}