// For Directory.GetFiles and Directory.GetDirectories
// For File.Exists, Directory.Exists

using System.Collections;
using System.Security.Cryptography.X509Certificates;

namespace PhotoDuplicateDetector
{
    public class ImageScanner
    {
        //hard coded list of image paths, the list is strictly of type string
        //this list will store every image path
        private static List<string> ImagePath = new List<string>();
        private static List<string> InputPath = new List<string>();

        public static void Main(string[] args)
        {
            // Get the path to the directory or file to process from the user.
            GetNumberOfInputPaths();
            InputPath.Clear();
            ImagePath.Clear();
            HashLayer.hashMap.Clear();
            foreach (string path in InputPath)
            {
                GetImagePaths(path);
            }
            HashLayer.Sub1();
        }
        
        public static void GetNumberOfInputPaths()
        {
            Console.WriteLine("how many paths do you want to scan?");
            string? NumPathsInput = Console.ReadLine();

            //check if the user inpout is empty or have white space
            if (string.IsNullOrWhiteSpace(NumPathsInput))
            {
                Console.WriteLine("Input cannot be empty. Exiting.");
                return;
            }

            // Convert the user input to an integer
            if (!int.TryParse(NumPathsInput, out int numPaths) || numPaths < 1)
            {
                Console.WriteLine("Invalid number of paths. Exiting.");
                return;
            }

            for (int i = 0; i < numPaths; i++)
            {
                Console.WriteLine("Enter target directory or file path:");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Input cannot be empty. Please enter a valid path.");
                    i--;
                    continue;
                }
                InputPath.Add(input);
            }
        }


        public static void GetImagePaths(string rootPath)
        {
            //for single file
            if (File.Exists(rootPath))
            {
                ProcessFile(rootPath);
                return;
            }

            //for invalid path
            if (!Directory.Exists(rootPath))
            {
                Console.WriteLine($"Invalid path: {rootPath}");
                return;
            }

            //for directory
            Stack<string> dirs = new Stack<string>();
            dirs.Push(rootPath);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();

                try
                {
                    foreach (var file in Directory.EnumerateFiles(currentDir))
                        ProcessFile(file);

                    foreach (var dir in Directory.EnumerateDirectories(currentDir))
                        dirs.Push(dir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing directory {currentDir}: {ex.Message}");
                }
            }
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