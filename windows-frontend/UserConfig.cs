using System;
using System.IO;
using System.Text.Json;

namespace PhishingFinder_v2
{
    public class UserConfig
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhishingFinder",
            "config.json"
        );

        /// <summary>
        /// Checks if the configuration file exists
        /// </summary>
        public static bool ConfigExists()
        {
            return File.Exists(ConfigFilePath);
        }

        /// <summary>
        /// Loads the user configuration from the config file
        /// </summary>
        public static UserConfig? Load()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return null;

                string json = File.ReadAllText(ConfigFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<UserConfig>(json, options);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - return null to trigger setup
                Console.WriteLine($"Error loading config: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves the user configuration to the config file
        /// </summary>
        public void Save()
        {
            try
            {
                // Create directory if it doesn't exist
                string? directory = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving config: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates if the configuration has all required fields
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(PhoneNumber);
        }
    }
}
