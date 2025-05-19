using System;
using System.Diagnostics;
using System.IO;

namespace EasySave.Model
{
    public class CryptoSoftManager
    {
        private string _cryptoSoftPath;

        public CryptoSoftManager(string cryptoSoftPath)
        {
            _cryptoSoftPath = cryptoSoftPath;
        }

        public long EncryptFile(string source, string destination)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _cryptoSoftPath,
                    Arguments = $"\"{source}\" \"{destination}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var stopwatch = Stopwatch.StartNew();
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    stopwatch.Stop();
                    if (process.ExitCode == 0)
                        return stopwatch.ElapsedMilliseconds;
                    else
                        return -process.ExitCode; // Negative error code
                }
            }
            catch
            {
                return -9999; // Arbitrary error code for failure
            }
        }

        public long GetEncryptionTime(string filePath)
        {
            // This method can be used to retrieve the last encryption time if needed.
            // For now, encryption time is returned by EncryptFile.
            return 0;
        }

        public string GetCryptoSoftPath() => _cryptoSoftPath;

        public void SetCryptoSoftPath(string path) => _cryptoSoftPath = path;

        public bool TestCryptoSoftConnection()
        {
            try
            {
                return File.Exists(_cryptoSoftPath);
            }
            catch
            {
                return false;
            }
        }
    }
}
