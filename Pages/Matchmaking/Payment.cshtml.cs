using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    [Authorize]
    public class PaymentModel : PageModel
    {
        private readonly IMatchService _matchService;
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public PaymentModel(
            IMatchService matchService,
            IMatchPaymentService matchPaymentService,
            INotificationService notificationService,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _matchService = matchService;
            _matchPaymentService = matchPaymentService;
            _notificationService = notificationService;
            _env = env;
            _config = config;
        }

        public PaymentPageItem? Item { get; set; }

        public async Task<IActionResult> OnGetAsync(int matchId, string type)
        {
            ViewData["ActivePage"] = "Matchmaking";
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var match = await _matchService.GetMatchDetailsAsync(matchId);
            if (match == null) return RedirectToPage("/Matchmaking/Index");

            var paymentType = NormalizeType(type);
            if (paymentType == null) return RedirectToPage("/Matchmaking/Index");

            // Authorization checks
            if (paymentType == "HostDeposit" && match.CreatedByUserID != userId)
                return Forbid();
            if (paymentType == "HostRemaining" && match.CreatedByUserID != userId)
                return Forbid();
            if (paymentType == "PlayerFee")
            {
                var myParticipant = match.Participants.FirstOrDefault(p => p.UserID == userId);
                if (myParticipant == null || myParticipant.JoinStatus != "Approved")
                    return RedirectToPage("/Matchmaking/Details", new { id = matchId });
            }

            var activePayment = await _matchPaymentService.GetActivePaymentAsync(matchId, userId, paymentType);

            var bankId = _config["SportHubPayment:BankId"] ?? "VCB";
            var accountNo = _config["SportHubPayment:AccountNumber"] ?? "0000000000";
            var accountName = _config["SportHubPayment:AccountName"] ?? "VO VAN HAI";

            decimal amount = paymentType switch
            {
                "HostDeposit" => _matchPaymentService.CalculateHostDeposit(match.MaxParticipants),
                "PlayerFee" => 5_000m,
                "HostRemaining" => _matchPaymentService.CalculateHostDeposit(match.MaxParticipants),
                _ => 0
            };

            var matchTitle = string.IsNullOrWhiteSpace(match.Title) ? match.MatchType : match.Title;
            var info = Uri.EscapeDataString($"SportHub {paymentType} {matchId}");
            var qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact.png?amount={(int)amount}&addInfo={info}&accountName={Uri.EscapeDataString(accountName)}";

            var myParticipantForDisplay = match.Participants.FirstOrDefault(p => p.UserID == userId);

            Item = new PaymentPageItem
            {
                MatchId = matchId,
                MatchTitle = matchTitle ?? "Trận đấu",
                PaymentType = paymentType,
                Amount = amount,
                QrUrl = qrUrl,
                BankId = bankId,
                AccountNumber = accountNo,
                AccountName = accountName,
                ExistingReceiptUrl = activePayment?.ReceiptUrl,
                PaymentId = activePayment?.MatchPaymentID,
                Deadline = myParticipantForDisplay?.PlayerFeeDeadline
            };

            return Page();
        }

        public async Task<IActionResult> OnPostUploadReceiptAsync(int matchId, string type, IFormFile? receipt)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var paymentType = NormalizeType(type);
            if (paymentType == null || receipt == null || receipt.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file biên lai.";
                return RedirectToPage(new { matchId, type });
            }

            if (receipt.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File quá lớn (tối đa 5MB).";
                return RedirectToPage(new { matchId, type });
            }

            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
            var ext = Path.GetExtension(receipt.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
            {
                TempData["ErrorMessage"] = "Chỉ chấp nhận JPG, PNG, WebP hoặc PDF.";
                return RedirectToPage(new { matchId, type });
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "receipts");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{paymentType.ToLower()}-{matchId}-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await receipt.CopyToAsync(stream);

            var receiptUrl = $"/uploads/receipts/{fileName}";
            bool saved = paymentType switch
            {
                "HostDeposit" => await _matchPaymentService.SubmitHostDepositReceiptAsync(matchId, userId, receiptUrl),
                "PlayerFee" => await _matchPaymentService.SubmitPlayerFeeReceiptAsync(matchId, userId, receiptUrl),
                "HostRemaining" => await _matchPaymentService.SubmitHostRemainingReceiptAsync(matchId, userId, receiptUrl),
                _ => false
            };

            if (!saved)
            {
                TempData["ErrorMessage"] = "Không tìm thấy giao dịch hợp lệ hoặc đã hết hạn.";
                return RedirectToPage(new { matchId, type });
            }

            // Notify admin (use system notification to a well-known admin userId if configured)
            TempData["SuccessMessage"] = "Biên lai đã được tải lên. Chờ admin xác nhận (thường trong vài giờ).";
            return RedirectToPage("/Matchmaking/Details", new { id = matchId });
        }

        private static string? NormalizeType(string? type) => type?.ToLower() switch
        {
            "deposit" or "hostdeposit" => "HostDeposit",
            "playerfee" or "fee" => "PlayerFee",
            "remaining" or "hostremaining" => "HostRemaining",
            _ => null
        };

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        public class PaymentPageItem
        {
            public int MatchId { get; set; }
            public string MatchTitle { get; set; } = string.Empty;
            public string PaymentType { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string QrUrl { get; set; } = string.Empty;
            public string BankId { get; set; } = string.Empty;
            public string AccountNumber { get; set; } = string.Empty;
            public string AccountName { get; set; } = string.Empty;
            public string? ExistingReceiptUrl { get; set; }
            public int? PaymentId { get; set; }
            public DateTime? Deadline { get; set; }
            public bool AlreadySubmitted => !string.IsNullOrWhiteSpace(ExistingReceiptUrl);
            public string TypeLabel => PaymentType switch
            {
                "HostDeposit" => "Đặt cọc host",
                "PlayerFee" => "Phí tham gia",
                "HostRemaining" => "Phí còn lại",
                _ => "Thanh toán"
            };
        }
    }
}
