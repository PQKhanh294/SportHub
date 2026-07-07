using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Security;

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
        public bool NotifyWalletCredit { get; set; } = true;
        public bool NotifyPromoCode { get; set; } = true;
        public bool NotifyMatchApproved { get; set; } = true;
        public bool NotifyMatchJoinRequest { get; set; } = true;
        public bool NotifyMatchCancelled { get; set; } = true;
        public bool NotifyMatchReminder { get; set; } = true;
        public bool NotifyPaymentReminder { get; set; } = true;
        public bool NotifyDailyDigest { get; set; } = true;
        public bool ShowContactToTeammates { get; set; } = true;
        public bool HasPassword { get; set; }

        [BindProperty]
        public ChangePasswordInput PasswordInput { get; set; } = new();

        [TempData] public string? PasswordSuccess { get; set; }
        [TempData] public string? PasswordError { get; set; }

        public class ChangePasswordInput
        {
            [Required(ErrorMessage = "Nhập mật khẩu hiện tại.")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nhập mật khẩu mới.")]
            [MinLength(6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự.")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Xác nhận mật khẩu mới.")]
            [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

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
                    var user = await _context.Users
                        .Where(u => u.UserID == userId)
                        .Select(u => new {
                            u.NotifyByEmail, u.NotifyWalletCredit, u.NotifyPromoCode, u.NotifyMatchApproved,
                            u.NotifyMatchJoinRequest, u.NotifyMatchCancelled, u.NotifyMatchReminder,
                            u.NotifyPaymentReminder, u.NotifyDailyDigest,
                            u.ShowContactToTeammates, u.PasswordHash
                        })
                        .FirstOrDefaultAsync();
                    if (user != null)
                    {
                        NotifyByEmail = user.NotifyByEmail;
                        NotifyWalletCredit = user.NotifyWalletCredit;
                        NotifyPromoCode = user.NotifyPromoCode;
                        NotifyMatchApproved = user.NotifyMatchApproved;
                        NotifyMatchJoinRequest = user.NotifyMatchJoinRequest;
                        NotifyMatchCancelled = user.NotifyMatchCancelled;
                        NotifyMatchReminder = user.NotifyMatchReminder;
                        NotifyPaymentReminder = user.NotifyPaymentReminder;
                        NotifyDailyDigest = user.NotifyDailyDigest;
                        ShowContactToTeammates = user.ShowContactToTeammates;
                        HasPassword = !string.IsNullOrEmpty(user.PasswordHash);
                    }
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

        public async Task<IActionResult> OnPostSetEmailPreferencesAsync(bool notifyByEmail, bool notifyWalletCredit,
            bool notifyPromoCode, bool notifyMatchApproved, bool notifyMatchJoinRequest, bool notifyMatchCancelled,
            bool notifyMatchReminder, bool notifyPaymentReminder, bool notifyDailyDigest)
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.NotifyByEmail = notifyByEmail;
                    user.NotifyWalletCredit = notifyWalletCredit;
                    user.NotifyPromoCode = notifyPromoCode;
                    user.NotifyMatchApproved = notifyMatchApproved;
                    user.NotifyMatchJoinRequest = notifyMatchJoinRequest;
                    user.NotifyMatchCancelled = notifyMatchCancelled;
                    user.NotifyMatchReminder = notifyMatchReminder;
                    user.NotifyPaymentReminder = notifyPaymentReminder;
                    user.NotifyDailyDigest = notifyDailyDigest;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetContactPrivacyAsync(bool showContactToTeammates)
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.ShowContactToTeammates = showContactToTeammates;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            if (!ModelState.IsValid)
            {
                PasswordError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Thông tin không hợp lệ.";
                return RedirectToPage();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                PasswordError = "Tài khoản này không hỗ trợ đổi mật khẩu.";
                return RedirectToPage();
            }

            if (user.PasswordHash != PasswordHasher.Hash(PasswordInput.CurrentPassword))
            {
                PasswordError = "Mật khẩu hiện tại không đúng.";
                return RedirectToPage();
            }

            user.PasswordHash = PasswordHasher.Hash(PasswordInput.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            PasswordSuccess = "Đã đổi mật khẩu thành công.";
            return RedirectToPage();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
