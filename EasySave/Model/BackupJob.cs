using System;
using System.IO;
using EasySave.Model.Enums;

namespace EasySave.Model
{
  
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

 
        private bool _isPaused;
        private bool _isStopped;

       
        public BackupJob(string name, string sourceDirectory, string targetDirectory, BackupType type)
        {
           
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Job name cannot be empty", nameof(name));

            if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
                throw new ArgumentException("Source directory is invalid or does not exist", nameof(sourceDirectory));

            if (string.IsNullOrEmpty(targetDirectory))
                throw new ArgumentException("Target directory cannot be empty", nameof(targetDirectory));

            Name = name;
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            Type = type;
            State = JobState.PENDING;
            Progress = 0.0f;

           
            _fileManager = new FileManager();

            
            _isPaused = false;
            _isStopped = false;
        }

   
        public bool Execute()
        {
            try
            {
                
                State = JobState.RUNNING;
                _isPaused = false;
                _isStopped = false;
                Progress = 0.0f;

               
                var logger = new Logger();
                logger.UpdateJobStatus(Name, State, Progress);

               
                if (!Directory.Exists(TargetDirectory))
                {
                    Directory.CreateDirectory(TargetDirectory);
                }

               
                bool isFullBackup = (Type == BackupType.FULL);
                long result = _fileManager.CopyDirectory(
                    SourceDirectory,
                    TargetDirectory,
                    isFullBackup,
                    onProgressUpdate: (progress) => {
                        Progress = progress;
                        logger.UpdateJobStatus(Name, State, Progress);

                        
                        if (_isPaused)
                        {
                            while (_isPaused && !_isStopped)
                            {
                                System.Threading.Thread.Sleep(500);
                            }
                        }
                        return !_isStopped; 
                    },
                    logger: logger, 
                    jobName: Name
                );

      
                LastRunTime = DateTime.Now;

                if (_isStopped)
                {
                    State = JobState.PENDING;
                }
                else
                {
                    State = (result >= 0) ? JobState.COMPLETED : JobState.FAILED;
                    Progress = (result >= 0) ? 100.0f : Progress;
                }

                logger.UpdateJobStatus(Name, State, Progress);

                return State == JobState.COMPLETED;
            }
            catch (Exception ex)
            {
                State = JobState.FAILED;
                Progress = 0.0f;

           
                var logger = new Logger();
                logger.UpdateJobStatus(Name, State, Progress);
                logger.LogError(Name, ex.Message);

                return false;
            }
        }

     
        public void Pause()
        {
            if (State == JobState.RUNNING)
            {
                _isPaused = true;
                State = JobState.PAUSED;
                var statusLogger = new Logger();
                statusLogger.UpdateJobStatus(Name, State, Progress);
            }
        }

      
        public void Resume()
        {
            if (State == JobState.PAUSED)
            {
                _isPaused = false;
                State = JobState.RUNNING;
                var statusLogger = new Logger();
                statusLogger.UpdateJobStatus(Name, State, Progress);
            }
        }

        public void Stop()
        {
            if (State == JobState.RUNNING || State == JobState.PAUSED)
            {
                _isStopped = true;
                _isPaused = false;
                State = JobState.PENDING;
                var statusLogger = new Logger();
                statusLogger.UpdateJobStatus(Name, State, Progress);
            }
        }

        public float GetProgress()
        {
            return Progress;
        }

        public JobState GetState()
        {
            return State;
        }
    }
}
    