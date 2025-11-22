using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhishingFinder_v2
{
    public class PhishingApiClient
    {
        // HttpClient should be reused across the application lifetime
        // Using a static instance is recommended for performance
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly AppSettings _settings = AppSettings.Instance;

        // Use endpoints from configuration
        private static string ApiEndpoint => _settings.ApiEndpoints.Evaluate;
        private static string AlertEndpoint => _settings.ApiEndpoints.Alert;
        private static string WhatsAppEndpoint => _settings.ApiEndpoints.WhatsApp;

        static PhishingApiClient()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.Timeouts.HttpClientTimeoutSeconds);
        }

        // Optional cleanup method if needed
        public static void Cleanup()
        {
            // HttpClient should typically not be disposed in long-running applications
            // But provide a method for cleanup during app shutdown if needed
            try
            {
                _httpClient?.Dispose();
            }
            catch { /* Ignore disposal errors */ }
        }

        /// <summary>
        /// Helper method to perform HTTP operations with retry logic
        /// </summary>
        private static async Task<T?> RetryAsync<T>(
            Func<Task<T?>> operation,
            string operationName,
            int? maxRetries = null,
            int? baseDelayMs = null) where T : class
        {
            int maxRetriesValue = maxRetries ?? _settings.ApiRetrySettings.MaxRetries;
            int baseDelayMsValue = baseDelayMs ?? _settings.ApiRetrySettings.BaseDelayMs;

            for (int attempt = 0; attempt < maxRetriesValue; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (HttpRequestException ex) when (attempt < maxRetriesValue - 1)
                {
                    int delay = baseDelayMsValue * (int)Math.Pow(2, attempt);
                    Console.WriteLine($"[API-CLIENT] Retry attempt {attempt + 1}/{maxRetriesValue} for {operationName} after {delay}ms - Error: {ex.Message}");
                    await Task.Delay(delay);
                }
                catch (TaskCanceledException) when (attempt < maxRetriesValue - 1)
                {
                    int delay = baseDelayMsValue * (int)Math.Pow(2, attempt);
                    Console.WriteLine($"[API-CLIENT] Timeout on attempt {attempt + 1}/{maxRetriesValue} for {operationName}, retrying after {delay}ms");
                    await Task.Delay(delay);
                }
            }
            return null;
        }

        public static async Task<PhishingResponse?> EvaluateScreenshotAsync(string screenshotPath)
        {
            Console.WriteLine($"[API-CLIENT] ═══════════════════════════════════════");
            Console.WriteLine($"[API-CLIENT] EvaluateScreenshotAsync INICIADO");
            Console.WriteLine($"[API-CLIENT] Hora: {DateTime.Now:HH:mm:ss.fff}");
            Console.WriteLine($"[API-CLIENT] Screenshot path: {screenshotPath}");

            if (!File.Exists(screenshotPath))
            {
                Console.WriteLine($"[API-CLIENT] ✗ ERROR: El archivo no existe");
                ApiLogger.LogResponse(ApiEndpoint, false, null, "Screenshot file does not exist");
                return null;
            }

            Console.WriteLine($"[API-CLIENT] ✓ Archivo existe");

            try
            {
                // Compress the image before sending
                byte[] imageBytes;
                long originalSize = new FileInfo(screenshotPath).Length;
                Console.WriteLine($"[API-CLIENT] Tamaño original: {originalSize / 1024.0:F2} KB");

                // Load image and optionally compress
                if (_settings.ImageCompression.Enabled)
                {
                    using (var originalImage = Image.FromFile(screenshotPath))
                    {
                        using (var ms = new MemoryStream())
                        {
                            // Save as JPEG with configured quality
                            var jpegCodec = ImageCodecInfo.GetImageEncoders()
                                .First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(
                                System.Drawing.Imaging.Encoder.Quality, (long)_settings.ImageCompression.Quality);

                            originalImage.Save(ms, jpegCodec, encoderParams);
                            imageBytes = ms.ToArray();
                        }
                    }
                }
                else
                {
                    // No compression - send original file
                    imageBytes = File.ReadAllBytes(screenshotPath);
                }

                long compressedSize = imageBytes.Length;
                double compressionRatio = (1.0 - (double)compressedSize / originalSize) * 100;
                Console.WriteLine($"[API-CLIENT] ✓ Imagen comprimida: {compressedSize / 1024.0:F2} KB (reducción: {compressionRatio:F1}%)");

                // Log request
                ApiLogger.LogRequest(ApiEndpoint, screenshotPath, compressedSize);

                // Use retry logic for the API call
                PhishingResponse? result = await RetryAsync(async () =>
                {
                    Console.WriteLine($"[API-CLIENT] Creando request multipart...");
                    using (var content = new MultipartFormDataContent())
                    {
                        // Change extension to .jpg since we compressed to JPEG
                        string compressedFileName = Path.GetFileNameWithoutExtension(screenshotPath) + ".jpg";
                        content.Add(new ByteArrayContent(imageBytes), "file", compressedFileName);
                        Console.WriteLine($"[API-CLIENT] ✓ Multipart form creado con archivo: {compressedFileName}");

                        // Send POST request
                        Console.WriteLine($"[API-CLIENT] Enviando POST a {ApiEndpoint}...");
                        HttpResponseMessage response = await _httpClient.PostAsync(ApiEndpoint, content);
                        Console.WriteLine($"[API-CLIENT] ✓ Respuesta recibida - Status: {response.StatusCode}");

                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[API-CLIENT] ✓ Body leído - Length: {responseBody.Length} chars");

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[API-CLIENT] ✓ HTTP Success - Parseando JSON...");
                            // Parse JSON response
                            try
                            {
                                var options = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                };

                                PhishingResponse? phishingResponse = JsonSerializer.Deserialize<PhishingResponse>(responseBody, options);
                                Console.WriteLine($"[API-CLIENT] ✓ JSON parseado correctamente");

                                // Log successful response
                                ApiLogger.LogResponse(ApiEndpoint, true, responseBody);
                                return phishingResponse;
                            }
                            catch (JsonException ex)
                            {
                                Console.WriteLine($"[API-CLIENT] ✗ Error parseando JSON: {ex.Message}");
                                ApiLogger.LogResponse(ApiEndpoint, false, responseBody, $"JSON parsing error: {ex.Message}");
                                return null;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[API-CLIENT] ✗ HTTP Error: {response.StatusCode} - {response.ReasonPhrase}");
                            ApiLogger.LogResponse(ApiEndpoint, false, responseBody, $"HTTP {response.StatusCode}: {response.ReasonPhrase}");

                            // Throw exception for retry if it's a temporary failure
                            if ((int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                            {
                                throw new HttpRequestException($"Server error: {response.StatusCode}");
                            }
                            return null;
                        }
                    }
                }, "EvaluateScreenshot");

                Console.WriteLine($"[API-CLIENT] EvaluateScreenshotAsync COMPLETADO");
                Console.WriteLine($"[API-CLIENT] ═══════════════════════════════════════");
                return result;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[API-CLIENT] ✗ TIMEOUT: Request cancelado después de {_httpClient.Timeout.TotalSeconds}s");
                ApiLogger.LogException(ApiEndpoint, ex);
                ApiLogger.LogResponse(ApiEndpoint, false, null, "Request timeout");
                Console.WriteLine($"[API-CLIENT] ═══════════════════════════════════════");
                return null;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[API-CLIENT] ✗ HTTP REQUEST ERROR: {ex.Message}");
                ApiLogger.LogException(ApiEndpoint, ex);
                ApiLogger.LogResponse(ApiEndpoint, false, null, $"HTTP error: {ex.Message}");
                Console.WriteLine($"[API-CLIENT] ═══════════════════════════════════════");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API-CLIENT] ✗ ERROR INESPERADO: {ex.Message}");
                Console.WriteLine($"[API-CLIENT] Stack trace: {ex.StackTrace}");
                ApiLogger.LogException(ApiEndpoint, ex);
                ApiLogger.LogResponse(ApiEndpoint, false, null, $"Unexpected error: {ex.Message}");
                Console.WriteLine($"[API-CLIENT] ═══════════════════════════════════════");
                return null;
            }
        }

        /// <summary>
        /// Sends a social engineering alert via email
        /// </summary>
        /// <param name="email">Email to send the alert to</param>
        /// <param name="scoring">Risk scoring value</param>
        /// <param name="reason">Reason for the alert</param>
        /// <param name="type">Type of social engineering attack</param>
        /// <returns>True if alert was sent successfully, false otherwise</returns>
        public static async Task<bool> SendPhishingAlertAsync(string email, double scoring, string reason, string type)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ApiLogger.LogResponse(AlertEndpoint, false, null, "Email is empty");
                return false;
            }

            try
            {
                // Create JSON payload
                var payload = new
                {
                    scoring = scoring,
                    reason = reason,
                    type = type,
                    recipient_email = email
                };

                string jsonPayload = JsonSerializer.Serialize(payload);

                // Log request
                ApiLogger.LogRequest(AlertEndpoint, jsonPayload, jsonPayload.Length);

                // Create JSON content
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Send POST request
                HttpResponseMessage response = await _httpClient.PostAsync(AlertEndpoint, content);

                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ApiLogger.LogResponse(AlertEndpoint, true, responseBody);
                    return true;
                }
                else
                {
                    ApiLogger.LogResponse(AlertEndpoint, false, responseBody, $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (TaskCanceledException ex)
            {
                ApiLogger.LogException(AlertEndpoint, ex);
                ApiLogger.LogResponse(AlertEndpoint, false, null, "Request timeout");
                return false;
            }
            catch (HttpRequestException ex)
            {
                ApiLogger.LogException(AlertEndpoint, ex);
                ApiLogger.LogResponse(AlertEndpoint, false, null, $"HTTP error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                ApiLogger.LogException(AlertEndpoint, ex);
                ApiLogger.LogResponse(AlertEndpoint, false, null, $"Unexpected error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends a WhatsApp notification alert
        /// </summary>
        /// <param name="phoneNumber">Phone number to send notification to (format: 573001234567)</param>
        /// <param name="reason">Reason for the alert</param>
        /// <returns>True if notification was sent successfully, false otherwise</returns>
        public static async Task<bool> SendWhatsAppNotificationAsync(string phoneNumber, string reason)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                ApiLogger.LogResponse(WhatsAppEndpoint, false, null, "Phone number is empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                ApiLogger.LogResponse(WhatsAppEndpoint, false, null, "Reason is empty");
                return false;
            }

            try
            {
                // Create JSON payload
                var payload = new
                {
                    to_number = phoneNumber,
                    reason = reason
                };

                string jsonPayload = JsonSerializer.Serialize(payload);

                // Log request
                ApiLogger.LogRequest(WhatsAppEndpoint, jsonPayload, jsonPayload.Length);

                // Create JSON content
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Send POST request
                HttpResponseMessage response = await _httpClient.PostAsync(WhatsAppEndpoint, content);

                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ApiLogger.LogResponse(WhatsAppEndpoint, true, responseBody);
                    return true;
                }
                else
                {
                    ApiLogger.LogResponse(WhatsAppEndpoint, false, responseBody, $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (TaskCanceledException ex)
            {
                ApiLogger.LogException(WhatsAppEndpoint, ex);
                ApiLogger.LogResponse(WhatsAppEndpoint, false, null, "Request timeout");
                return false;
            }
            catch (HttpRequestException ex)
            {
                ApiLogger.LogException(WhatsAppEndpoint, ex);
                ApiLogger.LogResponse(WhatsAppEndpoint, false, null, $"HTTP error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                ApiLogger.LogException(WhatsAppEndpoint, ex);
                ApiLogger.LogResponse(WhatsAppEndpoint, false, null, $"Unexpected error: {ex.Message}");
                return false;
            }
        }
    }
}

