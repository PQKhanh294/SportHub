using System.Globalization;
using System.Security.Claims;
using SportHub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly IBadgeService _badgeService;
        private readonly IMatchReviewService _reviewService;
        private readonly IWalletService _walletService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPromotionService _promotionService;

        public IndexModel(IUserService userService, ApplicationDbContext context, IBadgeService badgeService, IMatchReviewService reviewService, IWalletService walletService, ISubscriptionService subscriptionService, IPromotionService promotionService)
        {
            _userService = userService;
            _context = context;
            _badgeService = badgeService;
            _reviewService = reviewService;
            _walletService = walletService;
            _subscriptionService = subscriptionService;
            _promotionService = promotionService;
        }

        public ProfileViewModel Profile { get; set; } = new();
        public List<UserSportProfileItem> SportProfiles { get; set; } = new();
        public List<BadgeProgressItem> Badges { get; set; } = new();
        public UserRatingSummary HostRatingSummary { get; set; } = new();
        public UserRatingSummary PlayerRatingSummary { get; set; } = new();
        public List<MatchReviewHistoryItem> ReceivedReviews { get; set; } = new();
        public decimal WalletBalance { get; set; }
        public List<SportHub.Models.Entities.WalletTransaction> WalletHistory { get; set; } = new();
        public string? CurrentPlanKey { get; set; }
        public List<SavedCodeWithDetailsDto> SavedCodes { get; set; } = new();
        public List<SportHub.Models.Entities.UserVoucher> MyVouchers { get; set; } = new();

        public class UserSportProfileItem
        {
            public string SportName { get; set; } = string.Empty;
            public string SkillLevel { get; set; } = string.Empty;
        }

        private static string GetSkillDisplay(string? skillRequired)
        {
            if (string.IsNullOrWhiteSpace(skillRequired)) return "Mọi trình độ";
            return skillRequired.Trim().ToLower() switch
            {
                "any" => "Mọi trình độ",
                "beginner" => "Người mới (Beginner)",
                "intermediate" => "Trung bình (Intermediate)",
                "advanced" => "Nâng cao (Advanced)",
                "professional" => "Chuyên nghiệp (Professional)",
                _ => skillRequired
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Profile";

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Auth/Login");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return RedirectToPage("/Auth/Login");
            }

            var isEnglish = CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase);
            var notUpdatedText = isEnglish ? "Not updated" : "Chưa cập nhật";

            Profile = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? notUpdatedText : user.PhoneNumber,
                DefaultAddress = string.IsNullOrWhiteSpace(user.DefaultAddress) ? notUpdatedText : user.DefaultAddress,
                AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl)
                    ? "/images/avatar-default.png"
                    : user.AvatarUrl,
                JoinedText = user.CreatedAt.ToString("MM/yyyy"),
                MatchesPlayed = await _userService.GetTotalMatchesPlayedAsync(userId),
                Wins = await _userService.GetTotalWinsAsync(userId),
                TotalBookings = await _userService.GetTotalBookingsAsync(userId),
                EmailConfirmed = user.EmailConfirmed
            };

            SportProfiles = await _context.UserSportProfiles
                .Where(usp => usp.UserID == userId)
                .Include(usp => usp.Sport)
                .Select(usp => new UserSportProfileItem
                {
                    SportName = usp.Sport.SportName,
                    SkillLevel = GetSkillDisplay(usp.SkillLevel)
                })
                .ToListAsync();

            await _badgeService.SyncEarnedBadgesAsync(userId);
            Badges = await _badgeService.GetBadgeProgressAsync(userId);

            HostRatingSummary = await _reviewService.GetHostRatingSummaryAsync(userId);
            PlayerRatingSummary = await _reviewService.GetPlayerRatingSummaryAsync(userId);
            ReceivedReviews = await _reviewService.GetReceivedReviewsAsync(userId);

            WalletBalance = await _walletService.GetBalanceAsync(userId);
            WalletHistory = await _walletService.GetHistoryAsync(userId, 10);

            var activeSub = await _subscriptionService.GetActiveSubscriptionAsync(userId);
            CurrentPlanKey = activeSub?.PlanKey ?? "Free";

            SavedCodes = await _promotionService.GetSavedCodesAsync(userId);
            MyVouchers = await _promotionService.GetMyVouchersAsync(userId);

            return Page();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        public class ProfileViewModel
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string DefaultAddress { get; set; } = string.Empty;
            public string AvatarUrl { get; set; } = string.Empty;
            public string JoinedText { get; set; } = string.Empty;
            public int MatchesPlayed { get; set; }
            public int Wins { get; set; }
            public int TotalBookings { get; set; }
            public bool EmailConfirmed { get; set; }
        }
    }
}
