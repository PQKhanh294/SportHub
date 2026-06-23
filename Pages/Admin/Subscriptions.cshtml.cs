using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SubscriptionsModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ApplicationDbContext _context;

        public SubscriptionsModel(ISubscriptionService subscriptionService, ApplicationDbContext context)
        {
            _subscriptionService = subscriptionService;
            _context = context;
        }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public List<UserSubscription> ActiveSubscriptions { get; set; } = new();
        public List<SubscriptionOrder> RecentOrders { get; set; } = new();
        public List<UserMatchCredit> RecentCredits { get; set; } = new();
        public int TotalActive { get; set; }
        public Dictionary<string, int> PlanCounts { get; set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "Subscriptions";

            ActiveSubscriptions = await _subscriptionService.GetAllActiveSubscriptionsAsync();
            TotalActive = ActiveSubscriptions.Count;

            PlanCounts = ActiveSubscriptions
                .GroupBy(s => s.PlanKey)
                .ToDictionary(g => g.Key, g => g.Count());

            RecentOrders = await _context.SubscriptionOrders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .ToListAsync();

            RecentCredits = await _context.UserMatchCredits
                .Include(c => c.User)
                .OrderByDescending(c => c.PurchasedAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCancelAsync(int subscriptionId)
        {
            var sub = await _context.UserSubscriptions.FindAsync(subscriptionId);
            if (sub == null) return NotFound();

            sub.Status = "Cancelled";
            sub.EndAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã hủy gói đăng ký.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGrantAsync(int userId, string planKey, string billingCycle)
        {
            if (!new[] { "Starter", "Pro", "Club" }.Contains(planKey))
            {
                TempData["ErrorMessage"] = "Gói không hợp lệ.";
                return RedirectToPage();
            }
            await _subscriptionService.ActivateSubscriptionAsync(userId, planKey, billingCycle);
            TempData["SuccessMessage"] = $"Đã cấp gói {planKey} cho user {userId}.";
            return RedirectToPage();
        }
    }
}
