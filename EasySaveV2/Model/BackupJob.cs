using System;
using System.Collections.Generic;

namespace EasySaveV2.Model
{
    public class BackupJob
    {
        public string Name { get; set; } = string.Empty;
        public string SourceDirectory { get; set; } = string.Empty;
        public string TargetDirectory { get; set; } = string.Empty;
        public JobState State { get; set; }
        public List<string> ExtensionsToEncrypt { get; set; } = new List<string>();
        public List<string> BlockedProcesses { get; set; } = new List<string>();
        public long TotalSize { get; set; }
        public long ProcessedSize { get; set; }
        public int FilesProcessed { get; set; }
        public int TotalFiles { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ErrorMessage { get; set; }

        public BackupJob()
        {
            State = JobState.WAITING;
            StartTime = DateTime.Now;
        }

        public float GetProgress()
        {
            if (TotalSize == 0) return 0;
            return (float)ProcessedSize / TotalSize * 100;
        }

        public TimeSpan GetDuration()
        {
            var end = EndTime ?? DateTime.Now;
            return end - StartTime;
        }
    }
} 