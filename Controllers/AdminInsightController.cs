using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;
using System.Security.Claims;

namespace SportHub.Controllers
{
    [ApiController]
    [Route("api/admin-insight")]
    [Authorize]
    public class AdminInsightController : ControllerBase
    {
        private readonly IAiChatService _aiChatService;
        private readonly ApplicationDbContext _context;

        public AdminInsightController(IAiChatService aiChatService, ApplicationDbContext context)
        {
            _aiChatService = aiChatService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetInsight(
            [FromQuery] string section,
            [FromQuery] string? p1 = null,
            [FromQuery] string? p2 = null,
            [FromQuery] string? p3 = null,
            [FromQuery] string? p4 = null)
        {
            if (!await IsAdminAsync()) return Forbid();

            var systemPrompt = """
Bạn là chuyên gia phân tích dữ liệu cho nền tảng thể thao SportHub (Việt Nam).
Trả lời bằng tiếng Việt, cấu trúc rõ ràng, tối đa 120 từ.

Quy tắc định dạng bắt buộc:
- Dùng ## để đặt heading ngắn (ví dụ: ## Nhận định, ## Đề xuất)
- Dùng **text** để in đậm số liệu và từ khóa quan trọng
- Dùng - để liệt kê bullet points (mỗi ý 1 dòng)
- Không dùng dấu ngoặc đơn () hay [] quanh bullet

Không nói "dựa trên dữ liệu" — đi thẳng vào nhận định có số cụ thể.
""";

            string userMessage = section switch
            {
                "payments" => $"""
Phân tích tình trạng thanh toán chờ xác nhận của SportHub:
- Tổng đang chờ: {p1} giao dịch, tổng giá trị: {p2} xu
- Loại: {p3} đặt cọc host, {p4} phí player
Nhận định xu hướng và đề xuất cải thiện tỷ lệ xác nhận.
""",
                "users" => $"""
Phân tích người dùng SportHub:
- Tổng: {p1} tài khoản, hoạt động: {p2}, bị khóa: {p3}
- Đăng ký tháng này: {p4}
Đánh giá sức khỏe cộng đồng và gợi ý tăng retention.
""",
                "reviews" => $"""
Phân tích đánh giá người dùng SportHub:
- Tổng: {p1} đánh giá, điểm trung bình: {p2}/5
- 5 sao: {p3}, 4 sao: {p4}
Nhận định chất lượng trải nghiệm và đề xuất cải thiện.
""",
                "reports" => $"""
Phân tích báo cáo vi phạm SportHub:
- Đang chờ xử lý: {p1}, đã xử lý: {p2}
- Vi phạm nghiêm trọng (score ≥7): {p3}, trung bình (4-6): {p4}
Đánh giá mức độ vi phạm và đề xuất chính sách kiểm duyệt.
""",
                "dashboard" => $"""
Đề xuất 4-5 chiến lược tăng trưởng cụ thể cho SportHub — nền tảng ghép trận thể thao tại Việt Nam.
Tập trung vào: thu hút người dùng mới, tăng tần suất sử dụng, tăng doanh thu.
Mỗi gợi ý 1 câu, thực tế và có thể triển khai ngay.
Stats hiện tại: {p1} người dùng, {p2} trận đấu, {p3} đánh giá.
""",
                _ => "Phân tích tình trạng tổng quan và đề xuất cải thiện cho SportHub."
            };

            var insight = await _aiChatService.ChatAsync(systemPrompt, new(), userMessage, null, null);
            return Ok(new { insight });
        }

        [HttpGet("suggest-promo-codes")]
        public async Task<IActionResult> SuggestPromoCodes([FromQuery] string campaignName, [FromQuery] int count = 5)
        {
            if (!await IsAdminAsync()) return Forbid();
            if (string.IsNullOrWhiteSpace(campaignName)) return BadRequest(new { codes = Array.Empty<string>() });

            count = Math.Clamp(count, 1, 10);

            var prompt = $"""
Bạn là AI tạo mã khuyến mãi cho ứng dụng thể thao SportHub (Việt Nam).
Tạo đúng {count} mã khuyến mãi cho chiến dịch: "{campaignName}".
Yêu cầu:
- Mỗi mã tối đa 10 ký tự, CHỈ gồm chữ IN HOA (A-Z) và chữ số (0-9), KHÔNG dấu gạch ngang, KHÔNG khoảng trắng, KHÔNG ký tự đặc biệt
- Mã phải có ý nghĩa liên quan đến tên chiến dịch, dễ nhớ, ví dụ: SUMMER26, NEWUSER, BIRTHDAY, SPORTWL
- Trả về đúng {count} mã, mỗi mã trên 1 dòng, không đánh số, không giải thích
""";

            var result = await _aiChatService.ChatAsync(prompt, new(), "Tạo mã ngay.", null, null);
            var codes = (result ?? "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().ToUpper())
                .Where(l => l.Length >= 4 && l.Length <= 10 && l.All(c => char.IsLetterOrDigit(c)))
                .Distinct()
                .Take(count)
                .ToList();

            return Ok(new { codes });
        }

        [HttpGet("campaign-eligible-count/{campaignId}")]
        public async Task<IActionResult> GetCampaignEligibleCount(int campaignId, [FromServices] IPromotionService promotionService)
        {
            if (!await IsAdminAsync()) return Forbid();
            var count = await promotionService.GetEligibleUserCountAsync(campaignId);
            return Ok(new { count });
        }

        private async Task<bool> IsAdminAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId) || userId <= 0) return false;
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserID == userId && ur.Role.RoleName == "Admin");
        }
    }
}
