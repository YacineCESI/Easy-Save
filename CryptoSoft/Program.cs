using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CryptoSoft
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== CryptoSoft File Encryption ===");

            // Get directory
            Console.Write("Enter the directory to search: ");
            string directory = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                Console.WriteLine("Invalid directory.");
                return;
            }

            // Get extensions
            Console.Write("Enter file extensions to encrypt (comma-separated, e.g. .pdf,.docx): ");
            string extInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(extInput))
            {
                Console.WriteLine("No extensions specified.");
                return;
            }
            var extensions = extInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(e => e.Trim().ToLowerInvariant())
                                     .Where(e => e.StartsWith("."))
                                     .ToList();

            // Subdirectories?
            Console.Write("Include subdirectories? (y/n): ");
            bool recursive = Console.ReadLine()?.Trim().ToLowerInvariant() == "y";

            // Algorithm selection (for now, only AES)
            Console.WriteLine("Available encryption algorithms:");
            Console.WriteLine("1. AES");
            Console.Write("Select algorithm [1]: ");
            string algoChoice = Console.ReadLine();
            IEncryptionAlgorithm algorithm = new AesEncryptionAlgorithm();

            // Key/password
            Console.Write("Enter encryption key/password: ");
            string key = Console.ReadLine();
            if (string.IsNullOrEmpty(key))
            {
                Console.WriteLine("Encryption key cannot be empty.");
                return;
            }

            // Find files
            var files = Directory.EnumerateFiles(
                directory,
                "*.*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (files.Count == 0)
            {
                Console.WriteLine("No files found matching the specified extensions.");
                return;
            }

            Console.WriteLine($"Found {files.Count} file(s) to encrypt.");

            int success = 0, fail = 0;
            foreach (var file in files)
            {
                string outputFile = file + ".enc";
                try
                {
                    algorithm.Encrypt(file, outputFile, key);
                    Console.WriteLine($"Encrypted: {file} -> {outputFile}");
                    success++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to encrypt {file}: {ex.Message}");
                    fail++;
                }
            }

            Console.WriteLine($"Encryption complete. Success: {success}, Failed: {fail}");
        }
    }
}
