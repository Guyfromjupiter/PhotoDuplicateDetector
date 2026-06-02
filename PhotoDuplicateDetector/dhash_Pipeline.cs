using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoDuplicateDetector
{
    class dhash_Pipeline
    {

        private static List<String> Dhash_duplicate = new List<String>();
        internal static Dictionary<string, ulong> Dhashdictionary = new Dictionary<string, ulong>();
        private static List<string> NoDhashExactDuplicate = new List<string>();
        internal static Dictionary<ushort, List<(string path, ulong hash)>> dhash_Buckets =
    new Dictionary<ushort, List<(string path, ulong hash)>>();

        public static void Sub2()
        {
            int dhash_count = 0;

            Parallel.ForEach(SHA256HashLayer.GetNoExactDuplidata(), path =>
            {
                ulong hash;
                using (Bitmap resizeDhash = PerceptualHashing.PhotoResizing(path, 9, 8))
                {
                    double[,] gray_dhash = PerceptualHashing.GrayScalling(resizeDhash);
                    hash = dhash(gray_dhash);
                }
                lock (Dhashdictionary)
                {
                    Dhashdictionary[path] = hash;
                    dhash_count++;
                    if (dhash_count % 100 == 0)
                    {
                        Console.WriteLine($"Dhashed {dhash_count} images...");
                    }

                }
            });

            DhashCreateBuckets(Dhashdictionary);
            PerceptualHashing.Compare(5,dhash_Buckets,SHA256HashLayer.GetNoExactDuplidata(),Dhash_duplicate, NoDhashExactDuplicate);

        }
        public static ulong dhash(double[,] gray)
        {
            int columns = gray.GetLength(0);
            int rows = gray.GetLength(1);
            ulong hash = 0;
            int bit = 0;
            // 0 1 2  3  4 5 6 7 
            for (int i = 0; i< rows; i++)
            {
                //0 1 2 3 4 5 6 7 8 
                for (int j = 0; j <  columns - 1; j++)
                {
                    if (gray[j , i] < gray[j + 1 , i])
                    {
                        hash |= (1UL << bit);
                    }
                    bit++;
                }
            }
            return hash;
        }
        public static void DhashCreateBuckets(Dictionary<string, ulong> dic)
        {

            dhash_Buckets.Clear();
            foreach (var record in dic)
            {
                ulong hash = record.Value;



                ushort bucketkey = (ushort)(hash & 0xFFFF); // lowest 16 bits


                if (!dhash_Buckets.TryGetValue(bucketkey, out var list))
                {
                    list = new List<(string path, ulong hash)>();
                    dhash_Buckets[bucketkey] = list;
                }

                list.Add((record.Key, hash));
            }
        }
        public static List<string> GetdhashDuplidata()
        {
            return Dhash_duplicate;
        }
        public static List<string> GetNoDhashExactDuplidata()
        {
            return NoDhashExactDuplicate;
        }

    }
}
