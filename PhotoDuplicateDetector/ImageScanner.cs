// For Directory.GetFiles and Directory.GetDirectories
// For File.Exists, Directory.Exists

using System.Collections;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;

namespace PhotoDuplicateDetector
{
    public class ImageScanner
    {
        //hard coded list of image paths, the list is strictly of type string
        //this list will store every image path
        private static List<string> ImagePath = new List<string>();
        private static List<string> InputPath = new List<string>();
        internal static Dictionary<string, ulong> ImageDct = new Dictionary<string, ulong>();

        public static void Main(string[] args)
        {
            InputPath.Clear();
            ImagePath.Clear();
            SHA256HashLayer.hashMap.Clear();
            SHA256HashLayer.GetNoExactDuplidata().Clear();
            SHA256HashLayer.GetDuplidata().Clear();
            PerceptualHashing.ImageHammingDis.Clear();
            // Get the path to the directory or file to process from the user.

            GetNumberOfInputPaths();
            
            
            foreach (string path in InputPath)
            {
                GetImagePaths(path);
            }

            Console.WriteLine($"Total images found: {ImagePath.Count}");

            SHA256HashLayer.Sub1();

            Console.WriteLine($"Total images found when duplicate are removed: {SHA256HashLayer.GetNoExactDuplidata().Count}");
            PerceptualHashing.InitCosTable();
            int count = 0;

            //this is where we will compute the perceptual hash for each image and store it in the ImageDct dictionary
            //, we will also print out the progress every 100 images
            Parallel.ForEach(SHA256HashLayer.GetNoExactDuplidata(), path =>
            {
                ulong hash;
                using (Bitmap res = PerceptualHashing.PhotoResizing(path))
                {
                    double[,] gray = PerceptualHashing.GrayScalling(res);
                    double[,] dct = PerceptualHashing.DCT_2(gray);
                    hash = PerceptualHashing.Phash(dct);
                }
                lock (ImageDct)
                {
                    ImageDct[path] = hash;
                    count++;
                    if (count % 100 == 0)
                    {
                        Console.WriteLine($"Hashed {count} images...");
                    }
                }
            });
            PerceptualHashing.CreateBuckets();
            PerceptualHashing.PhashCompare();

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        public static void GetNumberOfInputPaths()
        {
            while (true)
            {
                Console.WriteLine("How many paths do you want to scan?");
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int numPaths) && numPaths > 0)
                {
                    for (int i = 0; i < numPaths; i++)
                    {
                        Console.WriteLine("Enter target directory or file path:");
                        string? path = Console.ReadLine();

                        if (!string.IsNullOrWhiteSpace(path))
                            InputPath.Add(path);
                        else
                            i--; // retry
                    }
                    break;
                }

                Console.WriteLine("Invalid input. Try again.");
            }
        }

        public static void GetImagePaths(string path)
        {
            if (File.Exists(path))
            {
                // This path is a file
                ProcessFile(path);
            }
            else if (Directory.Exists(path))
            {
                // This path is a directory
                ProcessDirectory(path);
            }
            else
            {
                Console.WriteLine("{0} is not a valid file or directory.", path);
            }
        }


        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public static void ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        public static void ProcessFile(string path)
        {
            // This is where we will check if the file is an image file, if it is we will add it to the list of image paths
            if (IsImageFile(path))
            {
                ImagePath?.Add(path);
            }

        }
        public static void printImagePaths()
        {
            //this is for tesing purposes
            for (int i = 0; i < ImagePath.Count; i++)
            {
                Console.WriteLine(i + " " + ImagePath[i]);
            }
        }
        public static bool IsImageFile(string path)
        {
            //it is the main condition checker to determine i.f the file type is what is authorized or not
            string extension = Path.GetExtension(path).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp";
            //.jpg .jpeg .png .bmp


        }
        public static IReadOnlyList<string> GetData()
        {
            return ImagePath.AsReadOnly();
        }
   
    }
}
