using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Services
{
    // Gửi email tổng hợp lúc 7h sáng (giờ VN): tin nhắn + thông báo chưa đọc từ hôm qua.
    public class DailyDigestEmailService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DailyDigestEmailService> _logger;

        private static readonly TimeZoneInfo _vnTz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

        public DailyDigestEmailService(IServiceProvider services, ILogger<DailyDigestEmailService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTz);

                var next7am = nowVn.Date.AddHours(7);
                if (nowVn.Hour >= 7) next7am = next7am.AddDays(1);

                var delay = next7am - nowVn;
                _logger.LogInformation("DailyDigestEmailService: next run at {NextRun} VN time ({Delay:hh\\:mm} from now)", next7am, delay);

                await Task.Delay(delay, stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;

                await RunDigestAsync(stoppingToken);
            }
        }

        private async Task RunDigestAsync(CancellationToken ct)
        {
            _logger.LogInformation("DailyDigestEmailService: building daily digest");
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var aiChatService = scope.ServiceProvider.GetRequiredService<IAiChatService>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = (config["App:BaseUrl"] ?? "https://sporthub-dn.id.vn/").TrimEnd('/');

                var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTz);
                var yesterdayStartUtc = TimeZoneInfo.ConvertTimeToUtc(nowVn.Date.AddDays(-1), _vnTz);
                var yesterdayEndUtc = TimeZoneInfo.ConvertTimeToUtc(nowVn.Date, _vnTz);

                var usersToNotify = await db.Users
                    .Where(u => u.NotifyByEmail && u.NotifyDailyDigest && !string.IsNullOrEmpty(u.Email))
                    .Select(u => new { u.UserID, u.Email, u.FullName })
                    .ToListAsync(ct);
                if (usersToNotify.Count == 0) return;

                var chatRows = await db.ChatMessages
                    .Where(m => !m.IsRead && !m.IsDeleted && m.CreatedAt >= yesterdayStartUtc && m.CreatedAt < yesterdayEndUtc)
                    .Select(m => new { m.ReceiverID, SenderName = m.Sender.FullName })
                    .ToListAsync(ct);

                var chatByReceiver = chatRows
                    .GroupBy(m => m.ReceiverID)
                    .ToDictionary(g => g.Key, g => g
                        .GroupBy(m => m.SenderName)
                        .Select(sg => (SenderName: sg.Key, UnreadCount: sg.Count()))
                        .ToList());

                var notifRows = await db.Notifications
                    .Where(n => !n.IsRead && n.CreatedAt >= yesterdayStartUtc && n.CreatedAt < yesterdayEndUtc)
                    .Select(n => new { n.UserID, n.Title, n.Message })
                    .ToListAsync(ct);

                var notifByUser = notifRows
                    .GroupBy(n => n.UserID)
                    .ToDictionary(g => g.Key, g => g.Select(n => (n.Title, n.Message)).ToList());

                var sentCount = 0;
                foreach (var user in usersToNotify)
                {
                    var chats = chatByReceiver.TryGetValue(user.UserID, out var c) ? c : new List<(string SenderName, int UnreadCount)>();
                    var notifs = notifByUser.TryGetValue(user.UserID, out var n) ? n : new List<(string Title, string Message)>();
                    if (chats.Count == 0 && notifs.Count == 0) continue;

                    var aiSummary = await TryBuildAiSummaryAsync(aiChatService, user.FullName, chats, notifs);
                    await emailService.SendDailyDigestAsync(user.Email, user.FullName, chats, notifs, aiSummary, $"{baseUrl}/Notifications");
                    sentCount++;
                }

                _logger.LogInformation("DailyDigestEmailService: sent {Count} digest email(s).", sentCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DailyDigestEmailService: error while building daily digest");
            }
        }

        // AI viết 1-2 câu tóm tắt thân thiện — không bao giờ được chặn việc gửi email nếu lỗi/timeout
        private async Task<string?> TryBuildAiSummaryAsync(IAiChatService aiChatService, string fullName,
            List<(string SenderName, int UnreadCount)> chats, List<(string Title, string Message)> notifs)
        {
            try
            {
                var chatDesc = chats.Count > 0 ? string.Join(", ", chats.Select(c => $"{c.UnreadCount} tin từ {c.SenderName}")) : "không có";
                var notifDesc = notifs.Count > 0 ? string.Join(", ", notifs.Select(n => n.Title)) : "không có";
                var userMessage = $"Tin nhắn chưa đọc: {chatDesc}. Thông báo chưa xem: {notifDesc}.";

                var systemPrompt = "Bạn là trợ lý viết email cho SportHub. Viết đúng 1 câu tiếng Việt thân thiện, ngắn gọn (dưới 30 từ), " +
                    "tóm tắt các hoạt động chưa xem của người dùng và khuyến khích họ mở app kiểm tra. Không dùng markdown, không xuống dòng, " +
                    "không lặp lại toàn bộ danh sách chi tiết — chỉ tóm tắt tự nhiên.";

                var result = await aiChatService.ChatAsync(systemPrompt, new List<AiChatHistoryItem>(), userMessage);
                return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
            }
            catch (Exception)
            {
                return null; // fallback: email vẫn gửi bình thường, chỉ thiếu câu tóm tắt AI
            }
        }
    }
}
