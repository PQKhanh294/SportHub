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
    public class TransactionsModel : PageModel
    {
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly ApplicationDbContext _context;

        public TransactionsModel(IMatchPaymentService matchPaymentService, ApplicationDbContext context)
        {
            _matchPaymentService = matchPaymentService;
            _context = context;
        }

        public List<TransactionHistoryItem> Transactions { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal TodayRevenue { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "Transactions";
            if (!await IsAdminAsync()) return Forbid();

            var all = await _matchPaymentService.GetTransactionHistoryAsync();

            Transactions = string.IsNullOrWhiteSpace(FilterType)
                ? all
                : all.Where(t => t.PaymentType == FilterType).ToList();

            var now = DateTime.UtcNow;
            TotalRevenue     = all.Sum(t => t.Amount);
            ThisMonthRevenue = all.Where(t => t.ConfirmedAt >= new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)).Sum(t => t.Amount);
            TodayRevenue     = all.Where(t => t.ConfirmedAt >= now.Date).Sum(t => t.Amount);

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
    }
}
