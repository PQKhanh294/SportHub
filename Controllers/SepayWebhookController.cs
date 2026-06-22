using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using SportHub.Hubs;

namespace SportHub.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/sepay")]
    public class SepayWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly INotificationService _notificationService;
        private readonly IWalletService _walletService;
        private readonly IConfiguration _config;
        private readonly ILogger<SepayWebhookController> _logger;

        public SepayWebhookController(
            ApplicationDbContext context,
            IMatchPaymentService matchPaymentService,
            INotificationService notificationService,
            IWalletService walletService,
            IConfiguration config,
            ILogger<SepayWebhookController> logger)
        {
            _context = context;
            _matchPaymentService = matchPaymentService;
            _notificationService = notificationService;
            _walletService = walletService;
            _config = config;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Receive([FromBody] SepayWebhookPayload payload)
        {
            if (!_config.GetValue<bool>("SePay:Enabled"))
                return Ok(new { success = true, message = "SePay disabled" });

            // Verify secret — SePay gửi qua "Authorization: Bearer {token}"
            var expectedSecret = _config["SePay:WebhookSecret"];
            if (!string.IsNullOrEmpty(expectedSecret) &&
                !string.Equals(expectedSecret, "ĐIỀN_SECRET_TOKEN_CỦA_BẠN_VÀO_ĐÂY", StringComparison.Ordinal))
            {
                // SePay sends: Authorization: Bearer {token}
                var authHeader = Request.Headers["Authorization"].ToString();
                var bearerToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authHeader["Bearer ".Length..].Trim()
                    : authHeader.Trim();

                // Fallback: some SePay versions use x-sepay-secret header
                if (!Request.Headers.TryGetValue("x-sepay-secret", out var legacySecret))
                    legacySecret = default;

                var isValid = string.Equals(bearerToken, expectedSecret, StringComparison.Ordinal)
                           || string.Equals(legacySecret.ToString(), expectedSecret, StringComparison.Ordinal);

                if (!isValid)
                {
                    _logger.LogWarning("SePay webhook: invalid secret. Auth='{Auth}'", authHeader);
                    return Unauthorized();
                }
            }

            // Only process incoming transfers
            if (payload.TransferType?.ToLower() != "in")
                return Ok(new { success = true, message = "ignored" });

            _logger.LogInformation("SePay webhook received: id={Id}, content={Content}, amount={Amount}",
                payload.Id, payload.Content, payload.TransferAmount);

            // Find TransactionRef in content (content may contain extra words)
            if (string.IsNullOrWhiteSpace(payload.Content))
                return Ok(new { success = true, message = "no content" });

            var words = payload.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
                // Fallback: check WalletTopUpRequest
                WalletTopUpRequest? topUp = null;
                foreach (var word in words)
                {
                    topUp = await _context.WalletTopUpRequests
                        .FirstOrDefaultAsync(t => t.TransactionRef == word && t.Status == "Pending");
                    if (topUp != null) break;
                }

                if (topUp != null)
                {
                    _logger.LogInformation("SePay webhook: matched WalletTopUp {Ref}, amount={Amount}", topUp.TransactionRef, payload.TransferAmount);
                    var credited = await _walletService.ConfirmTopUpAsync(topUp.TransactionRef, payload.TransferAmount);
                    if (credited)
                    {
                        var newBalance = await _walletService.GetBalanceAsync(topUp.UserID);
                        await _notificationService.CreateAsync(
                            topUp.UserID,
                            "WalletCredit",
                            "Ví đã được nạp tiền",
                            $"Số dư ví của bạn đã được cộng {payload.TransferAmount:N0} VND. Số dư hiện tại: {newBalance:N0} VND.",
                            "/Wallet");
                    }
                    return Ok(new { success = credited, type = "topup" });
                }

                _logger.LogInformation("SePay webhook: no matching pending payment found for content '{Content}'.", payload.Content);
                return Ok(new { success = true, message = "no match" });
            }

            MatchPayment mp = matched!; // null-forgiving: null guard above guarantees non-null

            if (payload.TransferAmount < mp.Amount)
            {
                _logger.LogWarning("SePay webhook: amount {Received} < required {Required} for payment {Id}.",
                    payload.TransferAmount, mp.Amount, mp.MatchPaymentID);
                return Ok(new { success = true, message = "amount_insufficient" });
            }

            var confirmed = await _matchPaymentService.ConfirmPaymentAsync(mp.MatchPaymentID);
            if (!confirmed)
            {
                _logger.LogWarning("SePay webhook: ConfirmPaymentAsync failed for payment {Id}.", mp.MatchPaymentID);
                return Ok(new { success = false, message = "confirm_failed" });
            }

            var matchTitle = mp.Match.Title ?? mp.Match.MatchType;
            var notifType = mp.PaymentType switch
            {
                "HostDeposit"   => ("MatchPaymentConfirmed", "Đặt cọc đã được xác nhận tự động", $"Khoản đặt cọc {mp.Amount:N0} VND cho trận \"{matchTitle}\" đã được ghi nhận qua hệ thống SePay."),
                "PlayerFee"     => ("MatchPaymentConfirmed", "Phí tham gia đã được xác nhận tự động", $"Chỗ của bạn tại trận \"{matchTitle}\" đã được xác nhận qua SePay!"),
                "HostRemaining" => ("MatchPaymentConfirmed", "Phí còn lại đã được xác nhận tự động", $"Phí còn lại {mp.Amount:N0} VND cho trận \"{matchTitle}\" đã được ghi nhận qua SePay."),
                _ => ("MatchPaymentConfirmed", "Thanh toán đã được xác nhận", $"Giao dịch cho trận \"{matchTitle}\" đã được xác nhận.")
            };

            await _notificationService.CreateAsync(
                mp.PayerUserID,
                notifType.Item1,
                notifType.Item2,
                notifType.Item3,
                $"/Matchmaking/Details?id={mp.MatchID}");

            _logger.LogInformation("SePay webhook: auto-confirmed payment {Id} for match {MatchId}.", mp.MatchPaymentID, mp.MatchID);
            return Ok(new { success = true, paymentId = mp.MatchPaymentID });
        }
    }

    public class SepayWebhookPayload
    {
        public int Id { get; set; }
        public string? Gateway { get; set; }
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? Content { get; set; }
        public string? TransferType { get; set; } // "in" | "out"
        public decimal TransferAmount { get; set; }
        public decimal Accumulated { get; set; }
        public string? SubAccount { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }
    }
}
