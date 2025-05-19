using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using EasySave.Model.Enums;

namespace EasySave.Model
{
  
    public class Logger
    {
        private string _dailyLogPath;
        private string _statusLogPath;

  
        public Logger()
        {
          
            string baseLogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

         
            Directory.CreateDirectory(baseLogDirectory);

            string dailyLogName = $"DailyLog_{DateTime.Now:yyyy-MM-dd}.json";
            _dailyLogPath = Path.Combine(baseLogDirectory, dailyLogName);

          
            _statusLogPath = Path.Combine(baseLogDirectory, "Status.json");

        
            EnsureLogExists();
        }

  
        public void LogAction(string jobName, string sourceFile, string destinationFile, long fileSize, long transferTime)
        {
            try
            {
          
                string uncSource = ToUncPath(sourceFile);
                string uncDestination = ToUncPath(destinationFile);

                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = jobName,
                    SourcePath = uncSource,
                    DestinationPath = uncDestination,
                    FileSize = fileSize,
                    TransferTime = transferTime
                };

                
                string jsonEntry = FormatLogEntry(logEntry);

             
                File.AppendAllText(_dailyLogPath, jsonEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging action: {ex.Message}");
            }
        }

        public void LogError(string jobName, string errorMessage)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = jobName,
                    SourcePath = "ERROR",
                    DestinationPath = "ERROR",
                    FileSize = 0,
                    TransferTime = -1,
                    ErrorMessage = errorMessage
                };

     
                string jsonEntry = FormatLogEntry(logEntry);


                File.AppendAllText(_dailyLogPath, jsonEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging error: {ex.Message}");
            }
        }


        public void UpdateJobStatus(string jobName, JobState status, float progress)
        {
            try
            {
          
                Dictionary<string, JobStatus> statuses = new Dictionary<string, JobStatus>();
                if (File.Exists(_statusLogPath))
                {
                    string json = File.ReadAllText(_statusLogPath);
                    try
                    {
                        statuses = JsonSerializer.Deserialize<Dictionary<string, JobStatus>>(json) ??
                            new Dictionary<string, JobStatus>();
                    }
                    catch
                    {
              
                        statuses = new Dictionary<string, JobStatus>();
                    }
                }

           
                statuses[jobName] = new JobStatus
                {
                    State = status.ToString(),
                    Progress = progress,
                    LastUpdated = DateTime.Now
                };

              
                string updatedJson = JsonSerializer.Serialize(statuses, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_statusLogPath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating job status: {ex.Message}");
            }
        }

       
        public string GetDailyLogPath()
        {
            return _dailyLogPath;
        }

   
        public string GetStatusLogPath()
        {
            return _statusLogPath;
        }

       
        private string FormatLogEntry(LogEntry entry)
        {
            return JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }


        private void EnsureLogExists()
        {
            try
            {
               
                if (!File.Exists(_dailyLogPath))
                {
                    File.WriteAllText(_dailyLogPath, "");
                }

             
                if (!File.Exists(_statusLogPath))
                {
                    File.WriteAllText(_statusLogPath, "{}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring logs exist: {ex.Message}");
            }
        }

  
        private string ToUncPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

           
            if (path.StartsWith(@"\\"))
                return path;
 
  
            
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return path;

      
            try
            {
                string root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root))
                    return path;

               
                var sb = new System.Text.StringBuilder(512);
                int size = sb.Capacity;
                int result = WNetGetConnection(root.TrimEnd('\\'), sb, ref size);
                if (result == 0)
                {
                    string uncRoot = sb.ToString();
                    string relativePath = path.Substring(root.Length);
                    return Path.Combine(uncRoot, relativePath);
                }
            }
            catch
            {
              
            }
            return path;
        }

        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        private static extern int WNetGetConnection(
            [MarshalAs(UnmanagedType.LPTStr)] string localName,
            [MarshalAs(UnmanagedType.LPTStr)] System.Text.StringBuilder remoteName,
            ref int length);

     
        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string JobName { get; set; }
            public string SourcePath { get; set; }
            public string DestinationPath { get; set; }
            public long FileSize { get; set; }
            public long TransferTime { get; set; }
            public string ErrorMessage { get; set; }
        }

    
        public class JobStatus
        {
            public string State { get; set; }
            public float Progress { get; set; }
            public DateTime LastUpdated { get; set; }
        }
    }
}
