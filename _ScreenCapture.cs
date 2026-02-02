using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class ScreenCapture2
{
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);
            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
public static extern bool BitBlt(IntPtr hObject,int nXDest,int nYDest,
                int nWidth,int nHeight,IntPtr hObjectSource,
                int nXSrc,int nYSrc,int dwRop);

    [DllImport("user32.dll")]
private static extern bool PrintWindow(
  IntPtr hwnd,
  IntPtr  hdcBlt,
  uint nFlags
);



// RECT structure for window bounds
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    // Import GetWindowRect from user32.dll
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public static void Capture()
    {
        string windowTitle = "World of Warcraft"; // Example window title
        IntPtr hwnd = FindWindow(null, windowTitle);

        if (hwnd == IntPtr.Zero)
        {
            Console.WriteLine("Window not found!");
            return;
        }

        RECT rect;
        GetWindowRect(hwnd, out rect);
        
        Console.WriteLine("Working Area {0}", rect);

        IntPtr hdcWindow = GetWindowDC(hwnd);
        //IntPtr hdcMemDC = CreateCompatibleDC(hdcWindow);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcWindow, rect.Right - rect.Left, rect.Bottom - rect.Top); // Replace with actual dimensions
        IntPtr hOldBitmap = SelectObject(hdcWindow, hBitmap);

PrintWindow(hdcWindow, hBitmap, 0);

        using (Bitmap bmp = Bitmap.FromHbitmap(hBitmap))
        {
            bmp.Save("screenshot0.png", ImageFormat.Bmp);
        }

BitBlt(hBitmap,0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top, hdcWindow, 0, 0, SRCCOPY);


        // Capture the screenshot
        using (Bitmap bmp = Bitmap.FromHbitmap(hBitmap))
        {
            bmp.Save("screenshot1.png", ImageFormat.Bmp);
        }

        // Clean up
        SelectObject(hdcWindow, hOldBitmap);
        ReleaseDC(hwnd, hdcWindow);
        DeleteObject(hBitmap);
        //DeleteObject(hdcMemDC);
    }
}