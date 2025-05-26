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

        // Global static set to track remaining priority files across all jobs
        private static readonly ConcurrentDictionary<string, byte> GlobalPriorityFiles = new();

        public FileManager(CryptoSoftManager cryptoSoftManager, Logger logger)
        {
            _cryptoSoftManager = cryptoSoftManager ?? throw new ArgumentNullException(nameof(cryptoSoftManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public CryptoSoftManager GetCryptoSoftManager()
        {
            return _cryptoSoftManager;
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

        public long CopyFile(string source, string destination, bool encrypt)
        {
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
            catch (Exception ex)
            {
                _logger.LogError(null, $"Error copying file {source} to {destination}: {ex.Message}");
                return -2;
            }
        }

        public long CopyDirectory(string sourceDir, string targetDir, List<string> extensionsToEncrypt, bool encrypt, Func<float, bool> onProgressUpdate = null)
        {
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
                CopyWithPriorityEnforcement(files.ToList(), priorityExtensions);

                foreach (string file in files)
                {
                    string relativePath = file.Substring(sourceDir.Length + 1);
                    string targetPath = Path.Combine(targetDir, relativePath);

                    bool encryptFile = encrypt && ShouldEncrypt(file, extensionsToEncrypt);

                    long fileTime = CopyFile(file, targetPath, encryptFile);

                    if (fileTime >= 0)
                    {
                        totalTime += fileTime;
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
            catch (Exception ex)
            {
                _logger.LogError(null, $"Error copying directory {sourceDir} to {targetDir}: {ex.Message}");
                return -2;
            }
        }

        private void CopyWithPriorityEnforcement(List<string> files, List<string> priorityExtensions)
        {
            // Process files by extension priority order
            foreach (var ext in priorityExtensions)
            {
                foreach (var file in files.Where(f => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    CopyFile(file, /*destination*/"", false);
                    MarkPriorityFileCopied(file);
                }
            }
            // Now process non-priority files
            foreach (var file in files)
            {
                bool isPriority = priorityExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
                if (!isPriority)
                {
                    while (PriorityFilesRemaining())
                    {
                        Thread.Sleep(100);
                    }
                    CopyFile(file, /*destination*/"", false);
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
    }
}
