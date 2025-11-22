using System;
using System.IO;
using System.Windows.Forms;

namespace PhishingFinder_v2
{
    public static class ApiLogger
    {
        private static readonly object _lock = new object();
        private static string GetLogsFolder()
        {
            string appFolder = Application.StartupPath;
            string logsFolder = Path.Combine(appFolder, "Logs");
            
            if (!Directory.Exists(logsFolder))
            {
                Directory.CreateDirectory(logsFolder);
            }

            return logsFolder;
        }

        private static string GetLogFilePath()
        {
            string folder = GetLogsFolder();
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(folder, $"api_log_{date}.txt");
        }

        public static void LogRequest(string endpoint, string filePath, long fileSize)
        {
            lock (_lock)
            {
                try
                {
                    string logFile = GetLogFilePath();
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] REQUEST - Endpoint: {endpoint}, File: {Path.GetFileName(filePath)}, Size: {fileSize} bytes{Environment.NewLine}";
                    
                    File.AppendAllText(logFile, logEntry);
                }
                catch
                {
                    // Silently fail if logging fails
                }
            }
        }

        public static void LogResponse(string endpoint, bool success, string? responseBody, string? errorMessage = null)
        {
            lock (_lock)
            {
                try
                {
                    string logFile = GetLogFilePath();
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string status = success ? "SUCCESS" : "FAILED";
                    string logEntry = $"[{timestamp}] RESPONSE - Endpoint: {endpoint}, Status: {status}";
                    
                    if (success && !string.IsNullOrEmpty(responseBody))
                    {
                        logEntry += $", Response: {responseBody}";
                    }
                    
                    if (!success && !string.IsNullOrEmpty(errorMessage))
                    {
                        logEntry += $", Error: {errorMessage}";
                    }
                    
                    logEntry += Environment.NewLine;
                    
                    File.AppendAllText(logFile, logEntry);
                }
                catch
                {
                    // Silently fail if logging fails
                }
            }
        }

        public static void LogException(string endpoint, Exception ex)
        {
            lock (_lock)
            {
                try
                {
                    string logFile = GetLogFilePath();
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] EXCEPTION - Endpoint: {endpoint}, Error: {ex.Message}, StackTrace: {ex.StackTrace}{Environment.NewLine}";
                    
                    File.AppendAllText(logFile, logEntry);
                }
                catch
                {
                    // Silently fail if logging fails
                }
            }
        }
    }
}

