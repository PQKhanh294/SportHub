using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Profile
{
    [Authorize]
    public class ReviewsModel : PageModel
    {
        private readonly IMatchReviewService _reviewService;

        public ReviewsModel(IMatchReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        public UserRatingSummary HostRatingSummary { get; set; } = new();
        public UserRatingSummary PlayerRatingSummary { get; set; } = new();
        public List<MatchReviewHistoryItem> ReceivedReviews { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Profile";

            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            HostRatingSummary = await _reviewService.GetHostRatingSummaryAsync(userId);
            PlayerRatingSummary = await _reviewService.GetPlayerRatingSummaryAsync(userId);
            ReceivedReviews = await _reviewService.GetReceivedReviewsAsync(userId);

            return Page();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
