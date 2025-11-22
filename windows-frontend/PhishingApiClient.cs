using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhishingFinder_v2
{
    public class PhishingApiClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiEndpoint = "https://pjemsvms4u.us-east-2.awsapprunner.com/evaluate";
        private const string AlertEndpoint = "http://localhost:8000/send-alert-email";
        private const string WhatsAppEndpoint = "https://api-notmeta.damascuss.io/notmeta/kora/notify/";

        static PhishingApiClient()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
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
                byte[] imageBytes = File.ReadAllBytes(screenshotPath);
                long fileSize = imageBytes.Length;
                Console.WriteLine($"[API-CLIENT] ✓ Archivo leído: {fileSize / 1024.0:F2} KB");

                // Log request
                ApiLogger.LogRequest(ApiEndpoint, screenshotPath, fileSize);

                Console.WriteLine($"[API-CLIENT] Creando request multipart...");
                // Create multipart form data content
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new ByteArrayContent(imageBytes), "file", Path.GetFileName(screenshotPath));
                    Console.WriteLine($"[API-CLIENT] ✓ Multipart form creado");

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

                            Console.WriteLine($"[API-CLIENT] EvaluateScreenshotAsync COMPLETADO ✓");
                            Console.WriteLine($"[API-CLIENT] ═══════════════════════════════════════");
                            return phishingResponse;
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"[API-CLIENT] ✗ Error parseando JSON: {ex.Message}");
                            ApiLogger.LogResponse(ApiEndpoint, false, responseBody, $"JSON parsing error: {ex.Message}");
                            Console.WriteLine($"[API-CLIENT] ═══════════════════════════════════════");
                            return null;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[API-CLIENT] ✗ HTTP Error: {response.StatusCode} - {response.ReasonPhrase}");
                        ApiLogger.LogResponse(ApiEndpoint, false, responseBody, $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
                        Console.WriteLine($"[API-CLIENT] ═══════════════════════════════════════");
                        return null;
                    }
                }
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
        /// <param name="refreshToken">Gmail OAuth2 refresh token</param>
        /// <param name="scoring">Risk scoring value</param>
        /// <param name="reason">Reason for the alert</param>
        /// <param name="type">Type of social engineering attack</param>
        /// <returns>True if alert was sent successfully, false otherwise</returns>
        public static async Task<bool> SendPhishingAlertAsync(string refreshToken, double scoring, string reason, string type)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                ApiLogger.LogResponse(AlertEndpoint, false, null, "Refresh token is empty");
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
                    refresh_token = refreshToken
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

