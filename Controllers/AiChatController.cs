using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;
using System.Security.Claims;

namespace SportHub.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [Authorize]
    public class AiChatController : ControllerBase
    {
        private readonly IAiChatService _aiChatService;
        private readonly ApplicationDbContext _context;
        private readonly IWalletService _walletService;
        private readonly ISubscriptionService _subscriptionService;

        public AiChatController(IAiChatService aiChatService, ApplicationDbContext context,
            IWalletService walletService, ISubscriptionService subscriptionService)
        {
            _aiChatService = aiChatService;
            _context = context;
            _walletService = walletService;
            _subscriptionService = subscriptionService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 500)
                return BadRequest(new { reply = "Tin nhắn không hợp lệ." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            // Subscription gating
            var activeSub = await _subscriptionService.GetActiveSubscriptionAsync(userId);
            var planKey = activeSub?.PlanKey ?? "Free";
            var canSeeMatches = planKey != "Free";
            var canAnalyzeImage = planKey is "Pro" or "Club";
            var canCoach = planKey is "Pro" or "Club";
            var historyLimit = planKey switch { "Starter" => 10, "Pro" => 20, "Club" => 30, _ => 5 };

            // Block image analysis for Free/Starter
            if (request.ImageBase64 != null && !canAnalyzeImage)
                return Ok(new { reply = "Phân tích hình ảnh chỉ dành cho gói Pro và Club. Nâng cấp tại trang Subscription nhé! ✨" });

            // Truncate history by plan
            if (request.History?.Count > historyLimit)
                request.History = request.History.TakeLast(historyLimit).ToList();

            var hostedRaw = await _context.Matches
                .Include(m => m.Participants)
                .Where(m => m.CreatedByUserID == userId &&
                    (m.Status == "Open" || m.Status == "Full" || m.Status == "InProgress"))
                .Take(3).ToListAsync();

            var joinedRaw = await _context.MatchParticipants
                .Include(p => p.Match).ThenInclude(m => m.Participants)
                .Where(p => p.UserID == userId && p.JoinStatus == "Accepted" &&
                    (p.Match.Status == "Open" || p.Match.Status == "Full" || p.Match.Status == "InProgress"))
                .Take(3).ToListAsync();

            var pendingPayments = await _context.MatchPayments
                .Where(p => p.PayerUserID == userId && p.Status == "Pending")
                .Select(p => new { p.PaymentType, p.Amount })
                .Take(3).ToListAsync();

            var walletBalance = await _walletService.GetBalanceAsync(userId);

            // Build context lines for AI prompt
            var contextLines = new List<string>();
            if (hostedRaw.Any())
                contextLines.Add("Trận đang tổ chức: " + string.Join(", ",
                    hostedRaw.Select(m => $"\"{m.Title ?? m.MatchType}\" ({m.MatchDate:dd/MM}, {m.Status})")));
            if (joinedRaw.Any())
                contextLines.Add("Trận đang tham gia: " + string.Join(", ",
                    joinedRaw.Select(p => $"\"{p.Match.Title ?? p.Match.MatchType}\" ({p.Match.MatchDate:dd/MM})")));
            if (pendingPayments.Any())
                contextLines.Add("Thanh toán chờ duyệt: " + string.Join(", ",
                    pendingPayments.Select(p => $"{p.PaymentType} {p.Amount:N0}đ")));

            var systemPrompt = $"""
Bạn là trợ lý hỗ trợ của SportHub — nền tảng ghép trận thể thao tại Việt Nam.
Trả lời ngắn gọn, thân thiện, bằng tiếng Việt. Tối đa 3-4 câu trừ khi cần giải thích dài hơn.

Thông tin người dùng hiện tại:
- Tên: {user.FullName}
- Số dư ví: {walletBalance:N0} xu
{string.Join("\n", contextLines)}

Bạn hỗ trợ về:
- Cách tạo/tham gia/hủy trận
- Quy trình thanh toán: đặt cọc host, phí tham gia, phí còn lại, nạp ví
- Ví SportHub: nạp tiền bằng chuyển khoản ngân hàng, số dư, lịch sử
- Tìm trận theo môn thể thao, khu vực, trình độ
- Đánh giá sau trận, huy hiệu thành tích
- Kết bạn, nhắn tin với người chơi khác

Khi người dùng hỏi về trận đấu của họ, hệ thống sẽ tự hiển thị thẻ trận — không cần liệt kê tên trận trong câu trả lời.
Nếu câu hỏi không liên quan đến SportHub hoặc thể thao, hãy lịch sự từ chối.

Gói đăng ký của người dùng: {planKey}.
{(canSeeMatches ? "" : "Nếu người dùng hỏi về tìm kiếm hoặc gợi ý trận mới phù hợp với mô tả, hãy lịch sự thông báo tính năng này yêu cầu gói Starter trở lên và khuyến khích nâng cấp. Không cung cấp gợi ý trận cụ thể.")}
{(canCoach ? "Bạn có thể tư vấn chiến thuật thi đấu, phân tích điểm mạnh/yếu, và coaching cá nhân hóa cho người dùng." : "Chỉ hỗ trợ câu hỏi cơ bản về nền tảng, không tư vấn chiến thuật chuyên sâu hay coaching cá nhân.")}
""";

            var reply = await _aiChatService.ChatAsync(systemPrompt, request.History ?? new(), request.Message, request.ImageBase64, request.ImageMimeType);

            // Return match cards when query is match-related
            var matchKeywords = new[] { "trận", "kèo", "tham gia", "ghép", "lịch", "đấu", "đang chơi", "đang tổ chức", "match", "join" };
            var isMatchRelated = matchKeywords.Any(k => request.Message.Contains(k, StringComparison.OrdinalIgnoreCase));

            List<MatchCardDto>? matchCards = null;
            if (canSeeMatches && isMatchRelated && (hostedRaw.Any() || joinedRaw.Any()))
            {
                var hostedIds = hostedRaw.Select(m => m.MatchID).ToHashSet();
                matchCards = hostedRaw.Select(m => new MatchCardDto
                {
                    MatchId   = m.MatchID,
                    Title     = m.Title ?? m.MatchType,
                    MatchType = m.MatchType,
                    DateDisplay = m.MatchDate.ToString("dd/MM"),
                    TimeDisplay = m.StartTime.ToString(@"hh\:mm"),
                    Location  = m.CustomCourtName ?? m.CustomCourtAddress ?? "",
                    SkillRequired = m.SkillRequired,
                    Status    = m.Status,
                    CurrentParticipants = m.Participants.Count(p => p.JoinStatus == "Accepted"),
                    MaxParticipants = m.MaxParticipants,
                    IsHost    = true,
                    IsJoined  = false
                })
                .Concat(joinedRaw
                    .Where(p => !hostedIds.Contains(p.MatchID))
                    .Select(p => new MatchCardDto
                    {
                        MatchId   = p.Match.MatchID,
                        Title     = p.Match.Title ?? p.Match.MatchType,
                        MatchType = p.Match.MatchType,
                        DateDisplay = p.Match.MatchDate.ToString("dd/MM"),
                        TimeDisplay = p.Match.StartTime.ToString(@"hh\:mm"),
                        Location  = p.Match.CustomCourtName ?? p.Match.CustomCourtAddress ?? "",
                        SkillRequired = p.Match.SkillRequired,
                        Status    = p.Match.Status,
                        CurrentParticipants = p.Match.Participants.Count(pp => pp.JoinStatus == "Accepted"),
                        MaxParticipants = p.Match.MaxParticipants,
                        IsHost    = false,
                        IsJoined  = true
                    }))
                .Take(4).ToList();
            }

            return Ok(new { reply, matches = matchCards });
        }
    }

    public class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<AiChatHistoryItem> History { get; set; } = new();
        public string? ImageBase64 { get; set; }
        public string? ImageMimeType { get; set; }
    }

    public class MatchCardDto
    {
        public int MatchId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MatchType { get; set; } = string.Empty;
        public string DateDisplay { get; set; } = string.Empty;
        public string TimeDisplay { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? SkillRequired { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CurrentParticipants { get; set; }
        public int MaxParticipants { get; set; }
        public bool IsHost { get; set; }
        public bool IsJoined { get; set; }
    }
}
