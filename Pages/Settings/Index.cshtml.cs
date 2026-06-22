using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SportHub.Pages.Settings
{
    public class IndexModel : PageModel
    {
        public string CurrentTheme { get; set; } = "light";
        public string CurrentLanguage { get; set; } = "vi-VN";

        public void OnGet()
        {
            ViewData["ActivePage"] = "Settings";
            
            // Read theme from cookie
            CurrentTheme = Request.Cookies["theme"] ?? "light";

            // Read language from RequestLocalization feature
            var feature = HttpContext.Features.Get<IRequestCultureFeature>();
            CurrentLanguage = feature?.RequestCulture.Culture.Name ?? "vi-VN";
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
    }
}
