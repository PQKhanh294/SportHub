using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    [Authorize]
    public class AdminUsersModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly IWalletService _walletService;

        public AdminUsersModel(IUserService userService, ApplicationDbContext context, IWalletService walletService)
        {
            _userService = userService;
            _context = context;
            _walletService = walletService;
        }

        public List<User> Users { get; set; } = new();
        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "Users";
            if (!await IsAdminAsync()) return Forbid();

            var all = await _userService.GetAllUsersAsync();
            Users = string.IsNullOrWhiteSpace(Search)
                ? all
                : all.Where(u => u.FullName.Contains(Search, StringComparison.OrdinalIgnoreCase)
                              || u.Email.Contains(Search, StringComparison.OrdinalIgnoreCase)).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int userId, bool currentActive)
        {
            if (!await IsAdminAsync()) return Forbid();

            var currentAdminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId == currentAdminId)
            {
                ErrorMessage = "Không thể tự khóa tài khoản của mình.";
                return RedirectToPage();
            }

            var result = await _userService.SetUserActiveAsync(userId, !currentActive);
            SuccessMessage = result
                ? (!currentActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.")
                : "Không tìm thấy người dùng.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreditWalletAsync(int userId, decimal amount, string description)
        {
            if (!await IsAdminAsync()) return Forbid();
            if (amount <= 0)
            {
                ErrorMessage = "Số tiền phải lớn hơn 0.";
                return RedirectToPage();
            }
            await _walletService.CreditAsync(userId, amount, string.IsNullOrWhiteSpace(description) ? "Admin hoàn tiền thủ công" : description);
            SuccessMessage = $"Đã cộng {amount:N0} ₫ vào ví người dùng #{userId}.";
            return RedirectToPage();
        }

        private async Task<bool> IsAdminAsync()
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return false;
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserID == userId && ur.Role.RoleName == "Admin");
        }
    }
}
