using System;
using System.IO;
using EasySave.Model.Enums;

namespace EasySave.Model
{
    /// Represents a backup job with source, destination and execution state
    public class BackupJob
    {
        public string Name { get; private set; }
        public string SourceDirectory { get; private set; }
        public string TargetDirectory { get; private set; }
        public BackupType Type { get; private set; }
        public JobState State { get; private set; }
        public DateTime LastRunTime { get; private set; }
        public float Progress { get; private set; }

        private FileManager _fileManager;
        private Logger _logger;

        private bool _isPaused;
        private bool _isStopped;

        public BackupJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        { }

        public bool Excute()
        { }

        public void Pause()
        { }

        public void Resume()
        { }

        public void Stop()
        { } 
        
        public float GetProgress()
        { }

        public JobState GetState()
        { }



    }
}