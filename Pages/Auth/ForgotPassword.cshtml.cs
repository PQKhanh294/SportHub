using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Auth
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public ForgotPasswordModel(ApplicationDbContext context, IEmailService emailService, IConfiguration config)
        {
            _context = context;
            _emailService = emailService;
            _config = config;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool SubmittedOk { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string Email { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Chỉ thật sự gửi mail nếu user tồn tại VÀ có mật khẩu (không phải tài khoản Google thuần).
            // Luôn hiện cùng 1 thông báo thành công để tránh lộ email nào đã đăng ký (chống dò email).
            if (user != null && !string.IsNullOrEmpty(user.PasswordHash))
            {
                var token = RandomNumberGenerator.GetHexString(48);
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                var baseUrl = (_config["App:BaseUrl"] ?? "https://sporthub-dn.id.vn").TrimEnd('/');
                var resetUrl = $"{baseUrl}/Auth/ResetPassword?token={Uri.EscapeDataString(token)}";
                await _emailService.SendPasswordResetAsync(user.Email, user.FullName, resetUrl);
            }

            SubmittedOk = true;
            return Page();
        }
    }
}
