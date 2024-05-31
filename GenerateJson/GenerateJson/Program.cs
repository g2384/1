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
            string ivFilePath = "iv.txt";

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
            var encryptionKey = GetKeyChars(keyFilePath);
            var encryptionIV = GetKeyChars(ivFilePath);

            var random = new Random(42); // Stable random generator seed

            for (var i = 0; i < 50; i++)
            {
                var reshuffledKey = Reshuffle(encryptionKey, random);
                var reshuffledIV = Reshuffle(encryptionIV, random);
                reshuffledKey = ResizetToKey(reshuffledKey);
                reshuffledIV = ResizeToIV(reshuffledIV);
                Encrypt(lines, i, reshuffledKey, reshuffledIV);
            }

            encryptionKey = ResizetToKey(encryptionKey);
            encryptionIV = ResizeToIV(encryptionIV);
            Encrypt(lines, 51, encryptionKey, encryptionIV);
        }

        private static string ResizeToIV(string reshuffledIV)
        {
            reshuffledIV = reshuffledIV.PadRight(16).Substring(0, 16);
            return reshuffledIV;
        }

        private static string ResizetToKey(string reshuffledKey)
        {
            return reshuffledKey.PadRight(256 / 8).Substring(0, 256 / 8);
        }

        private static string GetKeyChars(string keyFilePath)
        {
            string encryptionKey = File.ReadAllText(keyFilePath);
            encryptionKey = RemoveWhitespace(encryptionKey);
            return encryptionKey;
        }

        private static void Encrypt(string[] lines, int i, string key, string iv)
        {
            Console.WriteLine($"key: {key}    iv: {iv}");
            var encryptedCollection = new List<string>();

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);
            var encryptor = aes.CreateEncryptor();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string encryptedText = EncryptString(line, encryptor);
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

        static string EncryptString(string plainText, ICryptoTransform encryptor)
        {
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
