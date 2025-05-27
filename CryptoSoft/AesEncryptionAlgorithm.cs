using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CryptoSoft
{
    public class AesEncryptionAlgorithm : IEncryptionAlgorithm
    {
        public void Encrypt(string inputFile, string outputFile, string key)
        {
            using FileStream inputStream = new(inputFile, FileMode.Open, FileAccess.Read);
            using FileStream outputStream = new(outputFile, FileMode.Create, FileAccess.Write);

            using Aes aes = Aes.Create();
            byte[] keyBytes = new byte[32];
            byte[] passwordBytes = Encoding.UTF8.GetBytes(key);
            Array.Copy(passwordBytes, keyBytes, Math.Min(passwordBytes.Length, keyBytes.Length));
            aes.Key = keyBytes;
            aes.GenerateIV();
            outputStream.Write(aes.IV, 0, aes.IV.Length);

            using CryptoStream cryptoStream = new(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            inputStream.CopyTo(cryptoStream);
        }
    }
}
