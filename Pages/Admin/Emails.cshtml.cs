using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    public class EmailsModel : PageModel
    {
        private readonly IUserService _userService;

        public EmailsModel(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["AdminPage"] = "Emails";
            if (!await IsAdminAsync()) return Forbid();

            return Page();
        }

        private async Task<bool> IsAdminAsync()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out var id)) return false;
            return await _userService.IsAdminAsync(id);
        }
    }
}
