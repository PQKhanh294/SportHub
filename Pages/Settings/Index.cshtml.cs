using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;

namespace SportHub.Pages.Settings
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string CurrentTheme { get; set; } = "light";
        public string CurrentLanguage { get; set; } = "vi-VN";
        public bool IsAuthenticated { get; set; }
        public bool NotifyByEmail { get; set; } = true;

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Settings";

            // Read theme from cookie
            CurrentTheme = Request.Cookies["theme"] ?? "light";

            // Read language from RequestLocalization feature
            var feature = HttpContext.Features.Get<IRequestCultureFeature>();
            CurrentLanguage = feature?.RequestCulture.Culture.Name ?? "vi-VN";

            IsAuthenticated = User.Identity?.IsAuthenticated == true;
            if (IsAuthenticated)
            {
                var userId = GetCurrentUserId();
                if (userId > 0)
                {
                    NotifyByEmail = await _context.Users
                        .Where(u => u.UserID == userId)
                        .Select(u => u.NotifyByEmail)
                        .FirstOrDefaultAsync();
                }
            }
        }

        private static readonly HashSet<string> ValidThemes = new() { "light", "dark", "dark-2", "dark-3" };

        public IActionResult OnPostSetTheme(string theme)
        {
            if (!ValidThemes.Contains(theme)) theme = "light";
            Response.Cookies.Append("theme", theme, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/"
            });
            return RedirectToPage();
        }

        public IActionResult OnPostSetLanguage(string culture)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" }
            );
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetNotifyAsync(bool notifyByEmail)
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.NotifyByEmail = notifyByEmail;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
