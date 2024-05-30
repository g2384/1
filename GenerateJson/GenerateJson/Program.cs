using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GenerateJson
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputFilePath = "input.txt";
            string keyFilePath = "key.txt";

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine($"Input file '{inputFilePath}' does not exist.");
                return;
            }

            if (!File.Exists(keyFilePath))
            {
                Console.WriteLine($"Key file '{keyFilePath}' does not exist.");
                return;
            }

            var lines = File.ReadAllLines(inputFilePath);
            string encryptionKey = File.ReadAllText(keyFilePath);
            encryptionKey = RemoveWhitespace(encryptionKey);

            var random = new Random(42); // Stable random generator seed

            var maxLegalKeySize = 256;
            using (var aes = Aes.Create())
            {
                maxLegalKeySize = aes.LegalKeySizes.Max()!.MaxSize;
                Console.WriteLine($"Max legal key size: {maxLegalKeySize}");
            }

            for (var i = 0; i < 50; i++)
            {
                string reshuffledKey = Reshuffle(encryptionKey, random);
                reshuffledKey = reshuffledKey.PadRight(maxLegalKeySize / 8).Substring(0, maxLegalKeySize / 8);
                Encrypt(lines, i, reshuffledKey);
            }

            encryptionKey = encryptionKey.PadRight(maxLegalKeySize / 8).Substring(0, maxLegalKeySize / 8);
            Encrypt(lines, 51, encryptionKey);
        }

        private static void Encrypt(string[] lines, int i, string key)
        {
            Console.WriteLine(key);
            var encryptedCollection = new List<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string encryptedText = EncryptString(line, key);
                encryptedCollection.Add(encryptedText);
            }

            string json = JsonSerializer.Serialize(encryptedCollection, new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText($"{i}.json", json);
        }

        static string Reshuffle(string input, Random random)
        {
            return new string(input.OrderBy(_ => random.Next()).ToArray());
        }

        static string EncryptString(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16];
            var encryptor = aes.CreateEncryptor();

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                using var sw = new StreamWriter(cs);
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        static string RemoveWhitespace(string input)
        {
            return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }
    }
}
