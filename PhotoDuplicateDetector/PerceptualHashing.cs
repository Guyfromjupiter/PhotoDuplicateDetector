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

        //we will use buckets to store only the hash value that will be compared
        //in the list we use a concept called tuple to store vale and hash togethe, tuple is compile time struc
        //to add something in this list we add something like list.Add((path,hash))
        //to access the value we can do something like list[0].path and list[0].hash
        
        internal static Dictionary<ushort, List<(string path, ulong hash)>> Buckets =
            new Dictionary<ushort, List<(string path, ulong hash)>>();

        /*
         * had to change my resizing functionas it was calling new Bitmap(path) and GDI drawing API
         * as we are using Parallel.ForEach in Main  multiiple  image are using this and may are being resized simultaneously
         * well why did this happened? according to copilot and my understandind is that Bitmap constructor uses GDI+
         * GDI is not used for concurent use hence it sucks when we are creating multiple bitmap or graphic obj in parallel
         * this causes internal coruption or a corupted image causes it 
         * 
         * this is a defensive syc hack around GDI+
         * 
         * s_gdiLock is object used as lock s_ prefix is for static feild its naming convention
         * s_gdiLock is a key that thread competee for, a lock object
         * let say thread A enters lock => runtime checks if the any other thread has the key if not=> A runs or else => A sleep
         * when computatuion of owner thread happens, the key is released and one thread wakes up
         * 
         * what lock does?
         * lock(object)
         * {
         * 
         * }
         * 
         * is same as 
         * Monitor.Enter(obj);
         * try
         * {
         *      DOWORK();
         * }
         * finally
         * {
         *      Monitor.Exit();
         * }
         * 
         * why is lock necessary? because GDI+ is not thread safe 
         * this makes so only single thread can enter.
         * this is what you call serializzing, forrcing concurrent operation to run one afterother 
         * so that means parallelism is lost inside lock but well what can we do?
         * i would look into this later
         */

        private static readonly object s_gdiLock = new();
        [SupportedOSPlatform("windows6.1")]
        public static Bitmap PhotoResizing(string path)
        {
            lock (s_gdiLock)
            {
                using var image = new Bitmap(path);
                var resized = new Bitmap(32, 32);
                using var g = Graphics.FromImage(resized);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, 32, 32);
                return resized;
            }                   
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
         * 
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

        /*so...we have to optimize hell out of this, phash i usually used for reverse search and for the purpose i am using its slow
         * you can use dhash and ahash  and make a pieline which i might make if i want to.
         * so here in DCT we have 2 optimization 
         * cos  part is being many time so we compute it beforehand and store it in a 2d array
         * the second optimization is the for nested loop, all for loop is going to 32 so how many times is it running? 32^4 = 1048576 times
         * so we will exchange the first two for loop range to 8 because we only need 8*8 value for hash 8*8*32*32
         */
        public static double[,] DCT_2(double[,] Pixels)
        {
            int N = 32;
            double[,] DCT = new double[N, N];
            for (int u = 0; u < 8; u++)
            {
                for (int v = 0; v < 8; v++)
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

        /*
       * the key idea is locality sensitive hashing
       * 
       * so we will see first 16 bit and if hash differs in first 16 bits then mlst likely they are different
       * we will skip those 
       * if they are same then we will compare hashing distance
       * this will reduce the total amount of comparison we have to do and make it faster
       * 
       */
        public static void CreateBuckets()
        {
            foreach(var record in ImageScanner.ImageDct)
            {
                ulong hash = record.Value;

                //see we need 16 bit. The best way for it is to right shift 48 bit, so we will get 16 bits and hell lot of 0
                //ushort wil be used because it can store 16 bit value and we will save memory

                ushort bucketkey = (ushort)(hash >> 48);

                //TryGetValue is a safe dictionary lookup method that returns false instead of exception if key not found
                //out is interestin gone, this is a way to return value from the method, we can use it like this
                //so befoore reeturning it will first write in list
                //if statement checks if buketkey doesn't exist in dictionarry
                //if it doesn't trgetvalue return false and the not operator will make it true 
                //if-statement is true, we create a list and add it to the dictionary with the bucketkey as key
                //or else if bucketkey already exist, we will get the list and add the path and hash to it

                if (!Buckets.TryGetValue(bucketkey, out var list))
                {
                    list = new List<(string path, ulong hash)>();
                    Buckets[bucketkey] = list;
                }

                list.Add((record.Key, hash));
            }
        }
        public static void PhashCompare()
        {
            foreach (var bucket in Buckets.Values)
            {
                for (int i = 0; i < bucket.Count; i++)
                {
                    for (int j = i + 1; j < bucket.Count; j++)
                    {
                        int distance = HammingDistance(bucket[i].hash, bucket[j].hash);
                        ImageHammingDis[$"{bucket[i].path} <-> {bucket[j].path}"] = distance;
                    }
                }
            }
        }
    }
}    
                        
                    
                
          
