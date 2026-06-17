using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    [Authorize]
    public class AdminIndexModel : PageModel
    {
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly ApplicationDbContext _context;

        public AdminIndexModel(IMatchPaymentService matchPaymentService, ApplicationDbContext context)
        {
            _matchPaymentService = matchPaymentService;
            _context = context;
        }

        public AdminDashboardStats Stats { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "Dashboard";
            if (!await IsAdminAsync()) return Forbid();

            var revenue = await _matchPaymentService.GetRevenueStatsAsync();
            var recentTx = await _matchPaymentService.GetTransactionHistoryAsync();

            Stats = new AdminDashboardStats
            {
                TotalRevenue           = revenue.TotalRevenue,
                TodayRevenue           = revenue.TodayRevenue,
                ThisMonthRevenue       = revenue.ThisMonthRevenue,
                TotalConfirmedPayments = revenue.TotalConfirmedPayments,
                PendingPaymentsCount   = revenue.PendingPaymentsCount,
                TotalUsers             = await _context.Users.CountAsync(),
                ActiveMatchesCount     = await _context.Matches.CountAsync(m => m.Status == "Open" || m.Status == "Full"),
                RecentTransactions     = recentTx.Take(8).ToList()
            };

            return Page();
        }

        private async Task<bool> IsAdminAsync()
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return false;
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserID == userId && ur.Role.RoleName == "Admin");
        }

        public class AdminDashboardStats
        {
            public decimal TotalRevenue { get; set; }
            public decimal TodayRevenue { get; set; }
            public decimal ThisMonthRevenue { get; set; }
            public int TotalConfirmedPayments { get; set; }
            public int PendingPaymentsCount { get; set; }
            public int TotalUsers { get; set; }
            public int ActiveMatchesCount { get; set; }
            public List<SportHub.Services.Interfaces.TransactionHistoryItem> RecentTransactions { get; set; } = new();
        }
    }
}
