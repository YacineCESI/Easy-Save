using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace EasySave.Model
{

    public class FileManager
    {
        

        public long CopyFile(string sourcePath, string destinationPath, string jobName = null)
        {
            try
            {
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                var fileInfo = new FileInfo(sourcePath);
                long fileSizer = fileInfo.Length;


                var stopWatch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(sourcePath, destinationPath, true);
                stopWatch.Stop();
                long transferTime = stopWatch.ElapsedMilliseconds;

                return transferTime;



            }
            catch (Exception ex)
            {
              
                return -1;


            }
        }

        public long CopyDirectory(string sourceDir, string targetDir, bool fullBackup,
         Func<float, bool> onProgressUpdate = null)
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

                // for diff backup
                if (!fullBackup && Directory.Exists(targetDir))
                {
                    filesToCopy = CompareDirectories(sourceDir, targetDir);
                }

                if (filesToCopy.Count == 0)
                {
                    return 0;
                }

                int totalFiles = sourceFiles.Count;
                int copiedFiles = 0;


                foreach (string sourceFile in filesToCopy)
                {
                    
                    string relativePath = sourceFile.Substring(sourceDir.Length + 1);
                    string targetFile = Path.Combine(targetDir, relativePath);

                    long result = CopyFile(sourceFile, targetFile, Path.GetFileName(targetDir));

                    if (result >= 0)
                        copiedFiles++;

                   
                    float progress = (float)copiedFiles / totalFiles * 100;
                    if (onProgressUpdate != null)
                    {
                        bool continueOperation = onProgressUpdate(progress);
                        if (!continueOperation)
                        {
                           
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
            }catch
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
                // Create the relative path
                string relativePath = sourceFile.Substring(sourceDir.Length + 1);
                string targetFile = Path.Combine(targetDir, relativePath);

                // Check if file exists in target
                if (!File.Exists(targetFile))
                {
                    filesToCopy.Add(sourceFile);
                    continue;
                }

                // Check if file has been modified
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
