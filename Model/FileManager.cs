using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace EasySave.Model
{

    public class FileManager
    {
       
        public long CopyFile(string sourcePath, string destinationPath, Logger logger = null, string jobName = null)
        {
            try
            {
              
                string destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

             
                var fileInfo = new FileInfo(sourcePath);
                long fileSize = fileInfo.Length;

               
                var stopWatch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(sourcePath, destinationPath, true);
                stopWatch.Stop();
                long transferTime = stopWatch.ElapsedMilliseconds;

               
                if (logger != null && !string.IsNullOrEmpty(jobName))
                {
                    logger.LogAction(jobName, sourcePath, destinationPath, fileSize, transferTime);
                }

                return transferTime;
            }
            catch (Exception ex)
            {
                
                if (logger != null && !string.IsNullOrEmpty(jobName))
                {
                    logger.LogAction(jobName, sourcePath, destinationPath, 0, -1);
                }
                return -1;
            }
        }

  
 
        public long CopyDirectory(string sourceDir, string targetDir, bool fullBackup,
            Func<float, bool> onProgressUpdate = null, Logger logger = null, string jobName = null)
        {
            try
            {
               
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

               
                var sourceFiles = GetDirectoryFiles(sourceDir);
                if (sourceFiles.Count == 0)
                {
                    return 0; 
                }

                
                List<string> filesToCopy = sourceFiles;
                if (!fullBackup && Directory.Exists(targetDir))
                {
                    filesToCopy = CompareDirectories(sourceDir, targetDir);
                }

                if (filesToCopy.Count == 0)
                {
                    return 0; 
                }

               
                int totalFiles = filesToCopy.Count;
                int copiedFiles = 0;

                foreach (string sourceFile in filesToCopy)
                {
                  
                    string relativePath = sourceFile.Substring(sourceDir.Length + 1);
                    string targetFile = Path.Combine(targetDir, relativePath);

                    
                    long result = CopyFile(sourceFile, targetFile, logger, jobName);

                    if (result >= 0)
                        copiedFiles++;

                    float progress = (float)copiedFiles / totalFiles * 100;
                    if (onProgressUpdate != null)
                    {
                        bool continueOperation = onProgressUpdate(progress);
                        if (!continueOperation)
                        {
                            return copiedFiles;
                        }
                    }
                }

                return copiedFiles;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        
        public List<string> GetDirectoryFiles(string directoryPath)
        {
            try
            {
                return Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public List<string> CompareDirectories(string sourceDir, string targetDir)
        {
            var filesToCopy = new List<string>();

            var sourceFiles = GetDirectoryFiles(sourceDir);

            foreach (string sourceFile in sourceFiles)
            {
               
                string relativePath = sourceFile.Substring(sourceDir.Length + 1);
                string targetFile = Path.Combine(targetDir, relativePath);

                
                if (!File.Exists(targetFile))
                {
                    filesToCopy.Add(sourceFile);
                    continue;
                }

                
                DateTime sourceLastModified = File.GetLastWriteTime(sourceFile);
                DateTime targetLastModified = File.GetLastWriteTime(targetFile);

                if (sourceLastModified > targetLastModified)
                {
                    filesToCopy.Add(sourceFile);
                }
            }

            return filesToCopy;
        }

        public long GetFileSize(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                return fileInfo.Length;
            }
            catch
            {
                return 0;
            }
        }

        public DateTime GetLastModifiedTime(string path)
        {
            try
            {
                return File.GetLastWriteTime(path);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}
