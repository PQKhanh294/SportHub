using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IMatchReviewService
    {
        Task<bool> CanSubmitPlayerReviewAsync(int matchId, int reviewerUserId);
        Task<bool> CanSubmitHostReviewAsync(int matchId, int hostUserId, int reviewedPlayerId);
        Task<MatchReview?> SubmitPlayerReviewAsync(int matchId, int reviewerUserId, PlayerReviewInput input);
        Task<MatchReview?> SubmitHostReviewAsync(int matchId, int hostUserId, int reviewedPlayerId, HostReviewInput input);
        Task<UserRatingSummary> GetHostRatingSummaryAsync(int userId);
        Task<UserRatingSummary> GetPlayerRatingSummaryAsync(int userId);
        Task<List<MatchReviewHistoryItem>> GetReceivedReviewsAsync(int userId);
        Task<List<MatchReviewHistoryItem>> GetAllReviewsAsync();
        Task<bool> SetVisibilityAsync(int reviewId, bool isVisible);
        Task<List<PendingReviewItem>> GetPendingReviewsForUserAsync(int userId);
        Task<List<PlayerToRateItem>> GetPlayersToRateAsync(int matchId, int hostUserId);
        Task<Dictionary<int, (decimal Avg, int Count)>> GetBatchHostRatingsAsync(IEnumerable<int> hostIds);
    }

    public class PlayerReviewInput
    {
        public byte ScoreOrganization { get; set; }
        public byte ScoreEquipment { get; set; }
        public byte ScoreAtmosphere { get; set; }
        public byte ScoreHost { get; set; }
        public byte? ScoreValueForMoney { get; set; }
        public string? Comment { get; set; }
    }

    public class HostReviewInput
    {
        public byte ScorePunctuality { get; set; }
        public byte ScoreSportsmanship { get; set; }
        public byte ScoreSkillAccuracy { get; set; }
        public string? Comment { get; set; }
    }

    public class UserRatingSummary
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public decimal OverallAverage { get; set; }
        public int ReviewCount { get; set; }
        public bool HasEnoughReviews => ReviewCount >= 1;

        // For host (PlayerToMatch)
        public decimal? AvgOrganization { get; set; }
        public decimal? AvgEquipment { get; set; }
        public decimal? AvgAtmosphere { get; set; }
        public decimal? AvgHost { get; set; }
        public decimal? AvgValueForMoney { get; set; }

        // For player (HostToPlayer)
        public decimal? AvgPunctuality { get; set; }
        public decimal? AvgSportsmanship { get; set; }
        public decimal? AvgSkillAccuracy { get; set; }

        public string StarDisplay => HasEnoughReviews ? $"{OverallAverage:F1}" : "-";
    }

    public class MatchReviewHistoryItem
    {
        public int ReviewId { get; set; }
        public int MatchId { get; set; }
        public string MatchTitle { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }
        public string ReviewType { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string? ReviewerAvatarUrl { get; set; }
        public string ReviewedName { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public string? Comment { get; set; }
        public bool IsVisible { get; set; }
        public DateTime CreatedAt { get; set; }

        // Criteria (PlayerToMatch)
        public byte? ScoreOrganization { get; set; }
        public byte? ScoreEquipment { get; set; }
        public byte? ScoreAtmosphere { get; set; }
        public byte? ScoreHost { get; set; }
        public byte? ScoreValueForMoney { get; set; }

        // Criteria (HostToPlayer)
        public byte? ScorePunctuality { get; set; }
        public byte? ScoreSportsmanship { get; set; }
        public byte? ScoreSkillAccuracy { get; set; }
    }

    public class PendingReviewItem
    {
        public int MatchId { get; set; }
        public string MatchTitle { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }
        public bool CanRateAsPlayer { get; set; }
        public bool CanRateAsHost { get; set; }
        public List<PlayerToRateItem> PlayersToRate { get; set; } = new();
    }

    public class PlayerToRateItem
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool AlreadyRated { get; set; }
    }
}
