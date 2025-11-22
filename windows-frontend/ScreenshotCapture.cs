using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PhishingFinder_v2
{
    public static class ScreenshotCapture
    {
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        private const int SW_RESTORE = 9;
        private const int SW_MINIMIZE = 6;

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int PW_CLIENTONLY = 0x1;
        private const int PW_RENDERFULLCONTENT = 0x2;
        private const int SRCCOPY = 0x00CC0020;
        private const uint GA_ROOT = 2;

        public static bool CaptureWindow(IntPtr hWnd, string filePath)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                return false;

            bool wasMinimized = false;
            try
            {
                // Get the top-level parent window (important for browsers with child windows)
                IntPtr topLevelWindow = GetAncestor(hWnd, GA_ROOT);
                if (topLevelWindow != IntPtr.Zero)
                    hWnd = topLevelWindow;

                // Check if window is minimized and restore it temporarily if needed
                wasMinimized = IsIconic(hWnd);
                if (wasMinimized)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    System.Threading.Thread.Sleep(100); // Give window time to restore
                }

                // Try to get extended frame bounds (includes window borders) using DWM
                RECT rect = new RECT();
                bool gotExtendedBounds = false;
                try
                {
                    int result = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
                    if (result == 0) // S_OK
                    {
                        gotExtendedBounds = true;
                    }
                }
                catch
                {
                    // DWM API might not be available, fall back to GetWindowRect
                }

                // Fallback to GetWindowRect if DWM method failed
                if (!gotExtendedBounds)
                {
                    if (!GetWindowRect(hWnd, out rect))
                        return false;
                }

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (width <= 0 || height <= 0)
                    return false;

                // Ensure window is visible and on screen
                BringWindowToTop(hWnd);
                System.Threading.Thread.Sleep(50); // Brief pause to ensure window is ready

                // Method 1: Screen capture approach (capture the window's screen region)
                // This is the most reliable for capturing the full window including borders
                using (Bitmap bitmap = new Bitmap(width, height))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        // Capture from screen at the window's position
                        // Use the exact coordinates from the rectangle
                        graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                        
                        // Check if the bitmap is not all black
                        if (!IsBitmapBlack(bitmap))
                        {
                            // Ensure directory exists
                            string? directory = Path.GetDirectoryName(filePath);
                            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }

                            // Save bitmap
                            bitmap.Save(filePath, ImageFormat.Png);
                            return true;
                        }
                    }
                }

                // Method 2: Try PrintWindow with PW_RENDERFULLCONTENT (fallback for screen capture)
                using (Bitmap bitmap = new Bitmap(width, height))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        IntPtr hdc = graphics.GetHdc();
                        
                        // Try PrintWindow with PW_RENDERFULLCONTENT first (works with hardware acceleration)
                        bool success = PrintWindow(hWnd, hdc, PW_RENDERFULLCONTENT);
                        
                        if (!success)
                        {
                            // Try with PW_CLIENTONLY
                            success = PrintWindow(hWnd, hdc, PW_CLIENTONLY);
                        }
                        
                        if (!success)
                        {
                            // Try without flags
                            success = PrintWindow(hWnd, hdc, 0);
                        }
                        
                        graphics.ReleaseHdc(hdc);

                        if (success)
                        {
                            // Check if the bitmap is not all black (simple validation)
                            if (!IsBitmapBlack(bitmap))
                            {
                                // Ensure directory exists
                                string? directory = Path.GetDirectoryName(filePath);
                                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }

                                // Save bitmap
                                bitmap.Save(filePath, ImageFormat.Png);
                                return true;
                            }
                        }
                    }
                }

                // Method 3: Fallback to BitBlt with screen DC
                IntPtr hdcScreen = GetDC(IntPtr.Zero);
                if (hdcScreen != IntPtr.Zero)
                {
                    try
                    {
                        IntPtr hdcDest = CreateCompatibleDC(hdcScreen);
                        if (hdcDest != IntPtr.Zero)
                        {
                            try
                            {
                                IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
                                if (hBitmap != IntPtr.Zero)
                                {
                                    try
                                    {
                                        IntPtr hOld = SelectObject(hdcDest, hBitmap);
                                        BitBlt(hdcDest, 0, 0, width, height, hdcScreen, rect.Left, rect.Top, SRCCOPY);
                                        SelectObject(hdcDest, hOld);

                                        // Convert to Bitmap and save
                                        using (Bitmap bitmap = Image.FromHbitmap(hBitmap))
                                        {
                                            if (!IsBitmapBlack(bitmap))
                                            {
                                                // Ensure directory exists
                                                string? directory = Path.GetDirectoryName(filePath);
                                                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                                                {
                                                    Directory.CreateDirectory(directory);
                                                }

                                                bitmap.Save(filePath, ImageFormat.Png);
                                                return true;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        DeleteObject(hBitmap);
                                    }
                                }
                            }
                            finally
                            {
                                DeleteDC(hdcDest);
                            }
                        }
                    }
                    finally
                    {
                        ReleaseDC(IntPtr.Zero, hdcScreen);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                // Restore minimized state if window was originally minimized
                if (wasMinimized && hWnd != IntPtr.Zero)
                {
                    try
                    {
                        ShowWindow(hWnd, SW_MINIMIZE);
                    }
                    catch
                    {
                        // Best effort - ignore if we can't minimize it back
                    }
                }
            }

            return false;
        }

        private static readonly Random random = new Random();

        private static bool IsBitmapBlack(Bitmap bitmap)
        {
            // Sample a few pixels to check if the image is all black
            // This is a quick check - if all sampled pixels are black, likely the whole image is black
            int sampleCount = Math.Min(100, bitmap.Width * bitmap.Height);
            int blackPixels = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                int x = random.Next(bitmap.Width);
                int y = random.Next(bitmap.Height);
                Color pixel = bitmap.GetPixel(x, y);
                
                // Check if pixel is black or very dark (RGB all < 10)
                if (pixel.R < 10 && pixel.G < 10 && pixel.B < 10)
                {
                    blackPixels++;
                }
            }
            
            // If more than 95% of sampled pixels are black, consider it a black image
            return blackPixels > (sampleCount * 0.95);
        }

        public static string GetScreenshotsFolder()
        {
            string appFolder = Application.StartupPath;
            string screenshotsFolder = Path.Combine(appFolder, "Screenshots");
            
            if (!Directory.Exists(screenshotsFolder))
            {
                Directory.CreateDirectory(screenshotsFolder);
            }

            return screenshotsFolder;
        }

        public static string GenerateScreenshotFileName()
        {
            string folder = GetScreenshotsFolder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"browser_{timestamp}.png";
            return Path.Combine(folder, fileName);
        }
    }
}

