using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Players
{
    public class ProfileModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IMatchReviewService _reviewService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ApplicationDbContext _context;

        public ProfileModel(IUserService userService, IMatchReviewService reviewService, ISubscriptionService subscriptionService, ApplicationDbContext context)
        {
            _userService = userService;
            _reviewService = reviewService;
            _subscriptionService = subscriptionService;
            _context = context;
        }

        public PublicProfileItem? Profile { get; set; }
        public UserRatingSummary HostRating { get; set; } = new();
        public UserRatingSummary PlayerRating { get; set; } = new();
        public List<MatchReviewHistoryItem> PublicReviews { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ViewData["ActivePage"] = "Matchmaking";

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null || !user.IsActive)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToPage("/Players/Index");
            }

            var currentUserId = GetCurrentUserId();
            var canSeePhone = currentUserId > 0 && await _subscriptionService.CanSeePhoneNumberAsync(currentUserId);
            var showContact = user.ShowContactToTeammates && canSeePhone;

            var totalMatches = await _context.MatchParticipants
                .CountAsync(p => p.UserID == id && p.JoinStatus == "Accepted");

            Profile = new PublicProfileItem
            {
                UserId = user.UserID,
                FullName = user.FullName,
                AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) ? "/images/avatar-default.png" : user.AvatarUrl,
                SkillLevel = string.IsNullOrWhiteSpace(user.SkillLevel) ? "Chưa cập nhật" : user.SkillLevel,
                FavoriteSport = user.FavoriteSport,
                DefaultAddress = user.DefaultAddress,
                TotalMatches = totalMatches,
                Phone = showContact ? user.PhoneNumber : null,
                ZaloContact = showContact ? user.ZaloContact : null,
                IsSelf = currentUserId == id
            };

            HostRating = await _reviewService.GetHostRatingSummaryAsync(id);
            PlayerRating = await _reviewService.GetPlayerRatingSummaryAsync(id);
            PublicReviews = (await _reviewService.GetReceivedReviewsAsync(id))
                .Where(r => r.IsVisible)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToList();

            return Page();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var value) ? value : 0;
        }

        public class PublicProfileItem
        {
            public int UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string AvatarUrl { get; set; } = string.Empty;
            public string SkillLevel { get; set; } = string.Empty;
            public string? FavoriteSport { get; set; }
            public string? DefaultAddress { get; set; }
            public int TotalMatches { get; set; }
            public string? Phone { get; set; }
            public string? ZaloContact { get; set; }
            public bool IsSelf { get; set; }
        }
    }
}
