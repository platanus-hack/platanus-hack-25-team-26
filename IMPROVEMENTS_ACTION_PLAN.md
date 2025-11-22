# PhishingFinder v2 - Action Plan for Improvements

## Quick Wins (Can be fixed in < 1 hour each)

### 1. Fix Timer Interval Bug ⚡
**File**: `MainForm.cs:126`
**Current**: `screenshotTimer.Interval = 1000; // 5 seconds` (comment is wrong)
**Fix**: Change to `screenshotTimer.Interval = 5000;` or update comment to match
**Impact**: Reduces API calls by 80%, saves significant costs

### 2. Fix Random Instance Creation ⚡
**File**: `ScreenshotCapture.cs:279`
**Current**: `Random random = new Random();` (created each call)
**Fix**: Make it static: `private static readonly Random random = new Random();`
**Impact**: Better performance, thread-safety (though still not perfect)

### 3. Add Screenshot Cleanup After Processing ⚡
**File**: `MainForm.cs` after line 254
**Fix**: Add file deletion after successful API processing:
```csharp
// After line 254, add:
try
{
    if (File.Exists(filePath))
    {
        File.Delete(filePath);
        Console.WriteLine($"[Screenshot] Processed screenshot deleted: {Path.GetFileName(filePath)}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Screenshot] Warning: Failed to delete processed screenshot: {ex.Message}");
}
```
**Impact**: Prevents disk space accumulation

### 4. Extract Magic Numbers to Constants ⚡
**File**: `MainForm.cs`
**Fix**: Add constants at top of class:
```csharp
private const int SCREENSHOT_INTERVAL_MS = 5000;
private const int MOUSE_CHECK_INTERVAL_MS = 1000;
private const int CURSOR_FOLLOW_INTERVAL_MS = 50;
private const int MIN_THREAT_SCORE = 4;
private const int DANGER_THREAT_SCORE = 7;
```
**Impact**: Better maintainability

---

## High-Impact Performance Fixes (2-4 hours each)

### 5. Optimize Frame Comparison with LockBits 🔥
**File**: `MainForm.cs:316-359` - `CalculateFrameDifference()` method
**Current**: Uses `GetPixel()` which is extremely slow
**Fix**: Replace with `LockBits()` approach:
```csharp
private double CalculateFrameDifference(Bitmap previous, Bitmap current)
{
    if (previous.Width != current.Width || previous.Height != current.Height)
        return 1.0;
    
    int sampleStep = Math.Max(1, Math.Min(previous.Width, previous.Height) / 50);
    long totalDifference = 0;
    int sampleCount = 0;
    
    // Lock bits for fast pixel access
    var prevData = previous.LockBits(
        new Rectangle(0, 0, previous.Width, previous.Height),
        ImageLockMode.ReadOnly,
        previous.PixelFormat);
    var currData = current.LockBits(
        new Rectangle(0, 0, current.Width, current.Height),
        ImageLockMode.ReadOnly,
        current.PixelFormat);
    
    try
    {
        int bytesPerPixel = Image.GetPixelFormatSize(previous.PixelFormat) / 8;
        int prevStride = prevData.Stride;
        int currStride = currData.Stride;
        
        unsafe
        {
            byte* prevPtr = (byte*)prevData.Scan0;
            byte* currPtr = (byte*)currData.Scan0;
            
            for (int y = 0; y < previous.Height; y += sampleStep)
            {
                for (int x = 0; x < previous.Width; x += sampleStep)
                {
                    int prevIndex = y * prevStride + x * bytesPerPixel;
                    int currIndex = y * currStride + x * bytesPerPixel;
                    
                    long diffR = Math.Abs(prevPtr[prevIndex + 2] - currPtr[currIndex + 2]);
                    long diffG = Math.Abs(prevPtr[prevIndex + 1] - currPtr[currIndex + 1]);
                    long diffB = Math.Abs(prevPtr[prevIndex] - currPtr[currIndex]);
                    
                    totalDifference += diffR + diffG + diffB;
                    sampleCount++;
                }
            }
        }
    }
    finally
    {
        previous.UnlockBits(prevData);
        current.UnlockBits(currData);
    }
    
    if (sampleCount == 0)
        return 0.0;
    
    double averageDifference = (double)totalDifference / sampleCount;
    return averageDifference / 765.0;
}
```
**Note**: Requires `unsafe` keyword and project setting: `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`
**Impact**: 40x faster frame comparison (2 seconds → 50ms)

### 6. Add Rate Limiting/Throttling 🔥
**File**: `MainForm.cs`
**Fix**: Add minimum time between API calls:
```csharp
private DateTime lastApiCallTime = DateTime.MinValue;
private const int MIN_API_CALL_INTERVAL_MS = 5000; // 5 seconds minimum

// In ScreenshotTimer_Tick, before API call:
var timeSinceLastCall = DateTime.Now - lastApiCallTime;
if (timeSinceLastCall.TotalMilliseconds < MIN_API_CALL_INTERVAL_MS)
{
    Console.WriteLine($"[Screenshot] Rate limited - skipping (last call {timeSinceLastCall.TotalSeconds:F1}s ago)");
    return;
}

// Before calling API:
lastApiCallTime = DateTime.Now;
```
**Impact**: Prevents API spam, reduces costs

### 7. Fix HttpClient Disposal 🔥
**File**: `PhishingApiClient.cs`
**Fix**: Implement IDisposable or use IHttpClientFactory:
```csharp
// Option 1: Make HttpClient instance-based
public class PhishingApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    
    public PhishingApiClient()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }
    
    public void Dispose() => _httpClient?.Dispose();
}

// Option 2: Use static with proper disposal (current approach, but add cleanup)
// Add to Program.cs or MainForm cleanup:
// PhishingApiClient.Dispose(); // if you add static Dispose method
```
**Impact**: Prevents socket exhaustion

---

## Security Fixes (4-8 hours)

### 8. Encrypt Configuration File 🔒
**File**: `UserConfig.cs`
**Fix**: Use Windows DPAPI for encryption:
```csharp
using System.Security.Cryptography;
using System.Text;

public void Save()
{
    // ... existing directory creation code ...
    
    var options = new JsonSerializerOptions { WriteIndented = true };
    string json = JsonSerializer.Serialize(this, options);
    
    // Encrypt using DPAPI
    byte[] encrypted = ProtectedData.Protect(
        Encoding.UTF8.GetBytes(json),
        null,
        DataProtectionScope.CurrentUser);
    
    File.WriteAllBytes(ConfigFilePath + ".encrypted", encrypted);
    // Optionally delete old unencrypted file
}

public static UserConfig? Load()
{
    try
    {
        string encryptedPath = ConfigFilePath + ".encrypted";
        if (!File.Exists(encryptedPath))
            return null;
        
        byte[] encrypted = File.ReadAllBytes(encryptedPath);
        byte[] decrypted = ProtectedData.Unprotect(
            encrypted,
            null,
            DataProtectionScope.CurrentUser);
        
        string json = Encoding.UTF8.GetString(decrypted);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<UserConfig>(json, options);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading config: {ex.Message}");
        return null;
    }
}
```
**Requires**: `System.Security.Cryptography` namespace
**Impact**: Sensitive data protected

### 9. Compress Images Before Upload 💰
**File**: `PhishingApiClient.cs:40`
**Fix**: Compress PNG to JPEG before sending:
```csharp
using System.Drawing.Imaging;

// In EvaluateScreenshotAsync, before reading bytes:
byte[] imageBytes;
using (var originalImage = Image.FromFile(screenshotPath))
{
    using (var ms = new MemoryStream())
    {
        // Save as JPEG with 75% quality
        var jpegCodec = ImageCodecInfo.GetImageEncoders()
            .First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(
            Encoder.Quality, 75L);
        
        originalImage.Save(ms, jpegCodec, encoderParams);
        imageBytes = ms.ToArray();
    }
}

// Use imageBytes instead of File.ReadAllBytes
```
**Impact**: 50-70% smaller payloads, faster uploads

---

## Architecture Improvements (1-2 days)

### 10. Add Retry Logic with Exponential Backoff
**File**: `PhishingApiClient.cs`
**Fix**: Wrap API calls with retry logic:
```csharp
private static async Task<T?> RetryAsync<T>(
    Func<Task<T?>> operation,
    int maxRetries = 3,
    int baseDelayMs = 1000) where T : class
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (HttpRequestException) when (attempt < maxRetries - 1)
        {
            int delay = baseDelayMs * (int)Math.Pow(2, attempt);
            await Task.Delay(delay);
            Console.WriteLine($"[API] Retry attempt {attempt + 1}/{maxRetries} after {delay}ms");
        }
    }
    return null;
}
```

### 11. Add Configuration File Support
**File**: Create `appsettings.json`
```json
{
  "ScreenshotIntervalMs": 5000,
  "MouseCheckIntervalMs": 1000,
  "MinThreatScore": 4,
  "DangerThreatScore": 7,
  "ApiEndpoints": {
    "Evaluate": "https://pjemsvms4u.us-east-2.awsapprunner.com/evaluate",
    "Alert": "https://pjemsvms4u.us-east-2.awsapprunner.com/send-alert-email",
    "WhatsApp": "https://api-notmeta.damascuss.io/notmeta/kora/notify/"
  },
  "ImageCompression": {
    "Enabled": true,
    "Quality": 75
  }
}
```

### 12. Implement Proper Logging
**File**: Add `Microsoft.Extensions.Logging` package
**Fix**: Replace all `Console.WriteLine` with proper logging:
```csharp
private readonly ILogger<MainForm> _logger;

// In methods:
_logger.LogInformation("Screenshot captured: {FilePath}", filePath);
_logger.LogWarning("API call failed: {Error}", ex.Message);
```

---

## Testing Recommendations

### Unit Tests Needed:
1. `CalculateFrameDifference()` - test with various image sizes
2. `WindowDetector.IsMouseOverBrowser()` - mock window handles
3. `UserConfig.Save/Load()` - test encryption/decryption
4. `ScreenshotCapture.CaptureWindow()` - test with different window states

### Integration Tests:
1. End-to-end screenshot → API → alert flow
2. Rate limiting behavior
3. Error recovery scenarios

---

## Estimated Time & Impact

| Task | Time | Cost Savings | Performance Gain |
|------|------|--------------|------------------|
| Fix timer interval | 5 min | 80% API cost | - |
| Optimize frame comparison | 2-3 hrs | - | 40x faster |
| Add rate limiting | 1 hr | 20-30% | - |
| Compress images | 1-2 hrs | 50-70% bandwidth | Faster uploads |
| Encrypt config | 2-3 hrs | - | Security |
| Add retry logic | 2-3 hrs | - | Reliability |
| **Total Quick Wins** | **~1 hour** | **80% cost** | **-**
| **Total High-Impact** | **~1 day** | **85-90% cost** | **40x faster** |
| **Total Complete** | **~1 week** | **85-90% cost** | **40x faster + reliability** |

---

## Priority Order

1. ✅ Fix timer interval (5 min) - **CRITICAL BUG**
2. ✅ Add screenshot cleanup (15 min) - **DISK SPACE**
3. ✅ Optimize frame comparison (2-3 hrs) - **PERFORMANCE**
4. ✅ Add rate limiting (1 hr) - **COST**
5. ✅ Compress images (1-2 hrs) - **COST**
6. ✅ Encrypt config (2-3 hrs) - **SECURITY**
7. ✅ Fix HttpClient (1 hr) - **RELIABILITY**
8. ✅ Add retry logic (2-3 hrs) - **RELIABILITY**

---

*This action plan addresses the most critical issues first, prioritizing cost savings and performance improvements.*

