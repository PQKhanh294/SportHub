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

        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterRole { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        public const int PageSize = 30;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "Users";
            if (!await IsAdminAsync()) return Forbid();

            var query = _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
                query = query.Where(u => u.FullName.Contains(Search) || u.Email.Contains(Search));

            if (!string.IsNullOrWhiteSpace(FilterRole))
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == FilterRole));

            if (FilterStatus == "active")
                query = query.Where(u => u.IsActive);
            else if (FilterStatus == "banned")
                query = query.Where(u => !u.IsActive);

            TotalCount = await query.CountAsync();
            Users = await query.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();

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
            SuccessMessage = $"Đã cộng {amount:N0} xu vào ví người dùng #{userId}.";
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
