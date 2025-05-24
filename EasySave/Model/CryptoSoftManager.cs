using System;
using System.Diagnostics;
using System.IO;

namespace EasySave.Model
{
    public class CryptoSoftManager
    {
        private string _cryptoSoftPath;

        public CryptoSoftManager(string cryptoSoftPath = null)
        {
            _cryptoSoftPath = cryptoSoftPath ?? @"C:\Program Files\CryptoSoft\CryptoSoft.exe";
        }

        public long EncryptFile(string source, string destination)
        {
            if (!File.Exists(_cryptoSoftPath))
            {
                return -1; // CryptoSoft executable not found
            }

            if (!File.Exists(source))
            {
                return -2; // Source file not found
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _cryptoSoftPath,
                    Arguments = $"\"{source}\" \"{destination}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var watch = Stopwatch.StartNew();
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    watch.Stop();
                    
                    // If exit code is 0, encryption was successful
                    if (process.ExitCode == 0)
                    {
                        return watch.ElapsedMilliseconds;
                    }
                    else
                    {
                        return -3 - process.ExitCode; // Return negative value based on exit code
                    }
                }
            }
            catch (Exception)
            {
                return -10; // General encryption error
            }
        }

        public long GetEncryptionTime(string filePath)
        {
            if (!File.Exists(_cryptoSoftPath))
            {
                return -1; // CryptoSoft executable not found
            }

            if (!File.Exists(filePath))
            {
                return -2; // Source file not found
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Length;
                
                // Simple estimation: 10MB per second
                const long bytesPerSecond = 10 * 1024 * 1024;
                
                // Convert to milliseconds and add a bit of overhead
                return (long)(fileSize / (double)bytesPerSecond * 1000) + 100;
            }
            catch (Exception)
            {
                return -10; // Error estimating encryption time
            }
        }

        public string GetCryptoSoftPath()
        {
            return _cryptoSoftPath;
        }

        public void SetCryptoSoftPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _cryptoSoftPath = path;
            }
        }
    }
}
