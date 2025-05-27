using System;
using System.IO;
using System.Text.Json;
using System.Xml;
using EasySave.Model.Enums;

namespace EasySave.Model
{
    public class Logger
    {
        private readonly string _logDirectory;
        private readonly string _activityLogPathJson;
        private readonly string _statusLogPathJson;
        private readonly string _encryptionLogPathJson;
        private readonly string _activityLogPathXaml;
        private readonly string _statusLogPathXaml;
        private readonly string _encryptionLogPathXaml;

        public Logger()
        {
            try
            {
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
                
                // JSON log paths
                _activityLogPathJson = Path.Combine(_logDirectory, $"activity_log_{today}.json");
                _statusLogPathJson = Path.Combine(_logDirectory, $"status_log_{today}.json");
                _encryptionLogPathJson = Path.Combine(_logDirectory, $"encryption_log_{today}.json");

                // XAML log paths
                _activityLogPathXaml = Path.Combine(_logDirectory, $"activity_log_{today}.xaml");
                _statusLogPathXaml = Path.Combine(_logDirectory, $"status_log_{today}.xaml");
                _encryptionLogPathXaml = Path.Combine(_logDirectory, $"encryption_log_{today}.xaml");

                // Create JSON files if they don't exist
                EnsureFileExists(_activityLogPathJson, true);
                EnsureFileExists(_statusLogPathJson, false);
                EnsureFileExists(_encryptionLogPathJson, true);
                
                // Create XAML files if they don't exist
                EnsureFileExists(_activityLogPathXaml, true, true);
                EnsureFileExists(_statusLogPathXaml, false, true);
                EnsureFileExists(_encryptionLogPathXaml, true, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing logger: {ex.Message}");
                // Fall back to a directory that should always be writable
                _logDirectory = Path.Combine(Path.GetTempPath(), "EasySave", "Logs");
                Directory.CreateDirectory(_logDirectory);

                var today = DateTime.Now.ToString("yyyy-MM-dd");
                
                // JSON log paths
                _activityLogPathJson = Path.Combine(_logDirectory, $"activity_log_{today}.json");
                _statusLogPathJson = Path.Combine(_logDirectory, $"status_log_{today}.json");
                _encryptionLogPathJson = Path.Combine(_logDirectory, $"encryption_log_{today}.json");
                
                // XAML log paths
                _activityLogPathXaml = Path.Combine(_logDirectory, $"activity_log_{today}.xaml");
                _statusLogPathXaml = Path.Combine(_logDirectory, $"status_log_{today}.xaml");
                _encryptionLogPathXaml = Path.Combine(_logDirectory, $"encryption_log_{today}.xaml");

                Console.WriteLine($"Using fallback log directory: {_logDirectory}");
            }
        }

        private void EnsureFileExists(string filePath, bool isArray = false, bool isXaml = false)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    if (isXaml)
                    {
                        // Create empty XAML structure
                        string content = isArray 
                            ? "<ArrayOfLogEntry xmlns=\"http://schemas.datacontract.org/2004/07/EasySave.Model\" />" 
                            : "<LogEntry xmlns=\"http://schemas.datacontract.org/2004/07/EasySave.Model\" />";
                        File.WriteAllText(filePath, content);
                    }
                    else 
                    {
                        // Create empty JSON structure
                        File.WriteAllText(filePath, isArray ? "[]" + Environment.NewLine : "{}");
                    }
                    Console.WriteLine($"Created log file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating log file {filePath}: {ex.Message}");
            }
        }

        // Get the appropriate log path based on log type and format
        private string GetLogPath(string logType, LogFormat format)
        {
            return format == LogFormat.JSON
                ? logType switch
                {
                    "activity" => _activityLogPathJson,
                    "status" => _statusLogPathJson,
                    "encryption" => _encryptionLogPathJson,
                    _ => _activityLogPathJson
                }
                : logType switch
                {
                    "activity" => _activityLogPathXaml,
                    "status" => _statusLogPathXaml,
                    "encryption" => _encryptionLogPathXaml,
                    _ => _activityLogPathXaml
                };
        }

        public void LogAction(string jobName, string sourceFile, string destinationFile, long fileSize, long transferTime, long encryptionTime, LogFormat format = LogFormat.JSON)
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

                string logPath = GetLogPath("activity", format);
                
                if (format == LogFormat.JSON)
                {
                    // Serialize with better formatting
                    string json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });

                    // Use locking to avoid file access conflicts
                    lock (this)
                    {
                        // Read existing content if the file exists and isn't empty
                        string content = "[]";
                        try
                        {
                            if (File.Exists(logPath) && new FileInfo(logPath).Length > 0)
                            {
                                content = File.ReadAllText(logPath);
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

                        File.WriteAllText(logPath, content);
                    }
                }
                else // XAML format
                {
                    lock (this)
                    {
                        try
                        {
                            XmlDocument doc = new XmlDocument();
                            
                            // Load existing file or create new structure
                            if (File.Exists(logPath) && new FileInfo(logPath).Length > 0)
                            {
                                doc.Load(logPath);
                            }
                            else
                            {
                                doc.LoadXml("<ArrayOfLogEntry xmlns=\"http://schemas.datacontract.org/2004/07/EasySave.Model\" />");
                            }

                            // Create new entry
                            XmlElement entry = doc.CreateElement("LogEntry");
                            
                            // Add all properties
                            AddXmlElement(doc, entry, "Timestamp", logEntry.Timestamp);
                            AddXmlElement(doc, entry, "JobName", logEntry.JobName);
                            AddXmlElement(doc, entry, "SourceFile", logEntry.SourceFile);
                            AddXmlElement(doc, entry, "DestinationFile", logEntry.DestinationFile);
                            AddXmlElement(doc, entry, "FileSize", logEntry.FileSize.ToString());
                            AddXmlElement(doc, entry, "TransferTime", logEntry.TransferTime.ToString());
                            AddXmlElement(doc, entry, "EncryptionTime", logEntry.EncryptionTime.ToString());

                            // Append entry to root
                            doc.DocumentElement.AppendChild(entry);
                            
                            // Save the document
                            doc.Save(logPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating XAML log: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"Logged action for job {jobName} to {logPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging action: {ex.Message}");
            }
        }

        // Helper method to add XML elements
        private void AddXmlElement(XmlDocument doc, XmlElement parent, string name, string value)
        {
            XmlElement element = doc.CreateElement(name);
            element.InnerText = value;
            parent.AppendChild(element);
        }

        public void UpdateJobStatus(string jobName, JobState status, float progress, LogFormat format = LogFormat.JSON)
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

                string logPath = GetLogPath("status", format);
                
                if (format == LogFormat.JSON)
                {
                    string json = JsonSerializer.Serialize(statusEntry, new JsonSerializerOptions { WriteIndented = true });

                    // Use locking to avoid file access conflicts
                    lock (this)
                    {
                        // Use File.WriteAllText which is more reliable than AppendAllText
                        File.WriteAllText(logPath, json);
                    }
                }
                else // XAML format
                {
                    lock (this)
                    {
                        try
                        {
                            XmlDocument doc = new XmlDocument();
                            
                            // For status logs, we always overwrite the previous status
                            doc.LoadXml("<LogEntry xmlns=\"http://schemas.datacontract.org/2004/07/EasySave.Model\" />");
                            XmlElement root = doc.DocumentElement;
                            
                            // Add all properties
                            AddXmlElement(doc, root, "Timestamp", statusEntry.Timestamp);
                            AddXmlElement(doc, root, "JobName", statusEntry.JobName);
                            AddXmlElement(doc, root, "Status", statusEntry.Status);
                            AddXmlElement(doc, root, "Progress", statusEntry.Progress.ToString());
                            
                            // Save the document
                            doc.Save(logPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating XAML status log: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"Updated status for job {jobName} to {status} with progress {progress:F2}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating job status: {ex.Message}");
            }
        }

        public void LogEncryptionDetails(string filePath, long encryptionTime, LogFormat format = LogFormat.JSON)
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

                string logPath = GetLogPath("encryption", format);
                
                if (format == LogFormat.JSON)
                {
                    string json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });

                    // Use locking to avoid file access conflicts
                    lock (this)
                    {
                        // Read existing content if the file exists and isn't empty
                        string content = "[]";
                        try
                        {
                            if (File.Exists(logPath) && new FileInfo(logPath).Length > 0)
                            {
                                content = File.ReadAllText(logPath);
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

                        File.WriteAllText(logPath, content);
                    }
                }
                else // XAML format
                {
                    lock (this)
                    {
                        try
                        {
                            XmlDocument doc = new XmlDocument();
                            
                            // Load existing file or create new structure
                            if (File.Exists(logPath) && new FileInfo(logPath).Length > 0)
                            {
                                doc.Load(logPath);
                            }
                            else
                            {
                                doc.LoadXml("<ArrayOfLogEntry xmlns=\"http://schemas.datacontract.org/2004/07/EasySave.Model\" />");
                            }

                            // Create new entry
                            XmlElement entry = doc.CreateElement("LogEntry");
                            
                            // Add all properties
                            AddXmlElement(doc, entry, "Timestamp", logEntry.Timestamp);
                            AddXmlElement(doc, entry, "FilePath", logEntry.FilePath);
                            AddXmlElement(doc, entry, "EncryptionTime", logEntry.EncryptionTime.ToString());
                            AddXmlElement(doc, entry, "Status", logEntry.Status);

                            // Append entry to root
                            doc.DocumentElement.AppendChild(entry);
                            
                            // Save the document
                            doc.Save(logPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating XAML encryption log: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"Logged encryption details for {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging encryption details: {ex.Message}");
            }
        }

        public void LogError(string jobName, string errorMessage, LogFormat format = LogFormat.JSON)
        {
            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    JobName = jobName ?? "Unknown",
                    Error = errorMessage
                };

                string logPath = GetLogPath("activity", format);
                
                if (format == LogFormat.JSON)
                {
                    string json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });

                    // Use locking to avoid file access conflicts
                    lock (this)
                    {
                        // Read existing content if the file exists and isn't empty
                        string content = "[]";
                        try
                        {
                            if (File.Exists(logPath) && new FileInfo(logPath).Length > 0)
                            {
                                content = File.ReadAllText(logPath);
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

                        File.WriteAllText(logPath, content);
                    }
                }
                else // XAML format
                {
                    lock (this)
                    {
                        try
                        {
                            XmlDocument doc = new XmlDocument();
                            
                            // Load existing file or create new structure
                            if (File.Exists(logPath) && new FileInfo(logPath).Length > 0)
                            {
                                doc.Load(logPath);
                            }
                            else
                            {
                                doc.LoadXml("<ArrayOfLogEntry xmlns=\"http://schemas.datacontract.org/2004/07/EasySave.Model\" />");
                            }

                            // Create new entry
                            XmlElement entry = doc.CreateElement("LogEntry");
                            
                            // Add all properties
                            AddXmlElement(doc, entry, "Timestamp", logEntry.Timestamp);
                            AddXmlElement(doc, entry, "JobName", logEntry.JobName);
                            AddXmlElement(doc, entry, "Error", logEntry.Error);

                            // Append entry to root
                            doc.DocumentElement.AppendChild(entry);
                            
                            // Save the document
                            doc.Save(logPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating XAML error log: {ex.Message}");
                        }
                    }
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
