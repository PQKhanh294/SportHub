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
    public class ReportedMessagesModel : PageModel
    {
        private readonly IMessageReportService _reportService;
        private readonly IUserBanService _banService;
        private readonly ApplicationDbContext _context;

        public ReportedMessagesModel(IMessageReportService reportService, IUserBanService banService, ApplicationDbContext context)
        {
            _reportService = reportService;
            _banService = banService;
            _context = context;
        }

        public List<MessageReport> Reports { get; set; } = new();
        public int TotalPending { get; set; }
        public int TotalProcessed { get; set; }
        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string Tab { get; set; } = "pending";

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "ReportedMessages";
            if (!await IsAdminAsync()) return Forbid();

            TotalPending = await _reportService.GetPendingReportCountAsync();
            TotalProcessed = await _reportService.GetProcessedReportCountAsync();

            if (Tab == "processed")
                Reports = await _reportService.GetProcessedReportsAsync(Page, 20);
            else
                Reports = await _reportService.GetPendingReportsAsync(Page, 20);

            return Page();
        }

        public async Task<IActionResult> OnPostBanAsync(int reportId, int targetUserId, string banType, string reason)
        {
            if (!await IsAdminAsync()) return Forbid();
            var adminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

            var validTypes = new[] { "1day", "7days", "30days", "permanent" };
            if (!validTypes.Contains(banType))
            {
                ErrorMessage = "Loại ban không hợp lệ.";
                return RedirectToPage();
            }

            await _banService.BanUserAsync(targetUserId, adminId, banType, reason, reportId);
            await _reportService.ReviewReportAsync(reportId, adminId, "Reviewed", $"Đã ban người dùng: {banType}");
            SuccessMessage = "Đã ban người dùng và đánh dấu báo cáo là đã xử lý.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDismissAsync(int reportId)
        {
            if (!await IsAdminAsync()) return Forbid();
            var adminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

            await _reportService.ReviewReportAsync(reportId, adminId, "Dismissed", null);
            SuccessMessage = "Đã bỏ qua báo cáo.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetCountsAsync()
        {
            if (!await IsAdminAsync()) return Forbid();
            var count = await _reportService.GetPendingReportCountAsync();
            return new JsonResult(new { pendingReports = count });
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
