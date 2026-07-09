using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Common;
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
            [FromForm] string? defaultAddress,
            [FromForm] decimal? defaultLatitude,
            [FromForm] decimal? defaultLongitude,
            [FromForm] string? phoneNumber)
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
                    ApplyContactFields(user, defaultAddress, defaultLatitude, defaultLongitude, phoneNumber);
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    ApplyContactFields(user, defaultAddress, defaultLatitude, defaultLongitude, phoneNumber);
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Chào mừng đến SportHub! Hãy khám phá các trận đấu gần bạn.";
            return RedirectToPage("/Matchmaking/Index");
        }

        private static void ApplyContactFields(User user, string? address, decimal? lat, decimal? lon, string? phone)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                user.DefaultAddress = address.Trim();
                if (lat is >= -90 and <= 90) user.DefaultLatitude = lat;
                if (lon is >= -180 and <= 180) user.DefaultLongitude = lon;
            }
            // Client-side đã chặn SĐT sai định dạng trước khi submit — đây chỉ là lớp phòng vệ
            // nếu request bị chỉnh sửa bỏ qua JS; bỏ qua giá trị SĐT lỗi thay vì chặn cả luồng onboarding.
            if (!string.IsNullOrWhiteSpace(phone) && PhoneValidator.IsValidVietnamesePhone(phone))
                user.PhoneNumber = phone.Trim();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
