using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;

        public IndexModel(IUserService userService)
        {
            _userService = userService;
        }

        public ProfileViewModel Profile { get; set; } = new();

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

            Profile = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? "Not updated" : user.PhoneNumber,
                AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl)
                    ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(user.FullName)}&background=E2E8F0&color=1E293B"
                    : user.AvatarUrl,
                JoinedText = user.CreatedAt.ToString("MM/yyyy"),
                MatchesPlayed = await _userService.GetTotalMatchesPlayedAsync(userId),
                Wins = await _userService.GetTotalWinsAsync(userId),
                TotalBookings = await _userService.GetTotalBookingsAsync(userId)
            };

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
            public string AvatarUrl { get; set; } = string.Empty;
            public string JoinedText { get; set; } = string.Empty;
            public int MatchesPlayed { get; set; }
            public int Wins { get; set; }
            public int TotalBookings { get; set; }
        }
    }
}
