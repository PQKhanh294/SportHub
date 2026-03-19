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

        public RegisterModel(IUserService userService)
        {
            _userService = userService;
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

            try
            {
                await _userService.CreateUserAsync(user);
            }
            catch
            {
                ModelState.AddModelError("Input.Email", "Email is already in use.");
                return Page();
            }

            TempData["SuccessMessage"] = "Registration successful. You can now log in.";
            return RedirectToPage("/Auth/Login");
        }
    }
}
