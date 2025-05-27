using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace EasySave.Model
{
    public class FileManager
    {
        private readonly CryptoSoftManager _cryptoSoftManager;
        private readonly Logger _logger;
        private static SemaphoreSlim _fileOperationSemaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

        // Add reference to BackupManager for bandwidth/large file coordination
        private BackupManager _backupManager;

        // Global static set to track remaining priority files across all jobs
        private static readonly ConcurrentDictionary<string, byte> GlobalPriorityFiles = new();

        // Add reference to BusinessSoftwareManager for blocked process monitoring
        private BusinessSoftwareManager _businessSoftwareManager;

        public FileManager(CryptoSoftManager cryptoSoftManager, Logger logger)
        {
            _cryptoSoftManager = cryptoSoftManager ?? throw new ArgumentNullException(nameof(cryptoSoftManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _businessSoftwareManager = new BusinessSoftwareManager();
        }

        // New: Set BackupManager reference (call this after construction)
        public void SetBackupManager(BackupManager backupManager)
        {
            _backupManager = backupManager;
        }

        public CryptoSoftManager GetCryptoSoftManager()
        {
            return _cryptoSoftManager;
        }

        // Add method to get BusinessSoftwareManager
        public BusinessSoftwareManager GetBusinessSoftwareManager()
        {
            return _businessSoftwareManager;
        }

        // Call this before starting all jobs to initialize the global set
        public static void RegisterPriorityFiles(IEnumerable<string> allPriorityFiles)
        {
            GlobalPriorityFiles.Clear();
            foreach (var file in allPriorityFiles)
                GlobalPriorityFiles.TryAdd(file, 0);
        }

        // Call this after copying a priority file
        public static void MarkPriorityFileCopied(string filePath)
        {
            GlobalPriorityFiles.TryRemove(filePath, out _);
        }

        // Returns true if there are any priority files left to copy
        public static bool PriorityFilesRemaining()
        {
            return !GlobalPriorityFiles.IsEmpty;
        }

        // Add method for monitoring blocked processes
        public void CheckBlockedProcesses(List<string> blockedProcesses)
        {
            if (_businessSoftwareManager.IsBusinessSoftwareRunning(blockedProcesses))
            {
                throw new BlockedProcessRunningException("A blocked process is currently running. File operation paused.");
            }
        }

        public long CopyFile(string source, string destination, bool encrypt, List<string> blockedProcesses = null)
        {
            // Check if any blocked process is running before proceeding
            if (blockedProcesses != null && _businessSoftwareManager.IsBusinessSoftwareRunning(blockedProcesses))
            {
                _logger.LogError(null, $"File copy paused: Blocked process running while trying to copy {source}");
                throw new BlockedProcessRunningException($"Cannot copy file {source}: A blocked process is running");
            }

            if (!File.Exists(source))
            {
                return -1; // Error: source file does not exist
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));

                var fileInfo = new FileInfo(source);
                var fileSize = fileInfo.Length;

                var startTime = DateTime.Now;

                _fileOperationSemaphore.Wait();
                try
                {
                    File.Copy(source, destination, true);
                }
                finally
                {
                    _fileOperationSemaphore.Release();
                }

                var transferTime = (long)(DateTime.Now - startTime).TotalMilliseconds;

                _logger.LogAction(null, source, destination, fileSize, transferTime, 0);

                long encryptionTime = 0;
                if (encrypt)
                {
                    var tempPath = destination + ".tmp";
                    if (File.Exists(destination))
                    {
                        _fileOperationSemaphore.Wait();
                        try
                        {
                            File.Move(destination, tempPath, true);
                            encryptionTime = _cryptoSoftManager.EncryptFile(tempPath, destination);

                            _logger.LogEncryptionDetails(destination, encryptionTime);

                            if (encryptionTime > 0 && File.Exists(tempPath))
                            {
                                File.Delete(tempPath);
                            }
                            else if (encryptionTime < 0)
                            {
                                if (File.Exists(tempPath))
                                {
                                    File.Move(tempPath, destination, true);
                                }
                                _logger.LogError(null, $"Encryption failed for {destination}: {encryptionTime}");
                            }
                        }
                        finally
                        {
                            _fileOperationSemaphore.Release();
                        }
                    }
                }

                return transferTime + encryptionTime;
            }
            catch (BlockedProcessRunningException)
            {
                // Re-throw this specific exception to be handled appropriately
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(null, $"Error copying file {source} to {destination}: {ex.Message}");
                return -2;
            }
        }

        public long CopyDirectory(string sourceDir, string targetDir, List<string> extensionsToEncrypt, bool encrypt, Func<float, bool> onProgressUpdate = null, List<string> blockedProcesses = null)
        {
            if (blockedProcesses == null)
                blockedProcesses = new List<string>();

            if (!Directory.Exists(sourceDir))
            {
                return -1;
            }

            try
            {
                Directory.CreateDirectory(targetDir);

                string[] files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
                int totalFiles = files.Length;

                if (totalFiles == 0)
                {
                    onProgressUpdate?.Invoke(100);
                    return 0;
                }

                int processedFiles = 0;
                long totalTime = 0;

                var priorityExtensions = extensionsToEncrypt ?? new List<string>();
                CopyWithPriorityEnforcement(files.ToList(), priorityExtensions, blockedProcesses);

                foreach (string file in files)
                {
                    string relativePath = file.Substring(sourceDir.Length + 1);
                    string targetPath = Path.Combine(targetDir, relativePath);

                    bool encryptFile = encrypt && ShouldEncrypt(file, extensionsToEncrypt);

                    try
                    {
                        // Check if any blocked process is running before each file copy
                        if (blockedProcesses != null && _businessSoftwareManager.IsBusinessSoftwareRunning(blockedProcesses))
                        {
                            // If a blocked process is running, we need to pause by throwing an exception
                            throw new BlockedProcessRunningException($"Cannot copy file {file}: A blocked process is running");
                        }

                        long fileTime = CopyFile(file, targetPath, encryptFile, blockedProcesses);

                        if (fileTime >= 0)
                        {
                            totalTime += fileTime;
                        }
                    }
                    catch (BlockedProcessRunningException)
                    {
                        // This means we need to pause the operation
                        // The job's execution will handle this properly
                        return -4; // Special code for blocked process
                    }

                    processedFiles++;
                    float progress = (float)processedFiles / totalFiles * 100;

                    if (onProgressUpdate != null)
                    {
                        if (!onProgressUpdate(progress))
                        {
                            return -3;
                        }
                    }
                }

                return totalTime;
            }
            catch (BlockedProcessRunningException)
            {
                // This means we need to pause the operation
                return -4; // Special code for blocked process
            }
            catch (Exception ex)
            {
                _logger.LogError(null, $"Error copying directory {sourceDir} to {targetDir}: {ex.Message}");
                return -2;
            }
        }

        private void CopyWithPriorityEnforcement(List<string> files, List<string> priorityExtensions, List<string> blockedProcesses = null)
        {
            // Process files by extension priority order
            foreach (var ext in priorityExtensions)
            {
                foreach (var file in files.Where(f => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    // Check for blocked processes before each operation
                    if (blockedProcesses != null && _businessSoftwareManager.IsBusinessSoftwareRunning(blockedProcesses))
                    {
                        throw new BlockedProcessRunningException($"Priority file processing paused: A blocked process is running");
                    }

                    CopyFile(file, /*destination*/"", false, blockedProcesses);
                    MarkPriorityFileCopied(file);
                }
            }
            // Now process non-priority files
            foreach (var file in files)
            {
                bool isPriority = priorityExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
                if (!isPriority)
                {
                    // Check for blocked processes
                    if (blockedProcesses != null && _businessSoftwareManager.IsBusinessSoftwareRunning(blockedProcesses))
                    {
                        throw new BlockedProcessRunningException($"Non-priority file processing paused: A blocked process is running");
                    }

                    while (PriorityFilesRemaining())
                    {
                        Thread.Sleep(100);
                    }
                    CopyFile(file, /*destination*/"", false, blockedProcesses);
                }
            }
        }

        public bool ShouldEncrypt(string filePath, List<string> extensionsToEncrypt)
        {
            if (extensionsToEncrypt == null || extensionsToEncrypt.Count == 0)
            {
                return false;
            }

            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            if (extension.StartsWith("."))
            {
                extension = extension.Substring(1);
            }

            return extensionsToEncrypt.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        // Add a new exception class for blocked process situations
        public class BlockedProcessRunningException : Exception
        {
            public BlockedProcessRunningException(string message) : base(message) { }
        }
    }
}
