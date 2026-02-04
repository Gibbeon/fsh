using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Pipes;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

public static class Find
{
    public class StatusChangedEventArgs : EventArgs
    {
        public string Status { get; private set; }

        public StatusChangedEventArgs(string status)
        {
            Status = status;
        }    
    }

    public class TileChangedEventArgs : EventArgs
    {
        public Bitmap Image { get; private set; }
        public Rectangle Area { get; private set; }
        public double Brightness { get; private set; }

        public TileChangedEventArgs(Bitmap image, Rectangle area, double brightness)
        {
            Image = image;
            Area = area;
            Brightness = brightness;
        }    
    }

    public const int TILE_SIZE = 64;
    public const int X_PADD = 10;
    public const int Y_PADD = 6;
    public const float BOBBER_TOLERANCE = .0005f;
    public const float FIND_FISH_TOLERANCE = .0025f;
    public const int TEST_BOBBER_DELAY = 100;
    public const int TEST_BOBBER_DELAY_VARIANCE = 20;
    public const int TEST_DURATION = 30;
    public const int HUMAN_REACTION = 100;
    public const int HUMAN_REACTION_VARIANCE = 75;
    public static Random random = new Random();   
    
    public static event EventHandler<TileChangedEventArgs>? SelectedTileChanged;
    public static event EventHandler<TileChangedEventArgs>? TestedTileChanged;
    public static event EventHandler<StatusChangedEventArgs>? StatusChanged;
    public static Point RandomPointOutOfBounds()
    {
        return new Point(
            (int)(random.NextDouble() * X_PADD * TILE_SIZE),
            (int)(random.NextDouble() * Y_PADD * TILE_SIZE));
    }

    public static int GetTestDelay(int duration, int variance)
    {
        return (int)(duration + random.NextDouble() * variance);
    }

    public static int GetVariance(int min, int max)
    {
        return (int)Math.Round(random.NextDouble() * (max - min)) + min;
    }
    public static bool HasFish(Rectangle tile)
    {        
        DateTime lastLogTime = DateTime.Now;
        float maxSoundLevel = 0f;
        const float FISH_DETECTION_THRESHOLD = .30f;
        Thread.Sleep(100);
        int count = 0;
        while (true)
        {
            //Console.Write("{0} ", DateTime.Now - lastLogTime);
            float currentSoundLevel = AudioUtils.GetMasterVolumeLevel();
            
            if (currentSoundLevel > maxSoundLevel)
            {
                maxSoundLevel = currentSoundLevel;
            }
            
            if (currentSoundLevel >= FISH_DETECTION_THRESHOLD)
            {
                count++;

                if(count > 4)
                {
                    Console.WriteLine($"[FishingStateMachine]: FISH DETECTED! Sound level: {currentSoundLevel:F4} (threshold: {FISH_DETECTION_THRESHOLD})");
                        return true;
                }
            } else
            {
                count = 0;
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(50));

            if(DateTime.Now - lastLogTime > TimeSpan.FromSeconds(30))
                return false;
        }
    }
    public static bool Started => started;
    static bool started = false;
    static Thread? thread;

    private static void ChangeStatus(string message)
    {
        Console.WriteLine(message);
        if(StatusChanged != null) StatusChanged(null, new StatusChangedEventArgs(message));
    }    
    
    private static void ChangeSelectedTile(Bitmap image, Rectangle area, double brightness)
    {
        if(SelectedTileChanged != null) SelectedTileChanged(null, new TileChangedEventArgs(image, area, brightness));
    }    
    private static void ChangeTestedTile(Bitmap image, Rectangle area, double brightness)
    {
        
        Console.WriteLine("[Tile Founds]: {0}, {1}, {2:F8}", area.Left, area.Top, brightness);
        if(TestedTileChanged != null) TestedTileChanged(null, new TileChangedEventArgs(image, area, brightness));
    }

    public static void Toggle()
    {
        if (started)
        {
            ChangeStatus("User Stopping");
            started = false;
            if(thread != null)
            {
                thread.Join();
                thread = null;
            }
            ChangeStatus("User Stopped");
        }
        else
        {
            ChangeStatus("User Starting");
            thread = new Thread(new ThreadStart(() => Loop()));
            thread.Start();
            ChangeStatus("User Started");
        }
    }

    public static void Loop()
    {
        ChangeStatus("Loop Starting");
        ScreenCapture.Activate();

        started = true;

        while (started)
        {
            if (FindFish())
            {
                ChangeStatus("Found Fish");
            }
            else
            {
                ChangeStatus("Not Found Fish");
            }

            Thread.Sleep(GetTestDelay(400, 250));
        }

        ChangeStatus("Loop Stopping");
    }

    public static bool FindFish()
    {
        if (FindBobber(out Rectangle area))
        {
            ChangeStatus(string.Format("Found Bobber"));

            if (HasFish(area))
            {
                var delay = GetTestDelay(HUMAN_REACTION, HUMAN_REACTION_VARIANCE);

                ChangeStatus(string.Format("Fish Detected - delay {0}", delay));
                Thread.Sleep(delay);
                ScreenCapture.Activate();
                MouseUtils.SendMouseInput(MouseButtons.Right);

                delay = GetTestDelay(HUMAN_REACTION, HUMAN_REACTION_VARIANCE);
                
                ChangeStatus(string.Format("Clicked On Fish - delay {0}", delay));
                Thread.Sleep(delay);
                return true;
            }
        }

        ChangeStatus(string.Format("No Bobber Found"));
        Thread.Sleep(GetTestDelay(HUMAN_REACTION + 2000, HUMAN_REACTION_VARIANCE));
        return false;
    }

    public static bool FindBobber(out Rectangle area)
    {
        var resetPoint = RandomPointOutOfBounds();
        ChangeStatus(string.Format("Reset Mouse to {0}, {1}", resetPoint.X, resetPoint.Y));
        // reset mouse
        MouseUtils.SmoothMoveTo(resetPoint);

        ChangeStatus("Casting Line");
        KeyboardUtils.SendKeyInput(Keys.D1);

        Thread.Sleep(1000+GetTestDelay(200, HUMAN_REACTION_VARIANCE));
        // take screenshot
        
        Bitmap img = (Bitmap)ScreenCapture.CaptureScreen();
        Size numTiles = GetNumTiles(img.Size);

        var x_center = numTiles.Width / 2;
        var y_center = numTiles.Height / 2;
        var search_size = (numTiles.Height - Y_PADD) / 2 + 2;

        var tiles = GetArea(new Rectangle((x_center - 4) * TILE_SIZE, (y_center - 8)* TILE_SIZE, 8 * TILE_SIZE, 16 * TILE_SIZE));

        Rectangle candidate = Rectangle.Empty;
        double redValue = double.Epsilon;

        foreach(var tile in tiles)
        {
            img = (Bitmap)ScreenCapture.CaptureScreen();

            var hasRed = GetAverageRedUnsafe(img, tile);

            ChangeSelectedTile(img, tile, hasRed);

            //ChangeStatus(string.Format("Red {0:P12} [{1},{2}]", hasRed, tile.Left, tile.Top));
            
            if(hasRed > redValue)
            {
                ChangeTestedTile(img, tile, hasRed);



                redValue = hasRed;
                candidate = tile;
                continue;
            }
        }

        if(redValue > float.Epsilon)
        {
            var tileImage = img.Clone(candidate, img.PixelFormat);
            tileImage.Save("tile.bmp");
            area = candidate;

            double red = 0.0f;
            Rectangle targetRect = Rectangle.Empty;
            foreach(var subTile in GetArea(new Rectangle(0, 0, tileImage.Width, tileImage.Height), 8))
            {
                var hasRed = GetAverageRedUnsafe(tileImage, subTile);
                
                if(hasRed > red)
                {
                    red = hasRed;
                    targetRect = subTile;
                }
            }
            
            var testPoint = new Point(candidate.Left + targetRect.Left + 8 / 2 + GetVariance(-2, 2), candidate.Top + targetRect.Top + 8 / 2 + GetVariance(-2, 2));
            
            ChangeStatus(string.Format("Moving to {0}, {1} [{2},{3}]", testPoint.X, testPoint.Y, candidate.Left, candidate.Top));
            MouseUtils.SmoothMoveTo(testPoint);
            
            return true;
        }

        ChangeStatus(string.Format("Not Found!"));

        area = Rectangle.Empty;
        return false;
    }

    public unsafe static double GetAverageBrightnessUnsafe(Bitmap bmp, Rectangle rect)
    {
        //BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
        BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
        // Get the address of the first line.
        byte* src = (byte*)bmpData.Scan0;

        // Assuming 24bpp or 32bpp, adjust the loop if necessary
        int pixelSize = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
        uint totalBrightness = 0;

        Parallel.For(rect.Top, rect.Bottom, y =>
        {
            int lineIndex = y * bmpData.Stride;
            for (int x = rect.Left; x < rect.Right; x++)
            {
                int i = lineIndex + x * pixelSize;
                // The data is typically stored as BGR
                byte b = src[i];
                byte g = src[i + 1];
                byte r = src[i + 2];

                // Calculate perceived brightness/luminance using a standard formula
                // The Color.GetBrightness() algorithm is different, but this is a common alternative.
                uint brightness = (uint)((0.2126 * r + 0.7152 * g + 0.0722 * b) * short.MaxValue);
                
                // Use Interlocked.Add for thread-safe accumulation if using Parallel.For
                Interlocked.Add(ref totalBrightness, brightness);
            }
        });

        bmp.UnlockBits(bmpData);

        return (totalBrightness / short.MaxValue) / (bmp.Width * bmp.Height * 255.0); // Normalize to 0-1 range
    }

    public unsafe static double GetAverageRedUnsafe(Bitmap bmp, Rectangle rect)
    {
        //BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
        BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
        // Get the address of the first line.
        byte* src = (byte*)bmpData.Scan0;

        // Assuming 24bpp or 32bpp, adjust the loop if necessary
        int pixelSize = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
        uint totalBrightness = 0;

        Parallel.For(rect.Top, rect.Bottom, y =>
        {
            int lineIndex = y * bmpData.Stride;
            for (int x = rect.Left; x < rect.Right; x++)
            {
                int i = lineIndex + x * pixelSize;
                // The data is typically stored as BGR
                byte b = src[i];
                byte g = src[i + 1];
                byte r = src[i + 2];

                if((float)r / (r + b + g + 1.0f) > .60f)

                //if((r > 96) && (b < 96 && g < 96))
                {
                    Interlocked.Add(ref totalBrightness, (uint)short.MaxValue);
                }

            }
        });

        bmp.UnlockBits(bmpData);

        return (totalBrightness / short.MaxValue) / (bmp.Width * bmp.Height * 255.0); // Normalize to 0-1 range
    }

    public static IEnumerable<Rectangle> GetArea(Rectangle region, int tileSize = TILE_SIZE)
    {
        var size = GetNumTiles(region.Size, tileSize);
        for (var x = 0; x < size.Width; x++) {
        for (var y = 0; y < size.Width; y++) {
            yield return new Rectangle(region.Left + x * tileSize, region.Top + y* tileSize, tileSize, tileSize);
        }
        }
    }

    public static IEnumerable<Rectangle> GetOutline(Rectangle region)
    {
        var size = GetNumTiles(region.Size);
        var y = 0;

        // top row
        for (var x = 0; x < size.Width; x++) {
            yield return new Rectangle(region.Left + x * TILE_SIZE, region.Top + y* TILE_SIZE, TILE_SIZE, TILE_SIZE);
        }

        if(size.Height > 1)
        {
            for (var x = 0; x < size.Width; x++) {
                yield return new Rectangle(region.Left + x * TILE_SIZE, 
                    region.Top + (size.Height - 1) * TILE_SIZE, 
                    TILE_SIZE, 
                    TILE_SIZE);
            }      
        }

        if(size.Height > 2)
        {
            // sides
            for (y = 1; y < size.Height - 2; y++) {
                yield return new Rectangle(region.Left, 
                    region.Top + y * TILE_SIZE, 
                    TILE_SIZE, 
                    TILE_SIZE);

                yield return new Rectangle(region.Left + (size.Width - 1) * TILE_SIZE, 
                    region.Top + y, 
                    TILE_SIZE, 
                    TILE_SIZE);
            }
        }
    }

    public static Size GetNumTiles(Size rect, int tileSize = TILE_SIZE)
    {
        return new Size(rect.Width / tileSize, rect.Height / tileSize);
    }
}