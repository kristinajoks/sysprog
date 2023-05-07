using System.Net;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace _18203Proj1
{
    class Program
    {
        static HttpListener listener = new HttpListener();
        static HttpClient client = new HttpClient();
        static LocalCache cache = new LocalCache();
        static Stopwatch stopwatch = new Stopwatch();

        public static void ServerDivided()
        {
            listener.Prefixes.Add("http://localhost:5050/");
            listener.Start();

            Console.WriteLine("Listening on port 5050...\n");

            while (true)
            {
                try
                {
                    HttpListenerContext context = listener.GetContextAsync().Result;
                    HttpListenerRequest request = context.Request;

                    Console.WriteLine($"Recieved request for {request.Url}");

                    stopwatch.Restart();

                    if (request.Url == null)
                    {
                        throw new HttpRequestException();
                    }


                    HttpListenerResponse response = context.Response;
                    response.Headers.Set("Content-type", "image/jpg");

                    Stream stream = response.OutputStream;

                    Bitmap final;

                    if (cache.containReq(request.Url.ToString()))
                    {
                        Console.WriteLine("Found in cache");
                        cache.tryGetValue(request.Url.ToString(), out final);
                    }
                    else
                    {
                        Console.WriteLine("Not found in cache");
                        string localPath = "C:\\Users\\krist\\sysprog\\18203Proj1\\photos\\";
                        string path = localPath + request.Url.LocalPath;

                        byte[] buffer = File.ReadAllBytes(path);

                        if (request.Url.AbsolutePath == "/favicon.ico") 
                        {
                            stream.Write(buffer, 0, buffer.Length);
                            cache.addReq(request.Url.ToString(), new Bitmap(path)); //revise
                            return;
                        }

                        Bitmap imgFile = new Bitmap(path);
                        Bitmap[,] tiles = makeTiles((object)imgFile);
                        Bitmap[,] res = parallelImageProcess(tiles);
                        final = joinTiles(tiles, imgFile);

                        cache.addReq(request.Url.ToString(), final);
                    }

                    byte[] resArray = toByteArr(final, System.Drawing.Imaging.ImageFormat.Bmp);
                    stream.Write(resArray, 0, resArray.Length);

                    stopwatch.Stop();

                    Console.WriteLine($"Response time: {stopwatch.Elapsed}\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static byte[] toByteArr(Bitmap bitmap, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                return ms.ToArray();
            }
        }

        public static Bitmap[,] makeTiles(object bmap)
        {
            Bitmap bmp = (Bitmap)bmap;
            Size tilesize = new Size(bmp.Width / 4, bmp.Height / 3);
            Bitmap[,] bmparray = new Bitmap[4, 3];

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    //srcTile
                    Rectangle movingTile = new Rectangle(i * tilesize.Width, j * tilesize.Height, tilesize.Width, tilesize.Height);

                    bmparray[i, j] = new Bitmap(tilesize.Width, tilesize.Height);

                    //ubacivanje slika u bmparray
                    using (Graphics canvas = Graphics.FromImage(bmparray[i, j]))
                    {
                        canvas.DrawImage(bmp, new Rectangle(0, 0, tilesize.Width, tilesize.Height), movingTile, GraphicsUnit.Pixel);
                    }
                }
            }

            return bmparray;
        }
        
        static object locker = new object();

        public static Bitmap[,] parallelImageProcess(Bitmap[,] bmp)
        {

            int broj_niti = Environment.ProcessorCount;
            Console.WriteLine($"Dostupan broj niti je: {broj_niti}");
            int worker_niti, io_niti;
            ThreadPool.GetAvailableThreads(out worker_niti, out io_niti);

            foreach (Bitmap bitmap in bmp)
            {
                ThreadPool.QueueUserWorkItem((state) =>
                {
                    try
                    {
                        lock (locker)
                        {
                            int width = bitmap.Width;
                            int height = bitmap.Height;

                            for (int i = 0; i < width; i++)
                            {
                                for (int j = 0; j < height; j++)
                                {
                                    Color oldPixel = bitmap.GetPixel(i, j);

                                    int grayScale = (int)((oldPixel.R * 0.229) + (oldPixel.G * 0.587) + (oldPixel.B * 0.114));
                                    Color newPixel = Color.FromArgb(grayScale, grayScale, grayScale);

                                    bitmap.SetPixel(i, j, newPixel);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });
            }

            bool done = false; 
            while (!done)
            {
                Thread.Sleep(1000); 
                done = ThreadPool.PendingWorkItemCount == 0;
            }

            return bmp;
        }

        public static Bitmap joinTiles(Bitmap[,] bitmaps, Bitmap original)
        {
            Size tilesize = new Size(original.Width / 4, original.Height / 3);
            Bitmap res = new Bitmap(original.Width, original.Height);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    //srcTile
                    Rectangle movingTile = new Rectangle(0, 0, tilesize.Width, tilesize.Height);


                    //ubacivanje slika u rezultujucu mapu
                    using (Graphics canvas = Graphics.FromImage(res))
                    {
                        canvas.DrawImage(bitmaps[i, j], new Rectangle(i * tilesize.Width, j * tilesize.Height, tilesize.Width, tilesize.Height), movingTile, GraphicsUnit.Pixel);
                    }
                }
            }

            return res;
        }

        static void Main(string[] args)
        {
            ServerDivided();                       
        }

    }

}