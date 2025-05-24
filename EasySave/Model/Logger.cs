using System;
using System.IO;
using System.Text.Json;
using EasySave.Model.Enums;

namespace EasySave.Model
{
    public class Logger
    {
        private readonly string _logDirectory;
        private readonly string _activityLogPath;
        private readonly string _statusLogPath;
        private readonly string _encryptionLogPath;

        public Logger()
        {
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "Logs");
            Directory.CreateDirectory(_logDirectory);
            
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            _activityLogPath = Path.Combine(_logDirectory, $"activity_log_{today}.json");
            _statusLogPath = Path.Combine(_logDirectory, $"status_log_{today}.json");
            _encryptionLogPath = Path.Combine(_logDirectory, $"encryption_log_{today}.json");
        }

        public void LogAction(string jobName, string sourceFile, string destinationFile, long fileSize, long transferTime, long encryptionTime)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    JobName = jobName,
                    SourceFile = sourceFile,
                    DestinationFile = destinationFile,
                    FileSize = fileSize,
                    TransferTime = transferTime,
                    EncryptionTime = encryptionTime
                };

                string json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });
                File.AppendAllText(_activityLogPath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging action: {ex.Message}");
            }
        }

        public void UpdateJobStatus(string jobName, JobState status, float progress)
        {
            try
            {
                var statusEntry = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    JobName = jobName,
                    Status = status.ToString(),
                    Progress = progress
                };

                string json = JsonSerializer.Serialize(statusEntry, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_statusLogPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating job status: {ex.Message}");
            }
        }

        public void LogEncryptionDetails(string filePath, long encryptionTime)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    FilePath = filePath,
                    EncryptionTime = encryptionTime,
                    Status = encryptionTime > 0 ? "Success" : "Failed"
                };

                string json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });
                File.AppendAllText(_encryptionLogPath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging encryption details: {ex.Message}");
            }
        }

        public void LogError(string jobName, string errorMessage)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    JobName = jobName,
                    Error = errorMessage
                };

                string json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });
                File.AppendAllText(_activityLogPath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging error: {ex.Message}");
            }
        }
    }
}
