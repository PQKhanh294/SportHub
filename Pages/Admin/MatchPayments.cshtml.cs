using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Hubs;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    [Authorize]
    public class MatchPaymentsModel : PageModel
    {
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context;

        public MatchPaymentsModel(
            IMatchPaymentService matchPaymentService,
            INotificationService notificationService,
            IHubContext<NotificationHub> hubContext,
            ApplicationDbContext context)
        {
            _matchPaymentService = matchPaymentService;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _context = context;
        }

        public List<MatchPaymentAdminItem> Payments { get; set; } = new();
        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "Payments";
            if (!await IsAdminAsync()) return Forbid();
            Payments = await _matchPaymentService.GetPendingPaymentsAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetCountsAsync()
        {
            if (!await IsAdminAsync()) return new JsonResult(new { pendingPayments = 0 });
            var count = (await _matchPaymentService.GetPendingPaymentsAsync()).Count;
            return new JsonResult(new { pendingPayments = count });
        }

        public async Task<IActionResult> OnPostConfirmAsync(int paymentId)
        {
            if (!await IsAdminAsync()) return Forbid();

            var payment = await _context.MatchPayments
                .Include(p => p.Match)
                .Include(p => p.Payer)
                .FirstOrDefaultAsync(p => p.MatchPaymentID == paymentId);

            if (payment == null)
            {
                ErrorMessage = "Không tìm thấy giao dịch.";
                return RedirectToPage();
            }

            var confirmed = await _matchPaymentService.ConfirmPaymentAsync(paymentId);
            if (!confirmed)
            {
                ErrorMessage = "Không thể xác nhận giao dịch này.";
                return RedirectToPage();
            }

            var matchTitle = payment.Match.Title ?? payment.Match.MatchType;

            // Send notification based on payment type
            switch (payment.PaymentType)
            {
                case "HostDeposit":
                    await _notificationService.CreateAsync(
                        payment.PayerUserID,
                        "MatchPaymentConfirmed",
                        "Đặt cọc đã được xác nhận",
                        $"Khoản đặt cọc {payment.Amount:N0} VND cho trận \"{matchTitle}\" đã được xác nhận. Trận của bạn hiện đã được đăng!",
                        $"/Matchmaking/Details?id={payment.MatchID}");
                    break;

                case "PlayerFee":
                    // Notify player
                    await _notificationService.CreateAsync(
                        payment.PayerUserID,
                        "MatchPaymentConfirmed",
                        "Phí tham gia đã được xác nhận",
                        $"Chỗ của bạn tại trận \"{matchTitle}\" đã được xác nhận!",
                        $"/Matchmaking/Details?id={payment.MatchID}");

                    // Notify host
                    await _notificationService.CreateAsync(
                        payment.Match.CreatedByUserID,
                        "MatchApprove",
                        "Player đã thanh toán — chỗ được giữ",
                        $"{payment.Payer.FullName} đã thanh toán phí tham gia. Chỗ trong trận \"{matchTitle}\" đã được giữ.",
                        $"/Matchmaking/Details?id={payment.MatchID}");
                    break;

                case "HostRemaining":
                    await _notificationService.CreateAsync(
                        payment.PayerUserID,
                        "MatchPaymentConfirmed",
                        "Phí còn lại đã được xác nhận",
                        $"Phí còn lại {payment.Amount:N0} VND cho trận \"{matchTitle}\" đã được xác nhận.",
                        $"/Matchmaking/Details?id={payment.MatchID}");
                    break;
            }

            var countAfterConfirm = (await _matchPaymentService.GetPendingPaymentsAsync()).Count;
            await _hubContext.Clients.Group("role:Admin")
                .SendAsync("admin_counts_update", new { pendingPayments = countAfterConfirm });

            SuccessMessage = $"Đã xác nhận giao dịch #{paymentId}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int paymentId)
        {
            if (!await IsAdminAsync()) return Forbid();

            var payment = await _context.MatchPayments
                .Include(p => p.Match)
                .FirstOrDefaultAsync(p => p.MatchPaymentID == paymentId);

            if (payment == null)
            {
                ErrorMessage = "Không tìm thấy giao dịch.";
                return RedirectToPage();
            }

            var rejected = await _matchPaymentService.RejectPaymentAsync(paymentId);
            if (rejected)
            {
                await _notificationService.CreateAsync(
                    payment.PayerUserID,
                    "System",
                    "Biên lai không hợp lệ",
                    $"Biên lai thanh toán cho trận \"{payment.Match.Title ?? payment.Match.MatchType}\" bị từ chối. Vui lòng kiểm tra lại và tải lại biên lai.",
                    $"/Matchmaking/Payment?matchId={payment.MatchID}&type={payment.PaymentType.ToLower()}");
                SuccessMessage = $"Đã từ chối giao dịch #{paymentId}.";
            }
            else
            {
                ErrorMessage = "Không thể từ chối giao dịch này.";
            }

            var countAfterReject = (await _matchPaymentService.GetPendingPaymentsAsync()).Count;
            await _hubContext.Clients.Group("role:Admin")
                .SendAsync("admin_counts_update", new { pendingPayments = countAfterReject });

            return RedirectToPage();
        }

        private async Task<bool> IsAdminAsync()
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return false;
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserID == userId && ur.Role.RoleName == "Admin");
        }
    }
}
