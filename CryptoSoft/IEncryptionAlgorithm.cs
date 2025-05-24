using System.IO;

namespace CryptoSoft
{
    public interface IEncryptionAlgorithm
    {
        /// <summary>
        /// Encrypts the input file and writes the result to the output file.
        /// </summary>
        /// <param name="inputFile">Path to the input file.</param>
        /// <param name="outputFile">Path to the output (encrypted) file.</param>
        /// <param name="key">Encryption key or password.</param>
        void Encrypt(string inputFile, string outputFile, string key);
    }
}
