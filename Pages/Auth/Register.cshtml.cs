using System.ComponentModel.DataAnnotations;
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
        private readonly IEmailService _emailService;

        public RegisterModel(IUserService userService, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
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

            // Chưa đăng nhập ngay — bắt xác thực email bằng mã OTP trước
            var code = await _userService.GenerateEmailVerificationCodeAsync(createdUser.UserID);
            if (code != null)
            {
                await _emailService.SendVerificationCodeAsync(createdUser.Email, createdUser.FullName, code);
            }

            return RedirectToPage("/Auth/VerifyEmail", new { email = createdUser.Email, purpose = "register" });
        }
    }
}
