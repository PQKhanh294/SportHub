using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    [Authorize]
    public class AdminMatchesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public AdminMatchesModel(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public List<Match> Matches { get; set; } = new();
        public List<Sport> Sports { get; set; } = new();
        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)] public string? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)] public int? FilterSport { get; set; }
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public const int PageSize = 25;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "Matches";
            if (!await IsAdminAsync()) return Forbid();

            var query = _context.Matches
                .Include(m => m.Sport)
                .Include(m => m.CreatedByUser)
                .Include(m => m.Participants)
                .OrderByDescending(m => m.CreatedAt)
                .AsQueryable();

            Sports = await _context.Sports.OrderBy(s => s.SportName).ToListAsync();

            if (!string.IsNullOrWhiteSpace(FilterStatus))
                query = query.Where(m => m.Status == FilterStatus);

            if (FilterSport.HasValue)
                query = query.Where(m => m.SportID == FilterSport.Value);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                // Try SQL Server FTS CONTAINS(); fall back to LIKE if FTS not installed
                bool ftsAvailable = false;
                try
                {
                    var ftsCheck = await _context.Database
                        .SqlQueryRaw<int>("SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') AS Value")
                        .FirstOrDefaultAsync();
                    ftsAvailable = ftsCheck == 1;
                }
                catch { }

                if (ftsAvailable)
                {
                    var ftsQuery = $"\"{Search}*\" OR \"{Search}\"";
                    query = query.Where(m =>
                        EF.Functions.Contains(m.Title!, ftsQuery) ||
                        EF.Functions.Contains(m.CreatedByUser.FullName, ftsQuery));
                }
                else
                {
                    query = query.Where(m => (m.Title != null && m.Title.Contains(Search))
                                          || m.CreatedByUser.FullName.Contains(Search));
                }
            }

            TotalCount = await query.CountAsync();
            Matches = await query.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int matchId)
        {
            if (!await IsAdminAsync()) return Forbid();

            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null)
            {
                ErrorMessage = "Không tìm thấy trận đấu.";
                return RedirectToPage();
            }

            var matchTitle = match.Title ?? match.MatchType;
            var notifyUsers = match.Participants
                .Where(p => p.UserID != match.CreatedByUserID)
                .Select(p => p.UserID).Distinct().ToList();

            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();

            foreach (var uid in notifyUsers)
            {
                await _notificationService.CreateAsync(uid, "System",
                    "Trận đấu bị xóa bởi admin",
                    $"Trận \"{matchTitle}\" đã bị admin xóa khỏi hệ thống.",
                    "/Matchmaking/Index");
            }

            SuccessMessage = $"Đã xóa trận \"{matchTitle}\".";
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
