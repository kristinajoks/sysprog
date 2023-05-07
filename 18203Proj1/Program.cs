//using Newtonsoft.Json.Linq;
using System.Net;
using System.Diagnostics;
using System.Drawing;
//using Aspose.Imaging;
//using Image = Aspose.Imaging.Image; 
//using Size = System.Drawing.Size;


class Program
{
    static HttpListener listener = new HttpListener();
    static HttpClient client = new HttpClient();

    //static void ServerThreadPool() 
    //{
    //    try //ukoliko je ovde, nakon prve nedostupne slike se zatvara program, ukoliko je unutar while radi i nakon
    //    {
    //        listener.Prefixes.Add("http://localhost:5050/");
    //        listener.Start();

    //        Console.WriteLine("Listening on port 5050...");

    //        while (true)
    //        {
    //            try
    //            {
    //                HttpListenerContext context = listener.GetContextAsync().Result; //isto kao iznad zbog .Result
    //                HttpListenerRequest request = context.Request;

    //                Console.WriteLine($"Recieved request for {request.Url}");

    //                if (context.Request.Url == null)
    //                {
    //                    throw new HttpRequestException();
    //                }

    //                string localPath = "C:\\Users\\krist\\Downloads\\3. godina\\SP\\18203Proj1\\photos\\";
    //                string path = localPath + context.Request.Url.LocalPath;

    //                HttpListenerResponse response = context.Response;
    //                response.Headers.Set("Content-type", "image/jpg");

    //                byte[] buffer = File.ReadAllBytes(path);

    //                //odavde proba sa thread pool-om
    //                int broj_niti = Environment.ProcessorCount;
    //                Console.WriteLine($"Dostupan broj niti je: {broj_niti}");
    //                int worker_niti, io_niti;
    //                ThreadPool.GetAvailableThreads(out worker_niti, out io_niti);


    //                //treba izmeniti kod tako da nekako prolazi po delovima kroz strukturu, nit izvrsava izmenu za deo
    //                //i upisuje u zajednicku strukturu na definisano mesto
    //                ThreadPool.QueueUserWorkItem((state) =>
    //                {
    //                    //ovako ce se nakon spajanja prikazati konacni rezultat
    //                    Stream stream = response.OutputStream;

    //                    string respath;

    //                    Image image = Image.Load(path);
    //                    using (image)
    //                    {
    //                        respath = Convert(path, image);
    //                        byte[] res = File.ReadAllBytes(respath);
    //                        response.ContentLength64 = res.Length;
    //                        stream.Write(res, 0, res.Length);
    //                    }
    //                });

    //                bool gotova_obrada = false;
    //                while (!gotova_obrada)
    //                {
    //                    Thread.Sleep(1000);
    //                    gotova_obrada = ThreadPool.PendingWorkItemCount == 0; //cekamo da obrada bude gotova
    //                }                                       
    //            }
    //            catch(FileNotFoundException  ex)
    //            {
    //                Console.WriteLine(ex.Message);
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex.Message);
    //    }
    //}

    public static void ServerDivided()
    {
        listener.Prefixes.Add("http://localhost:5050/");
        listener.Start();

        Console.WriteLine("Listening on port 5050...");

        while (true)
        {
            try
            {
                HttpListenerContext context = listener.GetContextAsync().Result; //isto kao iznad zbog .Result
                HttpListenerRequest request = context.Request;

                Console.WriteLine($"Recieved request for {request.Url}");

                if (context.Request.Url == null)
                {
                    throw new HttpRequestException();
                }

                string localPath = "C:\\Users\\krist\\Downloads\\3. godina\\SP\\18203Proj1\\photos\\";
                string path = localPath + context.Request.Url.LocalPath;

                HttpListenerResponse response = context.Response;
                response.Headers.Set("Content-type", "image/jpg");

                byte[] buffer = File.ReadAllBytes(path);

                Stream stream = response.OutputStream;

                if (request.Url.AbsolutePath == "/favicon.ico")
                {
                    stream.Write(buffer, 0, buffer.Length);
                    return;
                }

                string respath;

                Bitmap imgFile = new Bitmap(path);
                Bitmap[,] tiles = makeTiles((object)imgFile);
                Bitmap[,] res = parallelImageProcess(tiles);
                Bitmap joined = joinTiles(res, imgFile);

                

                byte[] resArray = toByteArr(joined, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Write(resArray, 0, resArray.Length);
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
                Rectangle movingTile = new Rectangle(i* tilesize.Width, j*tilesize.Height, tilesize.Width, tilesize.Height);

                bmparray[i,j]= new Bitmap(tilesize.Width, tilesize.Height);

                //ubacivanje slika u bmparray
                using (Graphics canvas = Graphics.FromImage(bmparray[i, j]))
                {
                    canvas.DrawImage(bmp, new Rectangle(0, 0, tilesize.Width, tilesize.Height), movingTile, GraphicsUnit.Pixel);
                }
            }
        }

        return bmparray;
    }

    
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
                lock (bitmap)
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
            });
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
                    //proba sa param u rect
                    canvas.DrawImage(bitmaps[i,j], new Rectangle(i * tilesize.Width, j * tilesize.Height, tilesize.Width, tilesize.Height), movingTile, GraphicsUnit.Pixel);
                }
            }
        }

        return res;
    }

    //funkcija koja preuzima putanju slike i sliku, a vraca putanju crno-bele slike
    //static string Convert(string path, Image image)
    //{
    //    RasterCachedImage rasterCachedImage = (RasterCachedImage)image;
    //    if (!rasterCachedImage.IsCached)
    //    {
    //        rasterCachedImage.CacheData();
    //    }
    //    //sta ako jeste??

    //    path = path.Substring(0, path.Length - 4);
    //    string newpath = $"{path}grayscaled.jpg";
    //    rasterCachedImage.Grayscale();
    //    rasterCachedImage.Save(newpath);

    //    return newpath;
    //}

    //static void Server()
    //{
    //    listener.Prefixes.Add("http://localhost:5050/");
    //    listener.Start();

    //    Console.WriteLine("Listening on port 5050...");

    //    while (true)
    //    {
    //        try
    //        {
    //            //HttpListenerContext context = listener.GetContext(); //ceka zahtev i vraca njega kada pristigne
    //            //HttpListenerRequest request = context.Request;

    //            HttpListenerContext context = listener.GetContextAsync().Result; //isto kao iznad zbog .Result
    //            HttpListenerRequest request = context.Request;

    //            Console.WriteLine($"Recieved request for {request.Url}");

    //            if (context.Request.Url == null)
    //            {
    //                throw new HttpRequestException();
    //            }

    //            string localPath = "C:\\Users\\krist\\Downloads\\3. godina\\SP\\18203Proj1\\photos\\";
    //            string path = localPath + context.Request.Url.LocalPath;

    //            HttpListenerResponse response = context.Response;
    //            response.Headers.Set("Content-type", "image/jpg");

    //            byte[] buffer = File.ReadAllBytes(path);
    //            //response.ContentLength64 = buffer.Length;

    //            //ovako ce se nakon spajanja prikazati konacni rezultat
    //            Stream stream = response.OutputStream;
    //            //stream.Write(buffer, 0, buffer.Length);


    //            if(request.Url.AbsolutePath == "/favicon.ico")
    //            {
    //                stream.Write(buffer, 0, buffer.Length);
    //                return;
    //            }

    //            string respath;

    //            Image image = Image.Load(path);
    //            using (image)
    //            {
    //                respath = Convert(path, image);
    //                byte[] res = File.ReadAllBytes(respath);
    //                response.ContentLength64 = res.Length;
    //                stream.Write(res, 0, res.Length);
    //            }
    //        }
    //        catch (FileNotFoundException ex)
    //        {
    //            Console.WriteLine(ex.Message);
    //        }
    //    }
    //}

    static void Main(string[] args)
    {
        Console.WriteLine("Krece glavna nit...");
        ServerDivided();
    }

}