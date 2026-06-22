using System.Text.Json;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class ChatModerationService
    {
        private readonly IAiChatService _aiService;
        private readonly ILogger<ChatModerationService> _logger;

        public ChatModerationService(IAiChatService aiService, ILogger<ChatModerationService> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<(int Score, string Recommendation, string Analysis)> AnalyzeMessageAsync(string content)
        {
            const string systemPrompt =
                "Bạn là hệ thống kiểm duyệt nội dung cho mạng xã hội thể thao Việt Nam. " +
                "Phân tích tin nhắn sau có vi phạm không: phân biệt vùng miền, chủng tộc, " +
                "chống phá nhà nước, quấy rối tình dục, đe dọa bạo lực, ngôn từ thô tục nặng nề, " +
                "nội dung không phù hợp với chuẩn mực đạo đức. " +
                "Chỉ trả về JSON thuần (không có markdown, không có ```): " +
                "{\"score\":0-10,\"recommendation\":\"dismiss|warn|ban_1day|ban_7days|ban_30days|ban_permanent\",\"analysis\":\"lý do ngắn gọn bằng tiếng Việt\"}. " +
                "Điểm 0=hoàn toàn ổn, 10=vi phạm nghiêm trọng nhất. " +
                "Chỉ đề xuất ban khi score >= 6. dismiss khi score <= 3.";

            try
            {
                var reply = await _aiService.ChatAsync(systemPrompt, new(), content);
                // Strip markdown code fences if present
                reply = reply.Trim();
                if (reply.StartsWith("```")) reply = reply.Split('\n', 2)[1];
                if (reply.EndsWith("```")) reply = reply[..reply.LastIndexOf("```")].TrimEnd();

                using var doc = JsonDocument.Parse(reply);
                var root = doc.RootElement;
                int score = root.TryGetProperty("score", out var s) ? s.GetInt32() : 0;
                string rec = root.TryGetProperty("recommendation", out var r) ? r.GetString() ?? "dismiss" : "dismiss";
                string analysis = root.TryGetProperty("analysis", out var a) ? a.GetString() ?? "" : "";
                return (Math.Clamp(score, 0, 10), rec, analysis);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI moderation failed for message content");
                return (0, "dismiss", "Không thể phân tích tự động.");
            }
        }
    }
}
