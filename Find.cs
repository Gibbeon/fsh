using System;
using System.Drawing;

public static class Find
{
    public const int TILE_SIZE = 32;
    public const int X_PADD = 10;
    public const int Y_PADD = 6;
    public const float BOBBER_TOLERANCE = .1f;
    public const float FIND_FISH_TOLERANCE = .1f;

    public const int TEST_BOBBER_DELAY = 100;
    public const int TEST_BOBBER_DELAY_VARIANCE = 20;

    public const int TEST_DURATION = 30;

    static var random = new Random();

    public Point RandomPointOutOfBounds()
    {
        return new Point(
            random.GetDouble() * X_PADD * TILE_SIZE,
            random.GetDouble() * Y_PADD * TILE_SIZE);
    }

    public int GetTestDelay(int duration, int variance)
    {
        return duration + random.GetDouble() * variance;
    }

    public static bool HasFish()
    {
        DateTime startTime = DateTime.Now();
        Bitmap img = (Bitmap)ScreenCapture.CaptureScreen();
        var brightness = GetAverageBrightnessUnsafe(img, tile);
        while (true)
        {
            var newBrightness = GetAverageBrightnessUnsafe(img, tile);

            if (oldBrightness < (newBrightness - BOBBER_TOLERANCE))
            {
                return true;
            }

            Thread.Sleep(GetTestDelay(TEST_BOBBER_DELAY, TEST_BOBBER_DELAY_VARIANCE));

            if ((DateTime.Now - startTime).TotalSeconds > TEST_DURATION)
            {
                return false;
            }
        }

        return false;
    }

    public static void Toggle()
    {
        if (started)
        {
            thread.Abort();
        }
        else
        {
            thread = new ThreadStart(() => this.Loop());
        }
    }

    public static void Loop()
    {
        while (true)
        {
            if (Find())
            {
                Console.WriteLine("Found Fish");
            }
            else
            {
                Console.WriteLine("Not Found Fish");
            }

            Thread.Sleep(GetTestDelay(250, 250));
        }
    }

    public static bool Find()
    {
        if (FindBobber())
        {
            if (HasFish())
            {
                Mouse.RightClick();
                return true;
            }
        }

        return false;
    }

    public static bool FindBobber()
    {
        // reset mouse
        Mouse.SmoothMoveTo(RandomPointOutOfBounds());
        // take screenshot
        Bitmap img = (Bitmap)ScreenCapture.CaptureScreen();
        // move mouse to position 1
        Size numTiles = GetNumTiles(img.Bounds);
        // take screenshot
        var x_center = numTiles.Width / 2;
        var y_center = numTiles.Height / 2;
        var x_size = (numTiles.Width - X_PADD) / 2;
        var y_size = (numTiles.Height - Y_PADD) / 2;
        for (var x = 1; x < x_size; x++)
        {
            for (var y = 1; y < y_size; y++)
            {
                var area = new Rectangle((x_center - x) * TILE_SIZE, (y_center - y) * TILE_SIZE, x * TILE_SIZE, y * TILE_SIZE);
                var tiles = GetOutline(area).OrderBy(i => Guid.NewGuid()).ToList();

                foreach (var tile in tiles)
                {
                    var oldBrightness = GetAverageBrightnessUnsafe(img, tile);
                    Mouse.SmoothMoveTo(tile.Center);
                    var newBrightness = GetAverageBrightnessUnsafe(img, tile);

                    if (oldBrightness < (newBrightness - BOBBER_TOLERANCE))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static double GetAverageBrightnessUnsafe(Bitmap bmp, Rectanlge rect)
    {
        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

        // Get the address of the first line.
        IntPtr ptr = bmpData.Scan0;

        // Declare an array to hold the bytes of the bitmap.
        // This can be 32-bit (4 bytes) or 24-bit (3 bytes) depending on PixelFormat.
        int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
        byte[] rgbValues = new byte[bytes];

        // Copy the RGB values into the array.
        Marshal.Copy(ptr, rgbValues, 0, bytes);
        
        bmp.UnlockBits(bmpData);

        double totalBrightness = 0;
        // Assuming 24bpp or 32bpp, adjust the loop if necessary
        int pixelSize = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;

        Parallel.For(0, bmp.Height, y =>
        {
            int lineIndex = y * bmpData.Stride;
            for (int x = 0; x < bmp.Width; x++)
            {
                int i = lineIndex + x * pixelSize;
                // The data is typically stored as BGR
                byte b = rgbValues[i];
                byte g = rgbValues[i + 1];
                byte r = rgbValues[i + 2];

                // Calculate perceived brightness/luminance using a standard formula
                // The Color.GetBrightness() algorithm is different, but this is a common alternative.
                double brightness = (0.2126 * r + 0.7152 * g + 0.0722 * b);
                
                // Use Interlocked.Add for thread-safe accumulation if using Parallel.For
                Interlocked.Add(ref totalBrightness, brightness);
            }
        });

        return totalBrightness / (bmp.Width * bmp.Height * 255.0); // Normalize to 0-1 range
    }

    public static IEnumerable<Rectangle> GetOutline(Rectangle region)
    {
        var size = GetNumTiles(region);
        var y = 0;

        // top row
        for (var x = 0; x < size.Width; x++) {
            yield new Rectangle(x * TILE_SIZE, y* TILE_SIZE, TILE_SIZE, TILE_SIZE);
        }

        // sides
        for (y; y < size.Height - 1; y++) {
            yield new Rectangle(0, y* TILE_SIZE, TILE_SIZE, TILE_SIZE);
            yield new Rectangle((size.Width - 1) * TILE_SIZE, y, TILE_SIZE, TILE_SIZE);
        }

        // bottom row
        for (var x = 0; x < size.Width; x++) {
            yield new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE);
        }
    }

    public static Size GetNumTiles(Rectangle rect)
    {
        return new Size(rect.Width / TILE_SIZE, rect.Height / TILE_SIZE);
    }
}