using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Auth
{
    public class VerifyEmailModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IPromotionService _promotionService;

        public VerifyEmailModel(IUserService userService, IEmailService emailService, IPromotionService promotionService)
        {
            _userService = userService;
            _emailService = emailService;
            _promotionService = promotionService;
        }

        [BindProperty(SupportsGet = true)]
        public string Email { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string Purpose { get; set; } = "login"; // "register" hoặc "login"

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool ResendSent { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập mã xác thực.")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác thực gồm 6 số.")]
            public string Code { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                return RedirectToPage("/Auth/Login");
            }

            // Truy cập trực tiếp trang này (vd. từ Profile) chưa chắc đã có mã gửi sẵn — gửi mã
            // nếu chưa trong thời gian chờ (60s); nếu Register/Login vừa gửi thì bị chặn cooldown, không gửi trùng.
            var user = await _userService.GetUserByEmailAsync(Email);
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }
            if (user.EmailConfirmed)
            {
                return RedirectToPage("/Index");
            }

            var code = await _userService.GenerateEmailVerificationCodeAsync(user.UserID);
            if (code != null)
            {
                await _emailService.SendVerificationCodeAsync(user.Email, user.FullName, code);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostVerifyAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var ok = await _userService.ConfirmEmailCodeAsync(Email, Input.Code.Trim());
            if (!ok)
            {
                ModelState.AddModelError("Input.Code", "Mã xác thực không đúng hoặc đã hết hạn.");
                return Page();
            }

            var user = await _userService.GetUserByEmailAsync(Email);
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

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

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            var prevLoginCount = user.LoginCount;
            await _userService.IncrementLoginCountAsync(user.UserID);

            try
            {
                if (prevLoginCount == 0)
                    await _promotionService.TriggerFirstLoginAsync(user.UserID);
                await _promotionService.TriggerBirthdayAsync(user.UserID);
            }
            catch { /* non-critical, không break đăng nhập */ }

            if (Purpose == "register")
            {
                return RedirectToPage("/Onboarding/Index");
            }

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            ModelState.Clear();

            var user = await _userService.GetUserByEmailAsync(Email);
            if (user != null)
            {
                var code = await _userService.GenerateEmailVerificationCodeAsync(user.UserID);
                if (code != null)
                {
                    await _emailService.SendVerificationCodeAsync(user.Email, user.FullName, code);
                    ResendSent = true;
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng đợi ít phút trước khi gửi lại mã.");
                }
            }

            return Page();
        }
    }
}
