using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class MatchReviewService : IMatchReviewService
    {
        private readonly ApplicationDbContext _context;
        private const int ReviewWindowDays = 7;

        public MatchReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Match ended if MatchDate + EndTime <= now (UTC, Vietnam is UTC+7 so we add buffer)
        private static bool MatchHasEnded(Match match)
        {
            var endUtc = match.MatchDate.Date + match.EndTime - TimeSpan.FromHours(7);
            return DateTime.UtcNow >= endUtc;
        }

        private static bool WithinReviewWindow(Match match)
        {
            var endUtc = match.MatchDate.Date + match.EndTime - TimeSpan.FromHours(7);
            return DateTime.UtcNow <= endUtc.AddDays(ReviewWindowDays);
        }

        public async Task<bool> CanSubmitPlayerReviewAsync(int matchId, int reviewerUserId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);
            if (match == null || match.Status == "Cancelled") return false;
            var hasEnded = match.Status == "Completed" || MatchHasEnded(match);
            if (!hasEnded || !WithinReviewWindow(match)) return false;

            var isParticipant = match.Participants.Any(p =>
                p.UserID == reviewerUserId && p.JoinStatus == "Accepted");
            // Host can also write PlayerToMatch review if they want to review the match itself
            // Actually PlayerToMatch is player reviewing the match/host — so only non-host players
            var isHost = match.CreatedByUserID == reviewerUserId;
            if (!isParticipant && !isHost) return false;
            if (isHost) return false; // host doesn't review own match

            var alreadyReviewed = await _context.MatchReviews.AnyAsync(r =>
                r.MatchID == matchId &&
                r.ReviewerUserID == reviewerUserId &&
                r.ReviewType == "PlayerToMatch");
            return !alreadyReviewed;
        }

        public async Task<bool> CanSubmitHostReviewAsync(int matchId, int hostUserId, int reviewedPlayerId)
        {
            if (reviewedPlayerId == hostUserId) return false; // host cannot rate themselves
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);
            if (match == null || match.Status == "Cancelled") return false;
            if (match.CreatedByUserID != hostUserId) return false;
            var hasEnded = match.Status == "Completed" || MatchHasEnded(match);
            if (!hasEnded || !WithinReviewWindow(match)) return false;

            var playerParticipated = match.Participants.Any(p =>
                p.UserID == reviewedPlayerId && p.JoinStatus == "Accepted");
            if (!playerParticipated) return false;

            var alreadyReviewed = await _context.MatchReviews.AnyAsync(r =>
                r.MatchID == matchId &&
                r.ReviewerUserID == hostUserId &&
                r.ReviewedUserID == reviewedPlayerId &&
                r.ReviewType == "HostToPlayer");
            return !alreadyReviewed;
        }

        public async Task<MatchReview?> SubmitPlayerReviewAsync(int matchId, int reviewerUserId, PlayerReviewInput input)
        {
            if (!await CanSubmitPlayerReviewAsync(matchId, reviewerUserId)) return null;

            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return null;

            var review = new MatchReview
            {
                MatchID = matchId,
                ReviewerUserID = reviewerUserId,
                ReviewedUserID = match.CreatedByUserID,
                ReviewType = "PlayerToMatch",
                ScoreOrganization = input.ScoreOrganization,
                ScoreEquipment = input.ScoreEquipment,
                ScoreAtmosphere = input.ScoreAtmosphere,
                ScoreHost = input.ScoreHost,
                ScoreValueForMoney = input.ScoreValueForMoney,
                Comment = input.Comment?.Trim(),
                IsVisible = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.MatchReviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<MatchReview?> SubmitHostReviewAsync(int matchId, int hostUserId, int reviewedPlayerId, HostReviewInput input)
        {
            if (!await CanSubmitHostReviewAsync(matchId, hostUserId, reviewedPlayerId)) return null;

            var review = new MatchReview
            {
                MatchID = matchId,
                ReviewerUserID = hostUserId,
                ReviewedUserID = reviewedPlayerId,
                ReviewType = "HostToPlayer",
                ScorePunctuality = input.ScorePunctuality,
                ScoreSportsmanship = input.ScoreSportsmanship,
                ScoreSkillAccuracy = input.ScoreSkillAccuracy,
                Comment = input.Comment?.Trim(),
                IsVisible = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.MatchReviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<UserRatingSummary> GetHostRatingSummaryAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            var reviews = await _context.MatchReviews
                .Where(r => r.ReviewedUserID == userId && r.ReviewType == "PlayerToMatch" && r.IsVisible)
                .ToListAsync();

            var summary = new UserRatingSummary
            {
                UserId = userId,
                UserName = user?.FullName ?? string.Empty,
                AvatarUrl = user?.AvatarUrl,
                ReviewCount = reviews.Count
            };

            if (reviews.Count == 0) return summary;

            summary.AvgOrganization = reviews.Where(r => r.ScoreOrganization.HasValue).Select(r => (decimal)r.ScoreOrganization!.Value).DefaultIfEmpty(0).Average();
            summary.AvgEquipment = reviews.Where(r => r.ScoreEquipment.HasValue).Select(r => (decimal)r.ScoreEquipment!.Value).DefaultIfEmpty(0).Average();
            summary.AvgAtmosphere = reviews.Where(r => r.ScoreAtmosphere.HasValue).Select(r => (decimal)r.ScoreAtmosphere!.Value).DefaultIfEmpty(0).Average();
            summary.AvgHost = reviews.Where(r => r.ScoreHost.HasValue).Select(r => (decimal)r.ScoreHost!.Value).DefaultIfEmpty(0).Average();
            summary.AvgValueForMoney = reviews.Where(r => r.ScoreValueForMoney.HasValue).Select(r => (decimal)r.ScoreValueForMoney!.Value).DefaultIfEmpty(0).Average();
            summary.OverallAverage = reviews.Average(r => r.AverageScore);

            return summary;
        }

        public async Task<UserRatingSummary> GetPlayerRatingSummaryAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            var reviews = await _context.MatchReviews
                .Where(r => r.ReviewedUserID == userId && r.ReviewType == "HostToPlayer" && r.IsVisible)
                .ToListAsync();

            var summary = new UserRatingSummary
            {
                UserId = userId,
                UserName = user?.FullName ?? string.Empty,
                AvatarUrl = user?.AvatarUrl,
                ReviewCount = reviews.Count
            };

            if (reviews.Count == 0) return summary;

            summary.AvgPunctuality = reviews.Where(r => r.ScorePunctuality.HasValue).Select(r => (decimal)r.ScorePunctuality!.Value).DefaultIfEmpty(0).Average();
            summary.AvgSportsmanship = reviews.Where(r => r.ScoreSportsmanship.HasValue).Select(r => (decimal)r.ScoreSportsmanship!.Value).DefaultIfEmpty(0).Average();
            summary.AvgSkillAccuracy = reviews.Where(r => r.ScoreSkillAccuracy.HasValue).Select(r => (decimal)r.ScoreSkillAccuracy!.Value).DefaultIfEmpty(0).Average();
            summary.OverallAverage = reviews.Average(r => r.AverageScore);

            return summary;
        }

        public async Task<List<MatchReviewHistoryItem>> GetReceivedReviewsAsync(int userId)
        {
            var entities = await _context.MatchReviews
                .Include(r => r.Match)
                .Include(r => r.ReviewerUser)
                .Where(r => r.ReviewedUserID == userId && r.IsVisible)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return entities.Select(r => new MatchReviewHistoryItem
            {
                ReviewId = r.MatchReviewID,
                MatchId = r.MatchID,
                MatchTitle = r.Match.Title ?? r.Match.MatchType,
                MatchDate = r.Match.MatchDate,
                ReviewType = r.ReviewType,
                ReviewerName = r.ReviewerUser.FullName,
                ReviewerAvatarUrl = r.ReviewerUser.AvatarUrl,
                ReviewedName = string.Empty,
                AverageScore = r.AverageScore,
                Comment = r.Comment,
                IsVisible = r.IsVisible,
                CreatedAt = r.CreatedAt,
                ScoreOrganization = r.ScoreOrganization,
                ScoreEquipment = r.ScoreEquipment,
                ScoreAtmosphere = r.ScoreAtmosphere,
                ScoreHost = r.ScoreHost,
                ScoreValueForMoney = r.ScoreValueForMoney,
                ScorePunctuality = r.ScorePunctuality,
                ScoreSportsmanship = r.ScoreSportsmanship,
                ScoreSkillAccuracy = r.ScoreSkillAccuracy
            }).ToList();
        }

        public async Task<List<MatchReviewHistoryItem>> GetAllReviewsAsync()
        {
            var entities = await _context.MatchReviews
                .Include(r => r.Match)
                .Include(r => r.ReviewerUser)
                .Include(r => r.ReviewedUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return entities.Select(r => new MatchReviewHistoryItem
            {
                ReviewId = r.MatchReviewID,
                MatchId = r.MatchID,
                MatchTitle = r.Match.Title ?? r.Match.MatchType,
                MatchDate = r.Match.MatchDate,
                ReviewType = r.ReviewType,
                ReviewerName = r.ReviewerUser.FullName,
                ReviewerAvatarUrl = r.ReviewerUser.AvatarUrl,
                ReviewedName = r.ReviewedUser.FullName,
                AverageScore = r.AverageScore,
                Comment = r.Comment,
                IsVisible = r.IsVisible,
                CreatedAt = r.CreatedAt,
                ScoreOrganization = r.ScoreOrganization,
                ScoreEquipment = r.ScoreEquipment,
                ScoreAtmosphere = r.ScoreAtmosphere,
                ScoreHost = r.ScoreHost,
                ScoreValueForMoney = r.ScoreValueForMoney,
                ScorePunctuality = r.ScorePunctuality,
                ScoreSportsmanship = r.ScoreSportsmanship,
                ScoreSkillAccuracy = r.ScoreSkillAccuracy
            }).ToList();
        }

        public async Task<bool> SetVisibilityAsync(int reviewId, bool isVisible)
        {
            var review = await _context.MatchReviews.FindAsync(reviewId);
            if (review == null) return false;
            review.IsVisible = isVisible;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<PendingReviewItem>> GetPendingReviewsForUserAsync(int userId)
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-ReviewWindowDays);

            var matches = await _context.Matches
                .Include(m => m.Participants)
                .Where(m => m.Status != "Cancelled" && m.MatchDate >= DateOnly.FromDateTime(sevenDaysAgo).ToDateTime(TimeOnly.MinValue).Date
                    && (m.CreatedByUserID == userId ||
                        m.Participants.Any(p => p.UserID == userId && p.JoinStatus == "Accepted")))
                .ToListAsync();

            var existingReviews = await _context.MatchReviews
                .Where(r => r.ReviewerUserID == userId)
                .Select(r => new { r.MatchID, r.ReviewType, r.ReviewedUserID })
                .ToListAsync();

            var result = new List<PendingReviewItem>();

            foreach (var match in matches)
            {
                if (!MatchHasEnded(match) || !WithinReviewWindow(match)) continue;

                var isHost = match.CreatedByUserID == userId;
                var isPlayer = match.Participants.Any(p => p.UserID == userId && p.JoinStatus == "Accepted");

                var canRateAsPlayer = isPlayer && !isHost &&
                    !existingReviews.Any(r => r.MatchID == match.MatchID && r.ReviewType == "PlayerToMatch");

                var playersToRate = new List<PlayerToRateItem>();
                if (isHost)
                {
                    var acceptedPlayers = match.Participants.Where(p => p.JoinStatus == "Accepted" && p.UserID != userId).ToList();
                    foreach (var p in acceptedPlayers)
                    {
                        var alreadyRated = existingReviews.Any(r =>
                            r.MatchID == match.MatchID && r.ReviewType == "HostToPlayer" && r.ReviewedUserID == p.UserID);
                        var user = await _context.Users.FindAsync(p.UserID);
                        playersToRate.Add(new PlayerToRateItem
                        {
                            UserId = p.UserID,
                            FullName = user?.FullName ?? string.Empty,
                            AvatarUrl = user?.AvatarUrl,
                            AlreadyRated = alreadyRated
                        });
                    }
                }

                if (!canRateAsPlayer && playersToRate.All(p => p.AlreadyRated)) continue;

                result.Add(new PendingReviewItem
                {
                    MatchId = match.MatchID,
                    MatchTitle = match.Title ?? match.MatchType,
                    MatchDate = match.MatchDate,
                    CanRateAsPlayer = canRateAsPlayer,
                    CanRateAsHost = isHost && playersToRate.Any(p => !p.AlreadyRated),
                    PlayersToRate = playersToRate
                });
            }

            return result;
        }

        public async Task<Dictionary<int, (decimal Avg, int Count)>> GetBatchHostRatingsAsync(IEnumerable<int> hostIds)
        {
            var ids = hostIds.Distinct().ToList();
            if (ids.Count == 0) return new();

            var allReviews = await _context.MatchReviews
                .Where(r => ids.Contains(r.ReviewedUserID) && r.ReviewType == "PlayerToMatch" && r.IsVisible)
                .ToListAsync();

            var result = new Dictionary<int, (decimal Avg, int Count)>();
            foreach (var hostId in ids)
            {
                var hostReviews = allReviews.Where(r => r.ReviewedUserID == hostId).ToList();
                if (hostReviews.Count == 0) continue;
                result[hostId] = (hostReviews.Average(r => r.AverageScore), hostReviews.Count);
            }
            return result;
        }

        public async Task<List<PlayerToRateItem>> GetPlayersToRateAsync(int matchId, int hostUserId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);
            if (match == null || match.CreatedByUserID != hostUserId) return new();

            var existingReviews = await _context.MatchReviews
                .Where(r => r.ReviewerUserID == hostUserId && r.MatchID == matchId && r.ReviewType == "HostToPlayer")
                .Select(r => r.ReviewedUserID)
                .ToListAsync();

            var result = new List<PlayerToRateItem>();
            foreach (var p in match.Participants.Where(p => p.JoinStatus == "Accepted" && p.UserID != hostUserId))
            {
                var user = await _context.Users.FindAsync(p.UserID);
                result.Add(new PlayerToRateItem
                {
                    UserId = p.UserID,
                    FullName = user?.FullName ?? string.Empty,
                    AvatarUrl = user?.AvatarUrl,
                    AlreadyRated = existingReviews.Contains(p.UserID)
                });
            }
            return result;
        }
    }
}
