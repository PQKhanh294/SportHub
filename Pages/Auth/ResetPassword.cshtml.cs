using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Security;

namespace SportHub.Pages.Auth
{
    public class ResetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ResetPasswordModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Token { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool TokenValid { get; set; }
        public bool ResetSuccess { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
            [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự.")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
            [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            TokenValid = await IsTokenValidAsync(Token);
            if (!TokenValid) return Page();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            TokenValid = await IsTokenValidAsync(Token);
            if (!TokenValid) return Page();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == Token);
            if (user == null)
            {
                TokenValid = false;
                return Page();
            }

            user.PasswordHash = PasswordHasher.Hash(Input.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            ResetSuccess = true;
            return Page();
        }

        private async Task<bool> IsTokenValidAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
            return user != null && user.PasswordResetTokenExpiresAt.HasValue && user.PasswordResetTokenExpiresAt.Value > DateTime.UtcNow;
        }
    }
}
