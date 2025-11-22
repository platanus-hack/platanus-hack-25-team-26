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

        static PhishingApiClient()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public static async Task<PhishingResponse?> EvaluateScreenshotAsync(string screenshotPath)
        {
            if (!File.Exists(screenshotPath))
            {
                ApiLogger.LogResponse(ApiEndpoint, false, null, "Screenshot file does not exist");
                return null;
            }

            try
            {
                byte[] imageBytes = File.ReadAllBytes(screenshotPath);
                long fileSize = imageBytes.Length;

                // Log request
                ApiLogger.LogRequest(ApiEndpoint, screenshotPath, fileSize);

                // Create multipart form data content
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new ByteArrayContent(imageBytes), "file", Path.GetFileName(screenshotPath));

                    // Send POST request
                    HttpResponseMessage response = await _httpClient.PostAsync(ApiEndpoint, content);

                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        // Parse JSON response
                        try
                        {
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };
                            
                            PhishingResponse? phishingResponse = JsonSerializer.Deserialize<PhishingResponse>(responseBody, options);
                            
                            // Log successful response
                            ApiLogger.LogResponse(ApiEndpoint, true, responseBody);
                            
                            return phishingResponse;
                        }
                        catch (JsonException ex)
                        {
                            ApiLogger.LogResponse(ApiEndpoint, false, responseBody, $"JSON parsing error: {ex.Message}");
                            return null;
                        }
                    }
                    else
                    {
                        ApiLogger.LogResponse(ApiEndpoint, false, responseBody, $"HTTP {response.StatusCode}: {response.ReasonPhrase}");
                        return null;
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                ApiLogger.LogException(ApiEndpoint, ex);
                ApiLogger.LogResponse(ApiEndpoint, false, null, "Request timeout");
                return null;
            }
            catch (HttpRequestException ex)
            {
                ApiLogger.LogException(ApiEndpoint, ex);
                ApiLogger.LogResponse(ApiEndpoint, false, null, $"HTTP error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                ApiLogger.LogException(ApiEndpoint, ex);
                ApiLogger.LogResponse(ApiEndpoint, false, null, $"Unexpected error: {ex.Message}");
                return null;
            }
        }
    }
}

