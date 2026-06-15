using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly IBadgeService _badgeService;

        public IndexModel(IUserService userService, ApplicationDbContext context, IBadgeService badgeService)
        {
            _userService = userService;
            _context = context;
            _badgeService = badgeService;
        }

        public ProfileViewModel Profile { get; set; } = new();
        public List<UserSportProfileItem> SportProfiles { get; set; } = new();
        public List<BadgeProgressItem> Badges { get; set; } = new();

        public class UserSportProfileItem
        {
            public string SportName { get; set; } = string.Empty;
            public string SkillLevel { get; set; } = string.Empty;
        }

        private static string GetSkillDisplay(string? skillRequired)
        {
            if (string.IsNullOrWhiteSpace(skillRequired)) return "Mọi trình độ";
            return skillRequired.Trim().ToLower() switch
            {
                "any" => "Mọi trình độ",
                "beginner" => "Người mới (Beginner)",
                "intermediate" => "Trung bình (Intermediate)",
                "advanced" => "Nâng cao (Advanced)",
                "professional" => "Chuyên nghiệp (Professional)",
                _ => skillRequired
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Profile";

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            var isEnglish = CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);
            var notUpdatedText = isEnglish ? "Not updated" : "Chưa cập nhật";

            Profile = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? notUpdatedText : user.PhoneNumber,
                DefaultAddress = string.IsNullOrWhiteSpace(user.DefaultAddress) ? notUpdatedText : user.DefaultAddress,
                AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl)
                    ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(user.FullName)}&background=E2E8F0&color=1E293B"
                    : user.AvatarUrl,
                JoinedText = user.CreatedAt.ToString("MM/yyyy"),
                MatchesPlayed = await _userService.GetTotalMatchesPlayedAsync(userId),
                Wins = await _userService.GetTotalWinsAsync(userId),
                TotalBookings = await _userService.GetTotalBookingsAsync(userId)
            };

            SportProfiles = await _context.UserSportProfiles
                .Where(usp => usp.UserID == userId)
                .Include(usp => usp.Sport)
                .Select(usp => new UserSportProfileItem
                {
                    SportName = usp.Sport.SportName,
                    SkillLevel = GetSkillDisplay(usp.SkillLevel)
                })
                .ToListAsync();

            await _badgeService.SyncEarnedBadgesAsync(userId);
            Badges = await _badgeService.GetBadgeProgressAsync(userId);

            return Page();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        public class ProfileViewModel
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string DefaultAddress { get; set; } = string.Empty;
            public string AvatarUrl { get; set; } = string.Empty;
            public string JoinedText { get; set; } = string.Empty;
            public int MatchesPlayed { get; set; }
            public int Wins { get; set; }
            public int TotalBookings { get; set; }
        }
    }
}
