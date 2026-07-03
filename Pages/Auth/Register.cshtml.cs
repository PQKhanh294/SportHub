using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using SportHub.Services.Security;

namespace SportHub.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IPromotionService _promotionService;

        public RegisterModel(IUserService userService, IPromotionService promotionService)
        {
            _userService = userService;
            _promotionService = promotionService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your full name.")]
            [StringLength(100)]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please enter your email.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please enter your password.")]
            [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your password.")]
            [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var normalizedEmail = Input.Email.Trim().ToLower();
            var existed = await _userService.GetUserByEmailAsync(normalizedEmail);
            if (existed != null)
            {
                ModelState.AddModelError("Input.Email", "Email is already in use.");
                return Page();
            }

            var user = new User
            {
                Email = normalizedEmail,
                FullName = Input.FullName.Trim(),
                PasswordHash = PasswordHasher.Hash(Input.Password),
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            User createdUser;
            try
            {
                createdUser = await _userService.CreateUserAsync(user);
            }
            catch
            {
                ModelState.AddModelError("Input.Email", "Email is already in use.");
                return Page();
            }

            // Auto-login after registration
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, createdUser.UserID.ToString()),
                new(ClaimTypes.Name, createdUser.FullName),
                new(ClaimTypes.Email, createdUser.Email)
            };
            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Đồng bộ với luồng Login: đếm lượt đăng nhập + promo chào mừng
            await _userService.IncrementLoginCountAsync(createdUser.UserID);
            try
            {
                await _promotionService.TriggerFirstLoginAsync(createdUser.UserID);
            }
            catch { /* non-critical, không break đăng ký */ }

            return RedirectToPage("/Onboarding/Index");
        }
    }
}
