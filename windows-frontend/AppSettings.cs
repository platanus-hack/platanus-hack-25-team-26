using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhishingFinder_v2
{
    public class AppSettings
    {
        private static AppSettings? _instance;
        private static readonly object _lock = new object();

        public int ScreenshotIntervalMs { get; set; } = 5000;
        public int MouseCheckIntervalMs { get; set; } = 1000;
        public int CursorFollowIntervalMs { get; set; } = 50;
        public int MinThreatScore { get; set; } = 4;
        public int DangerThreatScore { get; set; } = 7;
        public double FrameDifferenceThreshold { get; set; } = 0.05;
        public int MinApiCallIntervalMs { get; set; } = 5000;

        public ApiRetrySettings ApiRetrySettings { get; set; } = new ApiRetrySettings();
        public ImageCompressionSettings ImageCompression { get; set; } = new ImageCompressionSettings();
        public ApiEndpointsSettings ApiEndpoints { get; set; } = new ApiEndpointsSettings();
        public TimeoutSettings Timeouts { get; set; } = new TimeoutSettings();

        /// <summary>
        /// Gets the singleton instance of AppSettings
        /// </summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = Load();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Loads settings from appsettings.json file
        /// </summary>
        private static AppSettings Load()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };

                    // Parse the JSON and extract the AppSettings section
                    using (JsonDocument document = JsonDocument.Parse(json))
                    {
                        if (document.RootElement.TryGetProperty("AppSettings", out JsonElement appSettingsElement))
                        {
                            string appSettingsJson = appSettingsElement.GetRawText();
                            var settings = JsonSerializer.Deserialize<AppSettings>(appSettingsJson, options);
                            if (settings != null)
                            {
                                Console.WriteLine("[AppSettings] Configuration loaded from appsettings.json");
                                return settings;
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[AppSettings] Configuration file not found at: {configPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppSettings] Error loading configuration: {ex.Message}");
            }

            Console.WriteLine("[AppSettings] Using default configuration");
            return new AppSettings();
        }

        /// <summary>
        /// Reloads the settings from file
        /// </summary>
        public static void Reload()
        {
            lock (_lock)
            {
                _instance = Load();
            }
        }
    }

    public class ApiRetrySettings
    {
        public int MaxRetries { get; set; } = 3;
        public int BaseDelayMs { get; set; } = 1000;
    }

    public class ImageCompressionSettings
    {
        public bool Enabled { get; set; } = true;
        public int Quality { get; set; } = 75;
        public string Format { get; set; } = "JPEG";
    }

    public class ApiEndpointsSettings
    {
        public string Evaluate { get; set; } = "https://pjemsvms4u.us-east-2.awsapprunner.com/evaluate";
        public string Alert { get; set; } = "https://pjemsvms4u.us-east-2.awsapprunner.com/send-alert-email";
        public string WhatsApp { get; set; } = "https://pjemsvms4u.us-east-2.awsapprunner.com/send-whatsapp-notification";
    }

    public class TimeoutSettings
    {
        public int HttpClientTimeoutSeconds { get; set; } = 30;
        public int WindowRestoreDelayMs { get; set; } = 100;
    }
}