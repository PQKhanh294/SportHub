using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Wallet
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IWalletService _walletService;
        private readonly IPromotionService _promotionService;

        public IndexModel(IWalletService walletService, IPromotionService promotionService)
        {
            _walletService = walletService;
            _promotionService = promotionService;
        }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public decimal Balance { get; set; }
        public List<WalletTransaction> Transactions { get; set; } = new();
        public List<WalletTopUpRequest> TopUpHistory { get; set; } = new();
        public List<UserVoucher> MyVouchers { get; set; } = new();
        public List<PromotionRedemption> PromoRedemptions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Wallet";
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            Balance = await _walletService.GetBalanceAsync(userId);
            Transactions = await _walletService.GetHistoryAsync(userId, 30);
            TopUpHistory = await _walletService.GetTopUpHistoryAsync(userId, 10);
            MyVouchers = await _promotionService.GetMyVouchersAsync(userId);
            PromoRedemptions = await _promotionService.GetUserRedemptionsAsync(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostCancelTopUpAsync()
        {
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");
            await _walletService.CancelTopUpRequestAsync(userId);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUseVoucherAsync(string voucherCode)
        {
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var result = await _promotionService.UseVoucherAsync(userId, voucherCode);
            if (result.Success)
                SuccessMessage = result.Message;
            else
                ErrorMessage = result.Message;

            return RedirectToPage();
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
