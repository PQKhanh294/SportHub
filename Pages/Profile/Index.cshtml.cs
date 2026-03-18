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
                PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? "Ch?a c?p nh?t" : user.PhoneNumber,
                AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl)
                    ? "https://lh3.googleusercontent.com/aida-public/AB6AXuBrQ1D_hZ1VuQlkRROfTOHkc9hceEK3EfoWqmk4eiEz22D_DD3lCtPUdc3L21Lribw0r6zMsEenoDs-sGtkHqw9vnsr50rzmE2CN9NyHwL10CYrzAc8nBVTb7edW4ngMGhtnCcf0Y2ai0H56_EW36OyywtcCBiir2wik_0srGdgIVd6vhiR-dUslF4dx5uVwNgjidfWVfzJxZSAAoYyEREvOCh6fm9jrTPtnCcMpUMFr0LDWI79fOpa-8FFDlU2G6BL075uL4Rbg5A"
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
