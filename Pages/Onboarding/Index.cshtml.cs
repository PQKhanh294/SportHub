using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;

namespace SportHub.Pages.Onboarding
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Sport> Sports { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Sports = await _context.Sports.OrderBy(s => s.SportName).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            [FromForm] List<int> sportIds,
            [FromForm] List<string> skillLevels,
            [FromForm] string? defaultAddress)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            // Save sport skills
            if (sportIds.Any())
            {
                var existing = await _context.UserSportProfiles
                    .Where(p => p.UserID == userId)
                    .ToListAsync();
                _context.UserSportProfiles.RemoveRange(existing);

                for (int i = 0; i < sportIds.Count; i++)
                {
                    if (sportIds[i] <= 0) continue;
                    _context.UserSportProfiles.Add(new UserSportProfile
                    {
                        UserID     = userId,
                        SportID    = sportIds[i],
                        SkillLevel = i < skillLevels.Count ? skillLevels[i] : "Any",
                        UpdatedAt  = DateTime.UtcNow
                    });
                }

                // Sync primary fields
                var firstSport = await _context.Sports.FirstOrDefaultAsync(s => s.SportID == sportIds[0]);
                var user = await _context.Users.FindAsync(userId);
                if (user != null && firstSport != null)
                {
                    user.FavoriteSport = firstSport.SportName;
                    user.SkillLevel    = skillLevels.FirstOrDefault() ?? "Any";
                    if (!string.IsNullOrWhiteSpace(defaultAddress))
                        user.DefaultAddress = defaultAddress.Trim();
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (!string.IsNullOrWhiteSpace(defaultAddress))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.DefaultAddress = defaultAddress.Trim();
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Chào mừng đến SportHub! Hãy khám phá các trận đấu gần bạn.";
            return RedirectToPage("/Matchmaking/Index");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
