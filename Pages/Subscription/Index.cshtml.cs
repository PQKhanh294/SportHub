using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Subscription
{
    public class IndexModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IWalletService _walletService;

        public IndexModel(ISubscriptionService subscriptionService, IWalletService walletService)
        {
            _subscriptionService = subscriptionService;
            _walletService = walletService;
        }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public List<SubscriptionPlan> Plans { get; set; } = new();
        public SubscriptionPlan? CurrentPlan { get; set; }
        public UserSubscription? ActiveSub { get; set; }
        public bool HasUsedTrial { get; set; }
        public int RemainingCredits { get; set; }
        public int MonthlyJoinCount { get; set; }
        public int MonthlyCreateCount { get; set; }
        public decimal WalletBalance { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Subscription";
            Plans = await _subscriptionService.GetAllPlansAsync();

            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                CurrentPlan = await _subscriptionService.GetCurrentPlanAsync(userId);
                ActiveSub = await _subscriptionService.GetActiveSubscriptionAsync(userId);
                HasUsedTrial = await _subscriptionService.HasUsedTrialAsync(userId);
                RemainingCredits = await _subscriptionService.GetRemainingCreditsAsync(userId);
                MonthlyJoinCount = await _subscriptionService.GetMonthlyJoinCountAsync(userId);
                MonthlyCreateCount = await _subscriptionService.GetMonthlyCreateCountAsync(userId);
                WalletBalance = await _walletService.GetBalanceAsync(userId);
            }
        }

        public async Task<IActionResult> OnPostTrialAsync()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var started = await _subscriptionService.StartTrialAsync(userId);
            TempData[started ? "SuccessMessage" : "ErrorMessage"] = started
                ? "Gói Pro dùng thử 7 ngày đã được kích hoạt!"
                : "Bạn đã sử dụng dùng thử rồi.";
            return RedirectToPage();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
