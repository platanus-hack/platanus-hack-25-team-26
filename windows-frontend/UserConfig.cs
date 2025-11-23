using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PhishingFinder_v2
{
    // Enum to define dialog display types
    public enum DialogDisplayType
    {
        NonBlockingCentered = 0,    // Non-blocking dialog at center of screen
        AlwaysOnTopBlocking = 1      // Always-on-top blocking dialog
    }

    public class UserConfig
    {
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DialogDisplayType DialogType { get; set; } = DialogDisplayType.NonBlockingCentered; // Legacy property for backward compatibility

        // Separate dialog types for different threat levels
        public DialogDisplayType WarningDialogType { get; set; } = DialogDisplayType.NonBlockingCentered; // For scoring 4-6
        public DialogDisplayType CriticalDialogType { get; set; } = DialogDisplayType.AlwaysOnTopBlocking; // For scoring 7-10

        // Parent control settings
        public bool ParentControlEnabled { get; set; } = true; // Whether parent control is enabled
        public bool SendEmailAlerts { get; set; } = true; // Whether to send email alerts when threats detected
        public bool SendPhoneAlerts { get; set; } = true; // Whether to send phone/WhatsApp alerts when threats detected

        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhishingFinder"
        );

        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");
        private static readonly string EncryptedConfigFilePath = Path.Combine(ConfigDirectory, "config.encrypted");

        /// <summary>
        /// Gets the appropriate dialog type based on the threat scoring
        /// </summary>
        public DialogDisplayType GetDialogTypeForScoring(double scoring)
        {
            if (scoring >= 7)
            {
                return CriticalDialogType;
            }
            else if (scoring >= 4)
            {
                return WarningDialogType;
            }
            else
            {
                // For low scores (1-3), use the warning dialog type
                return WarningDialogType;
            }
        }

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
                var config = JsonSerializer.Deserialize<UserConfig>(json, options);

                // Handle backward compatibility: if the new properties don't exist in the JSON,
                // use the legacy DialogType property to initialize them
                if (config != null)
                {
                    // If WarningDialogType and CriticalDialogType are not set (default values),
                    // initialize them based on the legacy DialogType property
                    if (json.IndexOf("WarningDialogType", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        config.WarningDialogType = config.DialogType;
                    }
                    if (json.IndexOf("CriticalDialogType", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        config.CriticalDialogType = config.DialogType;
                    }
                }

                return config;
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
            // If parent control is disabled, config is always valid
            if (!ParentControlEnabled)
            {
                return true;
            }

            // If parent control is enabled, at least one contact method is required
            return (!string.IsNullOrWhiteSpace(Email) && SendEmailAlerts) ||
                   (!string.IsNullOrWhiteSpace(PhoneNumber) && SendPhoneAlerts);
        }
    }
}
