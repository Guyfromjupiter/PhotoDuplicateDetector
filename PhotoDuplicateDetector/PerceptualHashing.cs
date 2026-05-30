using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Imaging.Effects;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace PhotoDuplicateDetector
{
    class PerceptualHashing
    {
        /*perceptual hashing has four main things
         * Resizing image
         * turning it to grayscale  
         * DCT (discrete cosine transform) to get the frequency of the image
         * perceptual hashing
         * hamming distance 
         * */

        //we will try to creat our own pHash
        internal static Dictionary<string, int> ImageHammingDis = new Dictionary<string, int>();
        static double[,] cosTable = new double[32, 32];
        [SupportedOSPlatform("windows6.1")]
        public static Bitmap PhotoResizing(string path)
        {
            //resize the image to 32x32
            using var image = new Bitmap(path);
            var ResizedImage = new Bitmap(32, 32);

            using (var g = Graphics.FromImage(ResizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, 32, 32);
            }

            return ResizedImage;
        }

        //so the thing is in my previous version of dct, we are computing cos((2 * x + 1) * u * pi 
        //so we compute it beforehand and store it in 2d array
        public static void InitCosTable()
        {
            for (int x = 0; x < 32; x++)
                for (int u = 0; u < 32; u++)
                    cosTable[x, u] =
                        Math.Cos((2 * x + 1) * u * Math.PI / 64);
        }
        /*this is slower and older versionn which i used a little of my brain and callstack for
         * [SupportedOSPlatform("windows6.1")]
        public static double[,] GreyScalling(Bitmap ResizedImage)
        {
            //create a blank bitmap the same size as Resized Image
            Bitmap newBitmap = new Bitmap(ResizedImage.Width, ResizedImage.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
                    [.2126f, .2126f, .2126f, 0, 0],
                    [.7152f, .7152f, .7152f, 0, 0],
                    [.0722f, .0722f, .0722f, 0, 0],
                    [0, 0, 0, 1, 0],
                    [0, 0, 0, 0, 1]
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(ResizedImage, new Rectangle(0, 0, ResizedImage.Width, ResizedImage.Height),
               0, 0, ResizedImage.Width, ResizedImage.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            double[,] Pixels = new double[32, 32];
            for (int i = 0; i < newBitmap.Width; i++)
            {
                for (int j = 0; j < newBitmap.Height; j++)
                {
                    // Get the grayscale value from the Color object
                    Color pixelColor = newBitmap.GetPixel(i, j);
                    // Since the image is already grayscale, R, G, and B are equal
                    Pixels[i, j] = pixelColor.R;
                }
            }
            return Pixels;
        }*/

        //lockbits version
        [SupportedOSPlatform("windows6.1")]
        public static double[,] GrayScalling (Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            double[,] pixels = new double[width, height];
            Rectangle recta = new Rectangle(0, 0, width, height);

            BitmapData data = bitmap.LockBits(recta, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int stride = data.Stride;
            int bytes = stride * height;

            byte[] buffer = new byte[bytes];

            Marshal.Copy(data.Scan0, buffer, 0, bytes);
            bitmap.UnlockBits(data);

                for (int y = 0; y < height; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        int i = row + x * 3;
    
                        byte b = buffer[i];
                        byte g = buffer[i + 1];
                        byte r = buffer[i + 2];
    
                        // ITU-R BT.709 luminance (correct for pHash)
                        pixels[x, y] = 0.2126 * r + 0.7152 * g + 0.0722 * b;
                    }
                }
            return pixels;
        }

        //we use dct 2 here, a normalised version off dct for image processing 
        /*
         * c(u,v) = (1/4) * c(u) * c(v) * sum(x = 0 to x = N-1) sum(y = 0 to y = N-1) f(x,y) * cos((2 * x + 1) * u * pi / (2 * N)) * cos((2 * y + 1) * u * pi / (2 * N))
         * this is ifferent from thee mathematical version and will confuse you all so here is some thing i found out 
         * c(k) = { 1/ sqrt(2) if k = 0 , 1 K>0}
         * 1/4 is a normalisation factor to make sure the values are between 0 and 1
         * its convention for jpeg
         */
        public static double[,] DCT_2(double[,] Pixels)
        {
            int N = 32;
            double[,] DCT = new double[N, N];
            for (int u = 0; u < N; u++)
            {
                for (int v = 0; v < N; v++)
                {
                    double sum = 0.0;
                    for (int x = 0; x < N; x++)
                    {
                        for (int y = 0; y < N; y++)
                        {
                            sum += Pixels[x, y] * cosTable[x, u] * cosTable[y, v];

                        }
                    }

                    double cu = (u == 0) ? 1 / Math.Sqrt(2) : 1.0;
                    double cv = (v == 0) ? 1 / Math.Sqrt(2) : 1.0;

                    DCT[u, v] = 0.25 * cu * cv * sum;
                }
            }
            return DCT;
        }

        //phash is perceptual hashing
        /*
         * "Looks like it" = https://www.hackerfactor.com/blog/?/archives/432-Looks-Like-It.html
         * the algorithm is as follows
         * step 1 resize the image to 32x32
         * step 2 turn it to grayscale
         * step 3 get the dct of the image
         * step 4 get mean (according to the source, but in modern implementation meedian is taken)
         * step 5 compare each median value with the dct value, if dct>median then 1 or else 0
         * step 6 convet to 64 bit hash (we use two things | (binary or) and << (binary left shift)) {read me has mor of it }
         * step 7 generate hamming ddistance
         * step 8 compare hamming distance 
         * 
         * hamming distance chart
         * hamming dis   descrip
         * 0             same hash
         * 1 to 5        very similar
         * 6 to 10       similar
         * 11 to 15      can be similar 
         * 16 to 20      not similar
         * 20+           different
         */
        public static ulong Phash(double[,] DCT)
        {
            ulong hash = 0;
            int bit = 0;
            List<double> value = new List<double>();
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (!(x == 0 && y == 0))
                    {
                        value.Add(DCT[x, y]);

                    }
                }
            }
            //the lamda function to get the median of the value list
            //v => v means us the value itself as the key for sorting 
            double median = value.OrderBy(v => v).ElementAt(value.Count / 2);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (x == 0 && y == 0) continue;

                    if (DCT[x, y] > median)
                        {
                            hash |= (1UL << bit);
                        }
                    
                    bit++;
                }
            }
            return hash;
        }
        public static int HammingDistance(ulong hash1, ulong hash2)
        {
            ulong a = hash1 ^ hash2;
            int distance = 0;
            while (a != 0)
            {
                distance += (int)(a & 1);
                a >>= 1;
            }
            return distance;
        }
        public static void PhashCompare()
        {
            var list_to_fasten = ImageScanner.ImageDct.ToList();
            for(int i = 0; i<list_to_fasten.Count();i++)
            {
                for (int j = i + 1; j < list_to_fasten.Count(); j++)
                {
                    int z = HammingDistance(list_to_fasten[i].Value, list_to_fasten[j].Value);
                    ImageHammingDis.Add($"{list_to_fasten[i].Key} and {list_to_fasten[j].Key}", z);
                }    
                
            }
            foreach(var distance in ImageHammingDis)
            {
                Console.WriteLine($"{distance.Key} has a hamming distance of {distance.Value}");
                if(distance.Value<=9)
                {
                    Console.WriteLine($"{distance.Key} are similar with a hamming distance of {distance.Value}");
                }
            }
        }
    }
}    
                        
                    
                
          
