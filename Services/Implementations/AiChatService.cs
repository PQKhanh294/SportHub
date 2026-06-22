using System.Text;
using System.Text.Json;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class AiChatService : IAiChatService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<AiChatService> _logger;

        public AiChatService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<AiChatService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<string> ChatAsync(string systemPrompt, List<AiChatHistoryItem> history, string userMessage, string? imageBase64 = null, string? imageMimeType = null)
        {
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return "Tính năng AI chưa được cấu hình. Vui lòng liên hệ admin.";

            var hasImage = !string.IsNullOrEmpty(imageBase64);
            // Switch to vision model when image is attached
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

            // Last user message — multimodal when image present
            if (hasImage)
            {
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = string.IsNullOrWhiteSpace(userMessage) ? "Hãy mô tả ảnh này và trả lời liên quan đến SportHub nếu có." : userMessage },
                        new { type = "image_url", image_url = new { url = $"data:{imageMimeType ?? "image/jpeg"};base64,{imageBase64}" } }
                    }
                });
            }
            else
            {
                messages.Add(new { role = "user", content = userMessage });
            }

            var requestBody = new
            {
                model,
                messages,
                temperature = 0.7,
                max_tokens = 512
            };

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
                    return "Xin lỗi, dịch vụ AI đang gặp sự cố. Vui lòng thử lại sau.";
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return text ?? "Xin lỗi, không có phản hồi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI API call failed.");
                return "Xin lỗi, không thể kết nối với dịch vụ AI.";
            }
        }
    }
}
