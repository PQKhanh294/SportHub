using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class AiChatService : IAiChatService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<AiChatService> _logger;
        private readonly IMemoryCache _cache;

        // Simple circuit breaker state (singleton via static)
        private static int _consecutiveFailures = 0;
        private static DateTime _circuitOpenUntil = DateTime.MinValue;
        private const int FailureThreshold = 5;
        private static readonly TimeSpan CircuitOpenDuration = TimeSpan.FromMinutes(3);

        public AiChatService(IHttpClientFactory httpClientFactory, IConfiguration config,
            ILogger<AiChatService> logger, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
            _cache = cache;
        }

        public async Task<string> ChatAsync(string systemPrompt, List<AiChatHistoryItem> history,
            string userMessage, string? imageBase64 = null, string? imageMimeType = null)
        {
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return "Tính năng AI chưa được cấu hình. Vui lòng liên hệ admin.";

            // Circuit breaker check
            if (DateTime.UtcNow < _circuitOpenUntil)
            {
                _logger.LogWarning("AI circuit breaker OPEN — skipping Groq call until {Until}", _circuitOpenUntil);
                return "Dịch vụ AI tạm thời không khả dụng. Vui lòng thử lại sau vài phút.";
            }

            var hasImage = !string.IsNullOrEmpty(imageBase64);

            // Cache lookup: only for text-only first messages (no history) — history changes each turn making keys unique
            string? cacheKey = null;
            if (!hasImage && history.Count == 0)
            {
                cacheKey = $"ai:{systemPrompt.GetHashCode()}:{userMessage.GetHashCode()}";
                if (_cache.TryGetValue(cacheKey, out string? cached))
                {
                    _logger.LogDebug("AI cache hit for key {Key}", cacheKey);
                    return cached!;
                }
            }

            var model = hasImage
                ? "meta-llama/llama-4-scout-17b-16e-instruct"
                : (_config["Gemini:Model"] ?? "llama-3.1-8b-instant");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            foreach (var item in history)
                messages.Add(new { role = item.Role == "assistant" ? "assistant" : "user", content = item.Content });

            if (hasImage)
            {
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = string.IsNullOrWhiteSpace(userMessage)
                            ? "Hãy mô tả ảnh này và trả lời liên quan đến SportHub nếu có." : userMessage },
                        new { type = "image_url", image_url = new { url = $"data:{imageMimeType ?? "image/jpeg"};base64,{imageBase64}" } }
                    }
                });
            }
            else
            {
                messages.Add(new { role = "user", content = userMessage });
            }

            var requestBody = new { model, messages, temperature = 0.7, max_tokens = 512 };

            try
            {
                var json = JsonSerializer.Serialize(requestBody);
                var response = await client.PostAsync(
                    "https://api.groq.com/openai/v1/chat/completions",
                    new StringContent(json, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("AI API error: {Status} — {Body}", response.StatusCode, errorBody);
                    RecordFailure();
                    return "Xin lỗi, dịch vụ AI đang gặp sự cố. Vui lòng thử lại sau.";
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                var result = text ?? "Xin lỗi, không có phản hồi.";

                // Cache successful non-image responses for 30 minutes
                if (cacheKey != null)
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

                Interlocked.Exchange(ref _consecutiveFailures, 0);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI API call failed.");
                RecordFailure();
                return "Xin lỗi, không thể kết nối với dịch vụ AI.";
            }
        }

        private static void RecordFailure()
        {
            var failures = Interlocked.Increment(ref _consecutiveFailures);
            if (failures >= FailureThreshold)
            {
                _circuitOpenUntil = DateTime.UtcNow.Add(CircuitOpenDuration);
                Interlocked.Exchange(ref _consecutiveFailures, 0);
            }
        }
    }
}
