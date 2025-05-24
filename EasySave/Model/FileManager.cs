using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave.Model
{
    public class FileManager
    {
        private readonly CryptoSoftManager _cryptoSoftManager;
        private readonly Logger _logger;

        public FileManager(CryptoSoftManager cryptoSoftManager, Logger logger)
        {
            _cryptoSoftManager = cryptoSoftManager ?? throw new ArgumentNullException(nameof(cryptoSoftManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public CryptoSoftManager GetCryptoSoftManager()
        {
            return _cryptoSoftManager;
        }

        public long CopyFile(string source, string destination, bool encrypt)
        {
            if (!File.Exists(source))
            {
                return -1; // Error: source file does not exist
            }

            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                
                // Get file info before copy
                var fileInfo = new FileInfo(source);
                var fileSize = fileInfo.Length;
                
                // Start timing the transfer
                var startTime = DateTime.Now;
                
                // Copy the file
                File.Copy(source, destination, true);
                
                // Calculate transfer time in ms
                var transferTime = (long)(DateTime.Now - startTime).TotalMilliseconds;
                
                // Log the transfer
                _logger.LogAction(null, source, destination, fileSize, transferTime, 0);
                
                // If encryption is required, encrypt the file
                long encryptionTime = 0;
                if (encrypt)
                {
                    var tempPath = destination + ".tmp";
                    if (File.Exists(destination))
                    {
                        // Rename original to temp
                        File.Move(destination, tempPath, true);
                        
                        // Encrypt the file
                        encryptionTime = _cryptoSoftManager.EncryptFile(tempPath, destination);
                        
                        // Log encryption details
                        _logger.LogEncryptionDetails(destination, encryptionTime);
                        
                        // Delete temp file if encryption successful
                        if (encryptionTime > 0 && File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                        else if (encryptionTime < 0)
                        {
                            // If encryption failed, restore original
                            if (File.Exists(tempPath))
                            {
                                File.Move(tempPath, destination, true);
                            }
                            _logger.LogError(null, $"Encryption failed for {destination}: {encryptionTime}");
                        }
                    }
                }
                
                return transferTime + encryptionTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(null, $"Error copying file {source} to {destination}: {ex.Message}");
                return -2; // Error during copy
            }
        }

        public long CopyDirectory(string sourceDir, string targetDir, List<string> extensionsToEncrypt, bool encrypt, Func<float, bool> onProgressUpdate = null)
        {
            if (!Directory.Exists(sourceDir))
            {
                return -1; // Error: source directory does not exist
            }

            try
            {
                // Create the target directory if it doesn't exist
                Directory.CreateDirectory(targetDir);
                
                // Get all files in the source directory
                string[] files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
                int totalFiles = files.Length;
                int processedFiles = 0;
                long totalTime = 0;
                
                // Process each file
                foreach (string file in files)
                {
                    // Calculate relative path
                    string relativePath = file.Substring(sourceDir.Length + 1);
                    string targetPath = Path.Combine(targetDir, relativePath);
                    
                    // Determine if this file should be encrypted
                    bool encryptFile = encrypt && ShouldEncrypt(file, extensionsToEncrypt);
                    
                    // Copy and potentially encrypt the file
                    long fileTime = CopyFile(file, targetPath, encryptFile);
                    
                    if (fileTime >= 0)
                    {
                        totalTime += fileTime;
                    }
                    
                    // Update progress and check if operation should continue
                    processedFiles++;
                    float progress = (float)processedFiles / totalFiles * 100;
                    
                    if (onProgressUpdate != null && !onProgressUpdate(progress))
                    {
                        return -3; // Operation was canceled
                    }
                }
                
                return totalTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(null, $"Error copying directory {sourceDir} to {targetDir}: {ex.Message}");
                return -2; // Error during copy
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
            
            // Remove the dot from the extension if present
            if (extension.StartsWith("."))
            {
                extension = extension.Substring(1);
            }
            
            return extensionsToEncrypt.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }
    }
}
