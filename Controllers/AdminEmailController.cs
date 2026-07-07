using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    public class AdminEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AdminEmailController(IEmailService emailService, ApplicationDbContext context, IConfiguration config)
        {
            _emailService = emailService;
            _context = context;
            _config = config;
        }

        // Chẩn đoán Resend: gửi mail thử và trả nguyên văn phản hồi API.
        // Domain chưa verify → Resend trả 403 kèm message rõ ràng trong detail.
        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail([FromQuery] string? to)
        {
            var target = string.IsNullOrWhiteSpace(to)
                ? User.FindFirstValue(ClaimTypes.Email)
                : to.Trim();
            if (string.IsNullOrWhiteSpace(target))
                return BadRequest(new { success = false, detail = "Không xác định được địa chỉ nhận." });

            var (success, detail) = await _emailService.SendTestAsync(target);
            return Ok(new { success, to = target, detail });
        }

        // Thanh toán đang chờ người dùng thao tác (chưa nộp biên lai) — dùng cho admin gửi nhắc nhở thủ công
        [HttpGet("pending-payments")]
        public async Task<IActionResult> GetPendingPayments()
        {
            var now = DateTime.UtcNow;
            var items = await _context.MatchPayments
                .Include(p => p.Match)
                .Include(p => p.Payer)
                .Where(p => p.Status == "Pending" && p.ReceiptUrl == null)
                .OrderBy(p => p.ExpiresAt ?? DateTime.MaxValue)
                .Select(p => new
                {
                    paymentId = p.MatchPaymentID,
                    matchId = p.MatchID,
                    matchTitle = p.Match.Title ?? p.Match.MatchType,
                    payerUserId = p.PayerUserID,
                    payerName = p.Payer.FullName,
                    payerEmail = p.Payer.Email,
                    paymentType = p.PaymentType,
                    amount = p.Amount,
                    expiresAt = p.ExpiresAt,
                    isOverdue = p.ExpiresAt.HasValue && p.ExpiresAt.Value < now
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("send-payment-reminder")]
        public async Task<IActionResult> SendPaymentReminder([FromBody] SendPaymentReminderRequest request)
        {
            if (request.PaymentIds == null || request.PaymentIds.Count == 0)
                return BadRequest(new { success = false, detail = "Chưa chọn khoản thanh toán nào." });

            var baseUrl = (_config["App:BaseUrl"] ?? "https://sporthub-dn.id.vn/").TrimEnd('/');
            var payments = await _context.MatchPayments
                .Include(p => p.Match)
                .Include(p => p.Payer)
                .Where(p => request.PaymentIds.Contains(p.MatchPaymentID) && p.Status == "Pending")
                .ToListAsync();

            var sent = 0;
            var skipped = 0;
            foreach (var p in payments)
            {
                if (p.Payer?.NotifyByEmail != true || string.IsNullOrWhiteSpace(p.Payer.Email))
                {
                    skipped++;
                    continue;
                }

                var title = string.IsNullOrWhiteSpace(p.Match.Title) ? p.Match.MatchType : p.Match.Title;
                var urgent = p.ExpiresAt.HasValue && p.ExpiresAt.Value <= DateTime.UtcNow.AddHours(4);
                await _emailService.SendPaymentReminderAsync(p.Payer.Email, p.Payer.FullName, title, p.PaymentType,
                    p.Amount, p.ExpiresAt ?? DateTime.UtcNow.AddHours(24),
                    $"{baseUrl}/Matchmaking/Payment?matchId={p.MatchID}&type={p.PaymentType.ToLower()}", urgent);
                p.ReminderSentAt = DateTime.UtcNow;
                sent++;
            }

            if (sent > 0) await _context.SaveChangesAsync();

            return Ok(new { success = true, sent, skipped });
        }

        // 50 email gần nhất — admin tra cứu deliverability
        [HttpGet("email-log")]
        public async Task<IActionResult> GetEmailLog()
        {
            var logs = await _context.EmailLogs
                .OrderByDescending(l => l.SentAt)
                .Take(50)
                .Select(l => new
                {
                    l.EmailLogID,
                    l.ToEmail,
                    l.Subject,
                    l.TemplateType,
                    l.Success,
                    l.ErrorDetail,
                    l.SentAt
                })
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var sentToday = await _context.EmailLogs.CountAsync(l => l.SentAt >= today);
            var failedToday = await _context.EmailLogs.CountAsync(l => l.SentAt >= today && !l.Success);

            return Ok(new { logs, sentToday, failedToday });
        }
    }

    public class SendPaymentReminderRequest
    {
        public List<int> PaymentIds { get; set; } = new();
    }
}
