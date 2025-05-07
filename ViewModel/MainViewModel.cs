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
        { }

        public string GetString(string key)
        { }

        public List<string> GetAvailableLanguages()
        { }

        public string GetCurrentLanguage()
        { }

        public bool ChangeLanguage(string language)
        { }

        public bool CreateBackupJob(string name, string sourceDir, string targetDir, BackupType type)
        { }

        public List<BackupJob> GetAllJobs()
        { }

        public bool ExecuteBackupJob(string jobName)
        { }

        public bool ExecuteAllBackupJobs()
        { }

        public void PauseJob(string name)
        { }

        public void ResumeJob(string name)
        { }

        public void StopJob(string name)
        { }

        public BackupJob GetJob(string name)
        { }

        public bool RemoveJob(string name)
        { }

        public string GetDailyLogPath()
        { }

        public string GetStatusLogPath()
        { }


        }
}