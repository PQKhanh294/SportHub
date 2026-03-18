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
            [Required(ErrorMessage = "Vui lòng nh?p h? tên.")]
            [StringLength(100)]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nh?p email.")]
            [EmailAddress(ErrorMessage = "Email không h?p l?.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nh?p m?t kh?u.")]
            [MinLength(6, ErrorMessage = "M?t kh?u t?i thi?u 6 ký t?.")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nh?p l?i m?t kh?u.")]
            [Compare(nameof(Password), ErrorMessage = "M?t kh?u nh?p l?i không kh?p.")]
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
                ModelState.AddModelError("Input.Email", "Email ?ã ???c s? d?ng.");
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
                ModelState.AddModelError("Input.Email", "Email ?ã ???c s? d?ng.");
                return Page();
            }

            TempData["SuccessMessage"] = "??ng ký thành công. B?n có th? ??ng nh?p ngay.";
            return RedirectToPage("/Auth/Login");
        }
    }
}
