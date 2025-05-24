using System;
using System.IO;
using CryptoSoft; // Reference the CryptoSoft project for IEncryptionAlgorithm and AesEncryptionAlgorithm

namespace EasySave.Model
{
    public class CryptoSoftManager
    {
        private readonly IEncryptionAlgorithm _algorithm;

        public CryptoSoftManager()
        {
            // For now, always use AES. This can be made configurable.
            _algorithm = new AesEncryptionAlgorithm();
        }

        /// <summary>
        /// Encrypts a file using the internal CryptoSoft algorithm.
        /// </summary>
        /// <param name="source">Path to the source file.</param>
        /// <param name="destination">Path to the destination file.</param>
        /// <returns>Encryption time in milliseconds, or negative value on error.</returns>
        public long EncryptFile(string source, string destination)
        {
            if (!File.Exists(source))
                return -2; // Source file not found

            try
            {
                // For demonstration, use a fixed key. In production, use a secure key management.
                string key = "DefaultCryptoSoftKey123!"; // Should be replaced with a secure key

                var watch = System.Diagnostics.Stopwatch.StartNew();
                _algorithm.Encrypt(source, destination, key);
                watch.Stop();
                return watch.ElapsedMilliseconds;
            }
            catch (Exception)
            {
                return -10; // General encryption error
            }
        }
    }
}
