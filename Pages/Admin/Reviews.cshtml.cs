using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Admin
{
    [Authorize]
    public class ReviewsModel : PageModel
    {
        private readonly IMatchReviewService _reviewService;
        private readonly ApplicationDbContext _context;

        public ReviewsModel(IMatchReviewService reviewService, ApplicationDbContext context)
        {
            _reviewService = reviewService;
            _context = context;
        }

        public List<MatchReviewHistoryItem> Reviews { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string? FilterType { get; set; }
        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Admin";
            ViewData["AdminPage"] = "Reviews";
            if (!await IsAdminAsync()) return Forbid();

            Reviews = await _reviewService.GetAllReviewsAsync();
            if (!string.IsNullOrWhiteSpace(FilterType))
                Reviews = Reviews.Where(r => r.ReviewType == FilterType).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostToggleVisibilityAsync(int reviewId, bool isVisible)
        {
            if (!await IsAdminAsync()) return Forbid();
            var ok = await _reviewService.SetVisibilityAsync(reviewId, isVisible);
            SuccessMessage = ok ? (isVisible ? "Đã hiện đánh giá." : "Đã ẩn đánh giá.") : "Không thể thay đổi.";
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
