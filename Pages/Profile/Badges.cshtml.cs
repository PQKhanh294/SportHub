using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Profile
{
    [Authorize]
    public class BadgesModel : PageModel
    {
        private readonly IBadgeService _badgeService;

        public BadgesModel(IBadgeService badgeService)
        {
            _badgeService = badgeService;
        }

        public List<BadgeProgressItem> Badges { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Profile";

            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            await _badgeService.SyncEarnedBadgesAsync(userId);
            Badges = await _badgeService.GetBadgeProgressAsync(userId);

            return Page();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
