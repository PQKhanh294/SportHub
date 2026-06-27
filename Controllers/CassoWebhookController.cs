using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Controllers
{
    [ApiController]
    [Route("api/casso")]
    public class CassoWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _config;
        private readonly ILogger<CassoWebhookController> _logger;

        public CassoWebhookController(
            ApplicationDbContext context,
            IMatchPaymentService matchPaymentService,
            INotificationService notificationService,
            IConfiguration config,
            ILogger<CassoWebhookController> logger)
        {
            _context = context;
            _matchPaymentService = matchPaymentService;
            _notificationService = notificationService;
            _config = config;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Receive([FromBody] CassoWebhookPayload payload)
        {
            if (!_config.GetValue<bool>("Casso:Enabled"))
                return Ok(new { success = true, message = "Casso disabled" });

            // Verify secret token
            var expectedToken = _config["Casso:SecretKey"];
            if (!string.IsNullOrEmpty(expectedToken))
            {
                Request.Headers.TryGetValue("x-api-key", out var receivedToken);
                if (receivedToken != expectedToken)
                {
                    _logger.LogWarning("Casso webhook: invalid API key.");
                    return Unauthorized();
                }
            }

            // Casso sends array of records in "data" field
            if (payload.Data == null || payload.Data.Count == 0)
                return Ok(new { success = true });

            foreach (var record in payload.Data)
            {
                // Only process credit (money in)
                if (record.Amount <= 0) continue;

                _logger.LogInformation("Casso webhook: id={Id}, description={Desc}, amount={Amount}",
                    record.Id, record.Description, record.Amount);

                if (string.IsNullOrWhiteSpace(record.Description)) continue;

                // Find TransactionRef in description
                var words = record.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                MatchPayment? matched = null;
                foreach (var word in words)
                {
                    matched = await _context.MatchPayments
                        .Include(p => p.Match)
                        .FirstOrDefaultAsync(p => p.TransactionRef == word && p.Status == "Pending");
                    if (matched != null) break;
                }

                if (matched == null)
                {
                    _logger.LogInformation("Casso: no match for description '{Desc}'.", record.Description);
                    continue;
                }

                MatchPayment mp = matched!;

                if (record.Amount < mp.Amount)
                {
                    _logger.LogWarning("Casso: amount {Recv} < required {Req} for payment {Id}.",
                        record.Amount, mp.Amount, mp.MatchPaymentID);
                    continue;
                }

                var confirmed = await _matchPaymentService.ConfirmPaymentAsync(mp.MatchPaymentID);
                if (!confirmed) continue;

                var matchTitle = mp.Match.Title ?? mp.Match.MatchType;
                var (notifType, title, body) = mp.PaymentType switch
                {
                    "HostDeposit"   => ("MatchPaymentConfirmed", "Đặt cọc đã được xác nhận tự động", $"Khoản đặt cọc {mp.Amount:N0} xu cho trận \"{matchTitle}\" đã được ghi nhận."),
                    "PlayerFee"     => ("MatchPaymentConfirmed", "Phí tham gia đã được xác nhận tự động", $"Chỗ của bạn tại trận \"{matchTitle}\" đã được xác nhận!"),
                    "HostRemaining" => ("MatchPaymentConfirmed", "Phí còn lại đã được xác nhận tự động", $"Phí còn lại {mp.Amount:N0} xu cho trận \"{matchTitle}\" đã được ghi nhận."),
                    _ => ("MatchPaymentConfirmed", "Thanh toán đã được xác nhận", $"Giao dịch cho trận \"{matchTitle}\" đã được xác nhận.")
                };

                await _notificationService.CreateAsync(
                    mp.PayerUserID, notifType, title, body,
                    $"/Matchmaking/Details?id={mp.MatchID}");

                _logger.LogInformation("Casso: auto-confirmed payment {Id} for match {MatchId}.", mp.MatchPaymentID, mp.MatchID);
            }

            return Ok(new { success = true });
        }
    }

    public class CassoWebhookPayload
    {
        public List<CassoRecord> Data { get; set; } = new();
    }

    public class CassoRecord
    {
        public long Id { get; set; }
        public string? Tid { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public decimal CusumBalance { get; set; }
        public string? When { get; set; }
        public string? BankSubAccId { get; set; }
        public string? BankAbbreviation { get; set; }
    }
}
