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
        private LanguageManager _languageManager;
        private ConfigManager _configManager;
        private Logger _logger;

        /// <summary>
        /// Constructor for MainViewModel
        /// </summary>
        public MainViewModel()
        {
            _backupManager = new BackupManager();
            _languageManager = new LanguageManager();
            _configManager = new ConfigManager();
            _logger = new Logger();
        }

        /// <summary>
        /// Get a translated string by key
        /// </summary>
        public string GetString(string key)
        {
            return _languageManager.GetString(key);
        }

        /// <summary>
        /// Get all available languages
        /// </summary>
        public List<string> GetAvailableLanguages()
        {
            return _languageManager.GetAvailableLanguages();
        }

        /// <summary>
        /// Get the current language
        /// </summary>
        public string GetCurrentLanguage()
        {
            return _languageManager.GetCurrentLanguage();
        }

        /// <summary>
        /// Change the application language
        /// </summary>
        public bool ChangeLanguage(string language)
        {
            return _languageManager.SwitchLanguage(language);
        }

        /// <summary>
        /// Create a new backup job
        /// </summary>
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

        /// <summary>
        /// Get all backup jobs
        /// </summary>
        public List<BackupJob> GetAllJobs()
        {
            return _backupManager.GetAllJobs();
        }

        /// <summary>
        /// Execute a backup job by name
        /// </summary>
        public bool ExecuteBackupJob(string jobName)
        {
            return _backupManager.ExecuteBackupJob(jobName);
        }

        /// <summary>
        /// Execute all backup jobs
        /// </summary>
        public bool ExecuteAllBackupJobs()
        {
            return _backupManager.ExecuteAllBackupJobs();
        }

        /// <summary>
        /// Pause a specific backup job
        /// </summary>
        public void PauseJob(string name)
        {
            _backupManager.PauseJob(name);
        }

        /// <summary>
        /// Resume a specific backup job
        /// </summary>
        public void ResumeJob(string name)
        {
            _backupManager.ResumeJob(name);
        }

        /// <summary>
        /// Stop a specific backup job
        /// </summary>
        public void StopJob(string name)
        {
            _backupManager.StopJob(name);
        }

        /// <summary>
        /// Get a specific backup job by name
        /// </summary>
        public BackupJob GetJob(string name)
        {
            return _backupManager.GetBackupJob(name);
        }

        /// <summary>
        /// Remove a backup job by name
        /// </summary>
        public bool RemoveJob(string name)
        {
            return _backupManager.RemoveBackupJob(name);
        }

        /// <summary>
        /// Get the daily log file path
        /// </summary>
        public string GetDailyLogPath()
        {
            return _logger.GetDailyLogPath();
        }

        /// <summary>
        /// Get the status log file path
        /// </summary>
        public string GetStatusLogPath()
        {
            return _logger.GetStatusLogPath();
        }
    }
}
