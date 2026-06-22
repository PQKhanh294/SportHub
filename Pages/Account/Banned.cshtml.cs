using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Account
{
    public class BannedModel : PageModel
    {
        private readonly IUserBanService _banService;

        public BannedModel(IUserBanService banService)
        {
            _banService = banService;
        }

        public UserBan? Ban { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId)) return RedirectToPage("/Index");

            Ban = await _banService.GetActiveBanAsync(userId);
            // If not actually banned, redirect home
            if (Ban == null || !Ban.IsActive || (Ban.EndAt.HasValue && Ban.EndAt.Value <= DateTime.UtcNow))
                return RedirectToPage("/Index");

            return Page();
        }
    }
}
