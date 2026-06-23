using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;
using SportHub.Models.Entities;

namespace SportHub.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IPromotionService _promotionService;

        public LoginModel(IUserService userService, IPromotionService promotionService)
        {
            _userService = userService;
            _promotionService = promotionService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your email.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please enter your password.")]
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public IActionResult OnGetExternalLogin(string provider = "Google", string? returnUrl = null)
        {
            var callbackUrl = Url.Page("/Auth/ExternalCallback", new { returnUrl });
            var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = callbackUrl
            };
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim();
            var isValid = await _userService.ValidateCredentialsAsync(email, Input.Password);
            if (!isValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Account does not exist.");
                return Page();
            }

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
            catch { /* non-critical, không break login */ }

            TempData["SuccessMessage"] = "Logged in successfully.";

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }
    }
}
