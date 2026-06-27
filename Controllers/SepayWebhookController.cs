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
        private readonly ISubscriptionService _subscriptionService;
        private readonly IConfiguration _config;
        private readonly ILogger<SepayWebhookController> _logger;

        public SepayWebhookController(
            ApplicationDbContext context,
            IMatchPaymentService matchPaymentService,
            INotificationService notificationService,
            IWalletService walletService,
            ISubscriptionService subscriptionService,
            IConfiguration config,
            ILogger<SepayWebhookController> logger)
        {
            _context = context;
            _matchPaymentService = matchPaymentService;
            _notificationService = notificationService;
            _walletService = walletService;
            _subscriptionService = subscriptionService;
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
                // SePay gửi: Authorization: Apikey {token} hoặc Bearer {token}
                var authHeader = Request.Headers["Authorization"].ToString();
                string extractedToken;
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    extractedToken = authHeader["Bearer ".Length..].Trim();
                else if (authHeader.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase))
                    extractedToken = authHeader["Apikey ".Length..].Trim();
                else
                    extractedToken = authHeader.Trim();

                // Fallback: x-sepay-secret header
                if (!Request.Headers.TryGetValue("x-sepay-secret", out var legacySecret))
                    legacySecret = default;

                var isValid = string.Equals(extractedToken, expectedSecret, StringComparison.Ordinal)
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

            // Normalize: banks sometimes strip dashes (TOPUP-14-xxx → TOPUP14xxx)
            static string Norm(string? s) => (s ?? "").Replace("-", "").Replace(" ", "").ToUpperInvariant();

            var words = payload.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var normalizedWords = words.Select(Norm).ToHashSet();

            // Match MatchPayment — load pending then compare normalized on both sides
            var pendingPayments = await _context.MatchPayments
                .Include(p => p.Match)
                .Where(p => p.Status == "Pending")
                .ToListAsync();

            MatchPayment? matched = pendingPayments.FirstOrDefault(p =>
                words.Any(w => w.Equals(p.TransactionRef, StringComparison.OrdinalIgnoreCase)) ||
                normalizedWords.Contains(Norm(p.TransactionRef)));

            if (matched == null)
            {
                // Fallback: check WalletTopUpRequest
                var pendingTopUps = await _context.WalletTopUpRequests
                    .Where(t => t.Status == "Pending")
                    .ToListAsync();

                var topUp = pendingTopUps.FirstOrDefault(t =>
                    words.Any(w => w.Equals(t.TransactionRef, StringComparison.OrdinalIgnoreCase)) ||
                    normalizedWords.Contains(Norm(t.TransactionRef)));

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
                            $"Số dư ví của bạn đã được cộng {payload.TransferAmount:N0} xu. Số dư hiện tại: {newBalance:N0} xu.",
                            "/Wallet");
                    }
                    return Ok(new { success = credited, type = "topup" });
                }

                // Check SubscriptionOrder
                var pendingSubs = await _context.SubscriptionOrders
                    .Where(o => o.Status == "Pending")
                    .ToListAsync();

                var subOrder = pendingSubs.FirstOrDefault(o =>
                    words.Any(w => w.Equals(o.TransactionRef, StringComparison.OrdinalIgnoreCase)) ||
                    normalizedWords.Contains(Norm(o.TransactionRef)));

                if (subOrder != null)
                {
                    _logger.LogInformation("SePay webhook: matched SubscriptionOrder {Ref}", subOrder.TransactionRef);
                    var activated = await _subscriptionService.ConfirmOrderAsync(subOrder.TransactionRef, payload.TransferAmount);
                    if (activated)
                    {
                        await _notificationService.CreateAsync(
                            subOrder.UserID,
                            "System",
                            $"Đăng ký gói {subOrder.PlanKey} thành công!",
                            $"Gói {subOrder.PlanKey} đã được kích hoạt. Tận hưởng các tính năng cao cấp ngay nhé!",
                            "/Subscription");
                    }
                    return Ok(new { success = activated, type = "subscription" });
                }

                // Check UserMatchCredit
                var pendingCredits = await _context.UserMatchCredits
                    .Where(c => c.Status == "Pending")
                    .ToListAsync();

                var creditOrder = pendingCredits.FirstOrDefault(c =>
                    words.Any(w => w.Equals(c.TransactionRef, StringComparison.OrdinalIgnoreCase)) ||
                    normalizedWords.Contains(Norm(c.TransactionRef)));

                if (creditOrder != null)
                {
                    _logger.LogInformation("SePay webhook: matched CreditOrder {Ref}", creditOrder.TransactionRef);
                    var credited2 = await _subscriptionService.ConfirmCreditOrderAsync(creditOrder.TransactionRef, payload.TransferAmount);
                    if (credited2)
                    {
                        await _notificationService.CreateAsync(
                            creditOrder.UserID,
                            "System",
                            "Mua credit trận đấu thành công!",
                            "Bạn vừa nhận 5 credit trận đấu. Dùng để tham gia thêm các trận nhé!",
                            "/Subscription");
                    }
                    return Ok(new { success = credited2, type = "credit" });
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
                "HostDeposit"   => ("MatchPaymentConfirmed", "Đặt cọc đã được xác nhận tự động", $"Khoản đặt cọc {mp.Amount:N0} xu cho trận \"{matchTitle}\" đã được ghi nhận qua hệ thống SePay."),
                "PlayerFee"     => ("MatchPaymentConfirmed", "Phí tham gia đã được xác nhận tự động", $"Chỗ của bạn tại trận \"{matchTitle}\" đã được xác nhận qua SePay!"),
                "HostRemaining" => ("MatchPaymentConfirmed", "Phí còn lại đã được xác nhận tự động", $"Phí còn lại {mp.Amount:N0} xu cho trận \"{matchTitle}\" đã được ghi nhận qua SePay."),
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
