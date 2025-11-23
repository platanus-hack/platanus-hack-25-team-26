using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PhishingFinder_v2
{
    public static class WindowDetector
    {
        // List of browser process names (without .exe)
        private static readonly HashSet<string> BrowserProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chrome",           // Google Chrome
            "msedge",           // Microsoft Edge
            "firefox",          // Mozilla Firefox
            "opera",            // Opera
            "opera_gx",         // Opera GX
            "brave",            // Brave Browser
            "vivaldi",          // Vivaldi
            "safari",           // Safari (if on Windows)
            "browser",          // Edge Legacy
            "iexplore",         // Internet Explorer
            "commet"            // Comet Browser
        };

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint GA_ROOT = 2;

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;
        }

        // Helper class to store enumeration data
        private class WindowEnumData
        {
            public Point MousePoint { get; set; }
            public List<IntPtr> BrowserWindows { get; set; } = new List<IntPtr>();
        }

        public static bool IsMouseOverBrowser()
        {
            // Use the new method to find browser windows at mouse position
            IntPtr browserHandle = GetBrowserWindowHandle();
            return browserHandle != IntPtr.Zero;
        }

        public static IntPtr GetBrowserWindowHandle()
        {
            var mousePos = System.Windows.Forms.Control.MousePosition;
            var pt = new Point { X = mousePos.X, Y = mousePos.Y };

            // First try the topmost window (for performance)
            IntPtr topWindow = WindowFromPoint(pt);
            if (topWindow != IntPtr.Zero && IsWindowVisible(topWindow))
            {
                GetWindowThreadProcessId(topWindow, out uint processId);
                string? processName = GetProcessName(processId);
                if (!string.IsNullOrEmpty(processName) && IsBrowserProcess(processName))
                {
                    // Get the root window (main browser window)
                    IntPtr rootWindow = GetAncestor(topWindow, GA_ROOT);
                    return rootWindow != IntPtr.Zero ? rootWindow : topWindow;
                }
            }

            // If topmost window is not a browser, enumerate all windows to find browsers underneath
            var enumData = new WindowEnumData { MousePoint = pt };

            EnumWindows((hWnd, lParam) =>
            {
                // Check if window is visible
                if (!IsWindowVisible(hWnd))
                    return true; // Continue enumeration

                // Get window rectangle
                if (!GetWindowRect(hWnd, out RECT rect))
                    return true; // Continue enumeration

                // Check if mouse point is within window bounds
                if (enumData.MousePoint.X >= rect.Left && enumData.MousePoint.X <= rect.Right &&
                    enumData.MousePoint.Y >= rect.Top && enumData.MousePoint.Y <= rect.Bottom)
                {
                    // Check if this is a browser window
                    GetWindowThreadProcessId(hWnd, out uint processId);
                    string? processName = GetProcessName(processId);

                    if (!string.IsNullOrEmpty(processName) && IsBrowserProcess(processName))
                    {
                        // Get the root window (main browser window)
                        IntPtr rootWindow = GetAncestor(hWnd, GA_ROOT);
                        enumData.BrowserWindows.Add(rootWindow != IntPtr.Zero ? rootWindow : hWnd);
                    }
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            // Return the first browser window found (if any)
            // This will be the topmost browser window in the z-order
            if (enumData.BrowserWindows.Count > 0)
            {
                Console.WriteLine($"[WindowDetector] Found {enumData.BrowserWindows.Count} browser window(s) at mouse position");
                return enumData.BrowserWindows[0];
            }

            return IntPtr.Zero;
        }

        private static string? GetProcessName(uint processId)
        {
            try
            {
                IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                if (hProcess == IntPtr.Zero)
                    return null;

                try
                {
                    uint size = 260;
                    StringBuilder sb = new StringBuilder((int)size);
                    if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                    {
                        string fullPath = sb.ToString();
                        // Extract just the filename
                        int lastBackslash = fullPath.LastIndexOf('\\');
                        if (lastBackslash >= 0 && lastBackslash < fullPath.Length - 1)
                        {
                            string fileName = fullPath.Substring(lastBackslash + 1);
                            // Remove .exe extension
                            if (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                return fileName.Substring(0, fileName.Length - 4);
                            }
                            return fileName;
                        }
                        return fullPath;
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }
            catch
            {
                // Fallback: try using Process class
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    return process.ProcessName;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static bool IsBrowserProcess(string processName)
        {
            return BrowserProcessNames.Contains(processName);
        }
    }
}

