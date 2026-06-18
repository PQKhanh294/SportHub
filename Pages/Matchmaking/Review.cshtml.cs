using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    [Authorize]
    public class ReviewModel : PageModel
    {
        private readonly IMatchReviewService _reviewService;
        private readonly ApplicationDbContext _context;

        public ReviewModel(IMatchReviewService reviewService, ApplicationDbContext context)
        {
            _reviewService = reviewService;
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public int MatchId { get; set; }
        // "player" = PlayerToMatch review; "host" = HostToPlayer review
        [BindProperty(SupportsGet = true)] public string? Mode { get; set; } = "player";
        // For host mode: which player to rate
        [BindProperty(SupportsGet = true)] public int? TargetUserId { get; set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public string MatchTitle { get; set; } = string.Empty;
        public string TargetUserName { get; set; } = string.Empty;
        public bool CanReview { get; set; }
        public List<PlayerToRateItem> PlayersToRate { get; set; } = new();

        [BindProperty] public PlayerReviewInputModel PlayerInput { get; set; } = new();
        [BindProperty] public HostReviewInputModel HostInput { get; set; } = new();

        public class PlayerReviewInputModel
        {
            public byte ScoreOrganization { get; set; } = 5;
            public byte ScoreEquipment { get; set; } = 5;
            public byte ScoreAtmosphere { get; set; } = 5;
            public byte ScoreHost { get; set; } = 5;
            public byte? ScoreValueForMoney { get; set; }
            [MaxLength(500)] public string? Comment { get; set; }
            public bool HasCost { get; set; }
        }

        public class HostReviewInputModel
        {
            public byte ScorePunctuality { get; set; } = 5;
            public byte ScoreSportsmanship { get; set; } = 5;
            public byte ScoreSkillAccuracy { get; set; } = 5;
            [MaxLength(500)] public string? Comment { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId == 0) return RedirectToPage("/Auth/Login");

            var match = await _context.Matches.FindAsync(MatchId);
            if (match == null) return NotFound();
            MatchTitle = match.Title ?? match.MatchType;

            Mode ??= "player";

            if (Mode == "player")
            {
                CanReview = await _reviewService.CanSubmitPlayerReviewAsync(MatchId, userId);
                var hostUser = await _context.Users.FindAsync(match.CreatedByUserID);
                TargetUserName = hostUser?.FullName ?? string.Empty;
                PlayerInput.HasCost = match.CustomPriceVnd.HasValue && match.CustomPriceVnd > 0;
            }
            else if (Mode == "host")
            {
                if (match.CreatedByUserID != userId) return Forbid();
                PlayersToRate = await _reviewService.GetPlayersToRateAsync(MatchId, userId);

                if (TargetUserId.HasValue)
                {
                    CanReview = await _reviewService.CanSubmitHostReviewAsync(MatchId, userId, TargetUserId.Value);
                    var targetUser = await _context.Users.FindAsync(TargetUserId.Value);
                    TargetUserName = targetUser?.FullName ?? string.Empty;
                }
                else
                {
                    CanReview = PlayersToRate.Any(p => !p.AlreadyRated);
                }
            }

            return Page();
        }

        private static byte ClampScore(byte v) => v is >= 1 and <= 5 ? v : (byte)5;

        public async Task<IActionResult> OnPostPlayerAsync()
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId == 0) return RedirectToPage("/Auth/Login");

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Dữ liệu đánh giá không hợp lệ. Vui lòng thử lại.";
                return RedirectToPage(new { matchId = MatchId, mode = "player" });
            }

            var input = new PlayerReviewInput
            {
                ScoreOrganization = ClampScore(PlayerInput.ScoreOrganization),
                ScoreEquipment = ClampScore(PlayerInput.ScoreEquipment),
                ScoreAtmosphere = ClampScore(PlayerInput.ScoreAtmosphere),
                ScoreHost = ClampScore(PlayerInput.ScoreHost),
                ScoreValueForMoney = PlayerInput.HasCost
                    ? (PlayerInput.ScoreValueForMoney is >= 1 and <= 5 ? PlayerInput.ScoreValueForMoney : (byte)5)
                    : null,
                Comment = PlayerInput.Comment
            };

            try
            {
                var review = await _reviewService.SubmitPlayerReviewAsync(MatchId, userId, input);
                if (review == null)
                {
                    ErrorMessage = await DiagnosePlayerReviewFailureAsync(MatchId, userId);
                    return RedirectToPage(new { matchId = MatchId, mode = "player" });
                }

                SuccessMessage = "Cảm ơn bạn đã đánh giá trận đấu!";
                return RedirectToPage("/Matchmaking/Details", new { id = MatchId });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Đã xảy ra lỗi khi lưu đánh giá: {ex.Message}";
                return RedirectToPage(new { matchId = MatchId, mode = "player" });
            }
        }

        private async Task<string> DiagnosePlayerReviewFailureAsync(int matchId, int userId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null) return "Trận đấu không tồn tại.";
            if (match.Status == "Cancelled") return "Trận đấu đã bị huỷ, không thể đánh giá.";

            var endUtc = match.MatchDate.Date + match.EndTime - TimeSpan.FromHours(7);
            var hasEnded = match.Status == "Completed" || DateTime.UtcNow >= endUtc;
            if (!hasEnded)
                return $"Trận chưa kết thúc (kết thúc lúc {endUtc.AddHours(7):dd/MM HH:mm} giờ VN, trạng thái: {match.Status}).";

            if (DateTime.UtcNow > endUtc.AddDays(7))
                return "Đã quá 7 ngày sau khi trận kết thúc.";

            if (match.CreatedByUserID == userId)
                return "Host không thể tự đánh giá trận của mình qua form người chơi.";

            var participant = match.Participants.FirstOrDefault(p => p.UserID == userId);
            if (participant == null)
                return "Bạn không có trong danh sách người tham gia trận này.";
            if (participant.JoinStatus != "Accepted")
                return $"Trạng thái tham gia của bạn là \"{participant.JoinStatus}\" (cần \"Accepted\" để đánh giá). Bạn cần hoàn tất thanh toán phí tham gia trước khi có thể đánh giá.";

            var alreadyReviewed = await _context.MatchReviews.AnyAsync(r =>
                r.MatchID == matchId && r.ReviewerUserID == userId && r.ReviewType == "PlayerToMatch");
            if (alreadyReviewed)
                return "Bạn đã đánh giá trận này rồi.";

            return "Không thể gửi đánh giá. Vui lòng thử lại sau.";
        }

        public async Task<IActionResult> OnPostHostAsync(int targetUserId)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId == 0) return RedirectToPage("/Auth/Login");

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Dữ liệu đánh giá không hợp lệ. Vui lòng thử lại.";
                return RedirectToPage(new { matchId = MatchId, mode = "host", targetUserId });
            }

            var input = new HostReviewInput
            {
                ScorePunctuality = ClampScore(HostInput.ScorePunctuality),
                ScoreSportsmanship = ClampScore(HostInput.ScoreSportsmanship),
                ScoreSkillAccuracy = ClampScore(HostInput.ScoreSkillAccuracy),
                Comment = HostInput.Comment
            };

            try
            {
                var review = await _reviewService.SubmitHostReviewAsync(MatchId, userId, targetUserId, input);
                if (review == null)
                {
                    ErrorMessage = "Không thể gửi đánh giá người chơi. Vui lòng kiểm tra lại điều kiện.";
                }
                else
                {
                    SuccessMessage = "Đã đánh giá người chơi thành công!";
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Đã xảy ra lỗi khi lưu đánh giá. Vui lòng thử lại sau.";
            }

            return RedirectToPage(new { matchId = MatchId, mode = "host" });
        }
    }
}
