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
        public List<SportHub.Services.Interfaces.DailyRevenue> DailyRevenue { get; set; } = new();
        public List<SportHub.Services.Interfaces.UnpaidRemainingFee> UnpaidFees { get; set; } = new();
        public List<(string Date, int Count)> DailyNewUsers { get; set; } = new();
        public Dictionary<string, int> MatchStatusCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int Days { get; set; } = 7;

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

            var days = Math.Clamp(Days, 7, 30);
            DailyRevenue = await _matchPaymentService.GetDailyRevenueAsync(days);
            UnpaidFees   = await _matchPaymentService.GetUnpaidRemainingFeesAsync();

            // New users per day
            var cutoff = DateTime.UtcNow.Date.AddDays(-days + 1);
            var newUsersByDay = await _context.Users
                .Where(u => u.CreatedAt >= cutoff)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();
            var dateRange = Enumerable.Range(0, days)
                .Select(i => DateTime.UtcNow.Date.AddDays(-days + 1 + i));
            DailyNewUsers = dateRange
                .Select(d => (d.ToString("dd/MM"), newUsersByDay.FirstOrDefault(x => x.Date == d)?.Count ?? 0))
                .ToList();

            // Match status breakdown
            MatchStatusCounts = await _context.Matches
                .GroupBy(m => m.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Extra KPIs
            Stats.TodayNewUsers       = await _context.Users.CountAsync(u => u.CreatedAt.Date == DateTime.UtcNow.Date);
            Stats.ActiveSubscriptions = await _context.SubscriptionOrders.CountAsync(o => o.Status == "Active" && o.ExpiresAt > DateTime.UtcNow);
            Stats.PendingDisputeCount = await _context.MatchDisputes.CountAsync(d => d.Status == "Pending" || d.Status == "UnderReview");

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
            public int TodayNewUsers { get; set; }
            public int ActiveSubscriptions { get; set; }
            public int PendingDisputeCount { get; set; }
            public List<SportHub.Services.Interfaces.TransactionHistoryItem> RecentTransactions { get; set; } = new();
        }
    }
}
