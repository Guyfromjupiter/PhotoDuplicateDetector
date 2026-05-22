using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Serialization.Formatters;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PhotoDuplicateDetector
{

    public class HashLayer
    {

        internal static Dictionary<string, List<string>> hashMap = new Dictionary<string, List<string>>();

        public static void Sub1()
        {
            foreach (string path in ImageScanner.GetData())
            {
                string? hash = ComputeSha256Hash(path);
                if (hash == null) continue;

                if (!hashMap.TryGetValue(hash, out var list))
                {
                    list = new List<string>();
                    hashMap[hash] = list;
                }

                list.Add(path);
            }

            Sha256Compare();
        }
        static string? ComputeSha256Hash(string filePath)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);

                byte[] hash = sha256.ComputeHash(stream);
                return Convert.ToHexString(hash);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Hash failed for {filePath}: {e.Message}");
                return null;
            }
        }
        public static void Sha256Compare()
        {
            bool duplicateFound = false;
            foreach (var record in hashMap)
            {
                if (record.Value.Count > 1)
                {
                    duplicateFound = true;
                    Console.WriteLine($"Duplicate found for hash: {record.Key}");
                    foreach (var path in record.Value)
                    {
                        Console.WriteLine($" - {path}");
                    }
                }

            }
            if (!duplicateFound)
            {
                Console.WriteLine("No duplicates found.");
            }
        }
    }
}