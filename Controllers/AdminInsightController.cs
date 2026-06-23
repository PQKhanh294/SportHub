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
Trả lời ngắn gọn bằng tiếng Việt, tối đa 4-5 câu.
Đưa ra nhận định cụ thể, có số liệu, và đề xuất hành động thiết thực.
Không nói "dựa trên dữ liệu" hay "theo thông tin" — đi thẳng vào nhận định.
""";

            string userMessage = section switch
            {
                "payments" => $"""
Phân tích tình trạng thanh toán chờ xác nhận của SportHub:
- Tổng đang chờ: {p1} giao dịch, tổng giá trị: {p2} VND
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
