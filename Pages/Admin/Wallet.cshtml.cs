using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    public class WalletModel : PageModel
    {
        private readonly IPromotionService _promotionService;
        private readonly IUserService _userService;

        public WalletModel(IPromotionService promotionService, IUserService userService)
        {
            _promotionService = promotionService;
            _userService = userService;
        }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public const int PageSize = 20;

        public List<WalletUserDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["AdminPage"] = "Wallet";
            if (!await IsAdminAsync()) return Forbid();

            Users = await _promotionService.GetUsersForWalletManagementAsync(Search, PageNumber, PageSize);
            TotalCount = await _promotionService.GetUsersCountAsync(Search);
            return Page();
        }

        public async Task<IActionResult> OnPostCreditAsync(int userId, decimal amount, string note)
        {
            if (!await IsAdminAsync()) return Forbid();

            if (amount <= 0)
            {
                ErrorMessage = "Số tiền phải lớn hơn 0.";
                return RedirectToPage(new { Search, PageNumber });
            }
            if (string.IsNullOrWhiteSpace(note))
            {
                ErrorMessage = "Vui lòng nhập nội dung thông báo.";
                return RedirectToPage(new { Search, PageNumber });
            }

            try
            {
                var adminId = GetAdminId();
                await _promotionService.AdminCreditAsync(adminId, userId, amount, note.Trim());
                SuccessMessage = $"Đã cộng {amount:N0}đ vào ví người dùng #{userId}.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage(new { Search, PageNumber });
        }

        public async Task<IActionResult> OnPostIssueVoucherAsync(int userId, decimal voucherAmount, DateTime? voucherExpiry, string? voucherNote)
        {
            if (!await IsAdminAsync()) return Forbid();

            if (voucherAmount <= 0)
            {
                ErrorMessage = "Số tiền voucher phải lớn hơn 0.";
                return RedirectToPage(new { Search, PageNumber });
            }

            try
            {
                var adminId = GetAdminId();
                var voucher = await _promotionService.IssueVoucherAsync(adminId, userId, null, voucherAmount, voucherExpiry?.ToUniversalTime(), voucherNote);
                SuccessMessage = $"Đã phát voucher {voucher.Code} ({voucherAmount:N0}đ) cho người dùng #{userId}.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage(new { Search, PageNumber });
        }

        private int GetAdminId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private async Task<bool> IsAdminAsync()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out var id)) return false;
            return await _userService.IsAdminAsync(id);
        }
    }
}
