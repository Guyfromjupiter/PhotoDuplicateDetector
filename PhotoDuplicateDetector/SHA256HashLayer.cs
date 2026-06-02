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

    public class SHA256HashLayer
    {

        internal static Dictionary<string, List<string>> hashMap = new Dictionary<string, List<string>>();
        private static List<String> SHA256_duplicate = new List<String>();
        private static List<string> NoSHA256ExactDuplicate  = new List<string>();
        public static void Sub1()
        {
            int count = 0;

            foreach (string path in ImageScanner.GetData())
            {
                count++;
                if (count % 100 == 0)
                {
                    Console.WriteLine($"Hashed {count} images...");
                }
                string? hash = ComputeSha256Hash(path);
                if (hash == null) continue;
                //we use hashmap concept here
                if (!hashMap.TryGetValue(hash, out var list))
                {
                    list = new List<string>();
                    hashMap[hash] = list;
                }

                list.Add(path);
            }

            Sha256Compare();
        }
        static string? ComputeSha256Hash(string rawData)
        {
            // Create a SHA256
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array

                using (FileStream fileStream = new FileStream(rawData, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        // Create a fileStream for the file.
                        // Be sure it's positioned to the beginning of the stream.
                        fileStream.Position = 0;
                        // Compute the hash of the fileStream.
                        byte[] hashValue = sha256Hash.ComputeHash(fileStream);
                        // Convert byte array to a string
                        StringBuilder builder = new StringBuilder();
                        for (int i = 0; i < hashValue.Length; i++)
                        {
                            builder.Append(hashValue[i].ToString("x2"));
                        }
                        return builder.ToString();
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine($"I/O Exception: {e.Message}");
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Console.WriteLine($"Access Exception: {e.Message}");
                    }

                }

            }
            return null;
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
                    for (int i = 1; i< record.Value.Count; i++)
                    {
                        SHA256_duplicate.Add(record.Value[i]);
                    }
                }

            }
            ImageScanner.RemoveDuplicate(ImageScanner.GetData(),SHA256_duplicate, NoSHA256ExactDuplicate);
            if (!duplicateFound)
            {
                Console.WriteLine("No duplicates found.");
            }
        }

        public static List<string> GetDuplidata()
        {
            return SHA256_duplicate;
        }
        public static List<string> GetNoExactDuplidata()
        {
            return NoSHA256ExactDuplicate;
        }
    }
}