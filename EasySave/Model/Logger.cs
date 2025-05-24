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
            try
            {
                // Use a more reliable path that doesn't require special permissions
              //  _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "Logs");
                string _logDirectory = Path.Combine(
             Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
             "EasySave", "Logs");

                // Ensure the directory exists with better error handling
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                    Console.WriteLine($"Created log directory: {_logDirectory}");
                }

                var today = DateTime.Now.ToString("yyyy-MM-dd");
                _activityLogPath = Path.Combine(_logDirectory, $"activity_log_{today}.json");
                _statusLogPath = Path.Combine(_logDirectory, $"status_log_{today}.json");
                _encryptionLogPath = Path.Combine(_logDirectory, $"encryption_log_{today}.json");

                // Create the files if they don't exist to avoid potential issues
                EnsureFileExists(_activityLogPath);
                EnsureFileExists(_statusLogPath);
                EnsureFileExists(_encryptionLogPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing logger: {ex.Message}");
                // Fall back to a directory that should always be writable
                _logDirectory = Path.Combine(Path.GetTempPath(), "EasySave", "Logs");
                Directory.CreateDirectory(_logDirectory);

                var today = DateTime.Now.ToString("yyyy-MM-dd");
                _activityLogPath = Path.Combine(_logDirectory, $"activity_log_{today}.json");
                _statusLogPath = Path.Combine(_logDirectory, $"status_log_{today}.json");
                _encryptionLogPath = Path.Combine(_logDirectory, $"encryption_log_{today}.json");

                Console.WriteLine($"Using fallback log directory: {_logDirectory}");
            }
        }

        private void EnsureFileExists(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    // Create an empty array for JSON files that should be arrays
                    if (filePath.Contains("activity_log_") || filePath.Contains("encryption_log_"))
                    {
                        File.WriteAllText(filePath, "[]" + Environment.NewLine);
                    }
                    else
                    {
                        File.WriteAllText(filePath, "{}");
                    }
                    Console.WriteLine($"Created log file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating log file {filePath}: {ex.Message}");
            }
        }

        public void LogAction(string jobName, string sourceFile, string destinationFile, long fileSize, long transferTime, long encryptionTime)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    JobName = jobName ?? "Unknown",
                    SourceFile = sourceFile,
                    DestinationFile = destinationFile,
                    FileSize = fileSize,
                    TransferTime = transferTime,
                    EncryptionTime = encryptionTime
                };

                // Serialize with better formatting
                string json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });

                // Use locking to avoid file access conflicts
                lock (this)
                {
                    // Read existing content if the file exists and isn't empty
                    string content = "[]";
                    try
                    {
                        if (File.Exists(_activityLogPath) && new FileInfo(_activityLogPath).Length > 0)
                        {
                            content = File.ReadAllText(_activityLogPath);
                            // If the file doesn't start with a bracket, initialize it as an array
                            if (!content.TrimStart().StartsWith("["))
                            {
                                content = "[]";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading activity log: {ex.Message}");
                    }

                    // Convert the array-style JSON with proper formatting
                    if (content == "[]")
                    {
                        // First entry
                        content = $"[\n{json}\n]";
                    }
                    else
                    {
                        // Remove the closing bracket, add the new entry, and close the array
                        content = content.TrimEnd().TrimEnd(']') + ",\n" + json + "\n]";
                    }

                    File.WriteAllText(_activityLogPath, content);
                }

                Console.WriteLine($"Logged action for job {jobName} to {_activityLogPath}");
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
                    JobName = jobName ?? "Unknown",
                    Status = status.ToString(),
                    Progress = progress
                };

                string json = JsonSerializer.Serialize(statusEntry, new JsonSerializerOptions { WriteIndented = true });

                // Use locking to avoid file access conflicts
                lock (this)
                {
                    // Use File.WriteAllText which is more reliable than AppendAllText
                    File.WriteAllText(_statusLogPath, json);
                }

                Console.WriteLine($"Updated status for job {jobName} to {status} with progress {progress:F2}%");
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

                // Use locking to avoid file access conflicts
                lock (this)
                {
                    // Read existing content if the file exists and isn't empty
                    string content = "[]";
                    try
                    {
                        if (File.Exists(_encryptionLogPath) && new FileInfo(_encryptionLogPath).Length > 0)
                        {
                            content = File.ReadAllText(_encryptionLogPath);
                            // If the file doesn't start with a bracket, initialize it as an array
                            if (!content.TrimStart().StartsWith("["))
                            {
                                content = "[]";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading encryption log: {ex.Message}");
                    }

                    // Convert the array-style JSON with proper formatting
                    if (content == "[]")
                    {
                        // First entry
                        content = $"[\n{json}\n]";
                    }
                    else
                    {
                        // Remove the closing bracket, add the new entry, and close the array
                        content = content.TrimEnd().TrimEnd(']') + ",\n" + json + "\n]";
                    }

                    File.WriteAllText(_encryptionLogPath, content);
                }

                Console.WriteLine($"Logged encryption details for {filePath}");
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
                    JobName = jobName ?? "Unknown",
                    Error = errorMessage
                };

                string json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });

                // Use locking to avoid file access conflicts
                lock (this)
                {
                    // Read existing content if the file exists and isn't empty
                    string content = "[]";
                    try
                    {
                        if (File.Exists(_activityLogPath) && new FileInfo(_activityLogPath).Length > 0)
                        {
                            content = File.ReadAllText(_activityLogPath);
                            // If the file doesn't start with a bracket, initialize it as an array
                            if (!content.TrimStart().StartsWith("["))
                            {
                                content = "[]";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading activity log: {ex.Message}");
                    }

                    // Convert the array-style JSON with proper formatting
                    if (content == "[]")
                    {
                        // First entry
                        content = $"[\n{json}\n]";
                    }
                    else
                    {
                        // Remove the closing bracket, add the new entry, and close the array
                        content = content.TrimEnd().TrimEnd(']') + ",\n" + json + "\n]";
                    }

                    File.WriteAllText(_activityLogPath, content);
                }

                Console.WriteLine($"Logged error for job {jobName}: {errorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging error message: {ex.Message}");
            }
        }

        // Method to get the current log directory path
        public string GetLogDirectory()
        {
            return _logDirectory;
        }
    }
}
