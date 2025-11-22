using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PhishingFinder_v2
{
    public class UserConfig
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhishingFinder"
        );

        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");
        private static readonly string EncryptedConfigFilePath = Path.Combine(ConfigDirectory, "config.encrypted");

        /// <summary>
        /// Checks if the configuration file exists (either encrypted or plain)
        /// </summary>
        public static bool ConfigExists()
        {
            return File.Exists(EncryptedConfigFilePath) || File.Exists(ConfigFilePath);
        }

        /// <summary>
        /// Loads the user configuration from the config file (supports both encrypted and plain)
        /// </summary>
        public static UserConfig? Load()
        {
            try
            {
                string json;

                // First, try to load encrypted config
                if (File.Exists(EncryptedConfigFilePath))
                {
                    try
                    {
                        byte[] encrypted = File.ReadAllBytes(EncryptedConfigFilePath);
                        byte[] decrypted = ProtectedData.Unprotect(
                            encrypted,
                            null,
                            DataProtectionScope.CurrentUser);

                        json = Encoding.UTF8.GetString(decrypted);
                        Console.WriteLine("[Config] Loaded encrypted configuration");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Config] Failed to decrypt config: {ex.Message}");
                        Console.WriteLine("[Config] Attempting to load plain config as fallback");

                        // If decryption fails, try plain config as fallback
                        if (File.Exists(ConfigFilePath))
                        {
                            json = File.ReadAllText(ConfigFilePath);
                            Console.WriteLine("[Config] Loaded plain configuration (migration needed)");

                            // Migrate to encrypted format
                            var tempConfig = JsonSerializer.Deserialize<UserConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (tempConfig != null)
                            {
                                tempConfig.Save(); // This will save encrypted
                                Console.WriteLine("[Config] Configuration migrated to encrypted format");
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                // If no encrypted config, try plain config
                else if (File.Exists(ConfigFilePath))
                {
                    json = File.ReadAllText(ConfigFilePath);
                    Console.WriteLine("[Config] Loaded plain configuration (migration needed)");

                    // Migrate to encrypted format immediately
                    var tempConfig = JsonSerializer.Deserialize<UserConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (tempConfig != null)
                    {
                        tempConfig.Save(); // This will save encrypted
                        Console.WriteLine("[Config] Configuration migrated to encrypted format");
                        return tempConfig;
                    }
                }
                else
                {
                    return null;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<UserConfig>(json, options);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - return null to trigger setup
                Console.WriteLine($"[Config] Error loading config: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves the user configuration to the config file (encrypted)
        /// </summary>
        public void Save()
        {
            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(this, options);

                // Encrypt using DPAPI
                byte[] encrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(json),
                    null,
                    DataProtectionScope.CurrentUser);

                // Save encrypted config
                File.WriteAllBytes(EncryptedConfigFilePath, encrypted);
                Console.WriteLine($"[Config] Configuration saved (encrypted) to: {EncryptedConfigFilePath}");

                // Delete old plain config file if it exists
                if (File.Exists(ConfigFilePath))
                {
                    try
                    {
                        File.Delete(ConfigFilePath);
                        Console.WriteLine("[Config] Old plain configuration file deleted");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Config] Warning: Could not delete old plain config: {ex.Message}");
                    }
                }
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
