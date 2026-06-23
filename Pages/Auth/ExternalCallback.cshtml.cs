using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;
using SportHub.Models.Entities;

namespace SportHub.Pages.Auth
{
    [AllowAnonymous]
    public class ExternalCallbackModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IPromotionService _promotionService;

        public ExternalCallbackModel(IUserService userService, IPromotionService promotionService)
        {
            _userService = userService;
            _promotionService = promotionService;
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync("ExternalCookie");
            if (!result.Succeeded)
                return RedirectToPage("/Auth/Login");

            var googleId = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email    = result.Principal?.FindFirstValue(ClaimTypes.Email);
            var fullName = result.Principal?.FindFirstValue(ClaimTypes.Name);
            var avatarUrl = result.Principal?.Claims
                .FirstOrDefault(c => c.Type.EndsWith("picture") || c.Type == "urn:google:picture")?.Value;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin từ Google. Vui lòng thử lại.";
                return RedirectToPage("/Auth/Login");
            }

            var user = await _userService.GetOrCreateGoogleUserAsync(googleId, email, fullName ?? email, avatarUrl);

            if (!user.IsActive)
            {
                TempData["ErrorMessage"] = "Tài khoản của bạn đã bị vô hiệu hóa.";
                return RedirectToPage("/Auth/Login");
            }

            if (user.IsBanned && (user.BanEndAt == null || user.BanEndAt > DateTime.UtcNow))
                return RedirectToPage("/Account/Banned");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email)
            };
            foreach (var ur in user.UserRoles)
                claims.Add(new Claim(ClaimTypes.Role, ur.Role.RoleName));

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            await HttpContext.SignOutAsync("ExternalCookie");
            var prevLoginCount = user.LoginCount;
            await _userService.IncrementLoginCountAsync(user.UserID);

            try
            {
                if (prevLoginCount == 0)
                    await _promotionService.TriggerFirstLoginAsync(user.UserID);
                await _promotionService.TriggerBirthdayAsync(user.UserID);
            }
            catch { /* non-critical, không break login */ }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToPage("/Index");
        }
    }
}
