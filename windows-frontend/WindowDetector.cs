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
            "iexplore"          // Internet Explorer
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

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;
        }

        public static bool IsMouseOverBrowser()
        {
            var mousePos = System.Windows.Forms.Control.MousePosition;
            var pt = new Point { X = mousePos.X, Y = mousePos.Y };
            
            IntPtr hWnd = WindowFromPoint(pt);
            
            if (hWnd == IntPtr.Zero)
                return false;

            // Check if window is visible
            if (!IsWindowVisible(hWnd))
                return false;

            // Get process ID from window handle
            GetWindowThreadProcessId(hWnd, out uint processId);
            
            if (processId == 0)
                return false;

            // Get process name
            string? processName = GetProcessName(processId);
            
            if (string.IsNullOrEmpty(processName))
                return false;

            // Check if it's a browser
            return IsBrowserProcess(processName);
        }

        public static IntPtr GetBrowserWindowHandle()
        {
            var mousePos = System.Windows.Forms.Control.MousePosition;
            var pt = new Point { X = mousePos.X, Y = mousePos.Y };
            
            IntPtr hWnd = WindowFromPoint(pt);
            
            if (hWnd == IntPtr.Zero)
                return IntPtr.Zero;

            // Check if window is visible
            if (!IsWindowVisible(hWnd))
                return IntPtr.Zero;

            // Get process ID from window handle
            GetWindowThreadProcessId(hWnd, out uint processId);
            
            if (processId == 0)
                return IntPtr.Zero;

            // Get process name
            string? processName = GetProcessName(processId);
            
            if (string.IsNullOrEmpty(processName))
                return IntPtr.Zero;

            // Check if it's a browser
            if (IsBrowserProcess(processName))
                return hWnd;

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

