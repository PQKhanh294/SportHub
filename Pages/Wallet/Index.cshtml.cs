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

        public IndexModel(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public decimal Balance { get; set; }
        public List<WalletTransaction> Transactions { get; set; } = new();
        public List<WalletTopUpRequest> TopUpHistory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Wallet";
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            Balance = await _walletService.GetBalanceAsync(userId);
            Transactions = await _walletService.GetHistoryAsync(userId, 30);
            TopUpHistory = await _walletService.GetTopUpHistoryAsync(userId, 10);
            return Page();
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
