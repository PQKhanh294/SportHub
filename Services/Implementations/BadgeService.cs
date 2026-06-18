using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class BadgeService : IBadgeService
    {
        private readonly ApplicationDbContext _context;

        public BadgeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BadgeProgressItem>> GetBadgeProgressAsync(int userId)
        {
            var stats = await BuildStatsAsync(userId);
            var earned = await _context.UserBadges
                .Where(b => b.UserID == userId)
                .ToDictionaryAsync(b => b.BadgeKey);

            var badges = new List<BadgeProgressItem>
            {
                BuildBadge("morning-player", "Morning Player", "Chơi trận bắt đầu trước 10:00.", "wb_twilight", stats.MorningMatches, new[] { 3, 10, 25 }, earned),
                BuildBadge("reliable-teammate", "Reliable Teammate", "Giữ lịch hẹn tốt, ít hủy/no-show.", "verified", stats.ReliabilityScore, new[] { 80, 90, 95 }, earned),
                BuildBadge("friendly-host", "Friendly Host", "Tạo trận và kéo được người chơi tham gia.", "diversity_3", stats.HostedMatchesWithPlayers, new[] { 3, 10, 25 }, earned),
                BuildBadge("quick-responder", "Quick Responder", "Xử lý yêu cầu tham gia thay vì để chờ.", "bolt", stats.HandledJoinRequests, new[] { 5, 20, 50 }, earned),
                BuildBadge("community-builder", "Community Builder", "Đã chơi cùng nhiều người khác nhau.", "groups", stats.UniquePartners, new[] { 5, 15, 40 }, earned),
                BuildBadge("top-rated-host", "Top Rated Host", "Nhận đánh giá cao (≥4.0 sao) từ người chơi.", "star_rate", stats.HighRatingReviews, new[] { 5, 10, 20 }, earned)
            };

            return badges;
        }

        public async Task SyncEarnedBadgesAsync(int userId)
        {
            var progress = await GetBadgeProgressAsync(userId);
            var existing = await _context.UserBadges
                .Where(b => b.UserID == userId)
                .ToDictionaryAsync(b => b.BadgeKey);

            foreach (var item in progress.Where(p => p.IsEarned))
            {
                if (!existing.TryGetValue(item.Key, out var badge))
                {
                    _context.UserBadges.Add(new UserBadge
                    {
                        UserID = userId,
                        BadgeKey = item.Key,
                        BadgeName = item.Name,
                        Level = item.Level,
                        ProgressValue = item.ProgressValue,
                        TargetValue = item.TargetValue,
                        Description = item.Description,
                        EarnedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    badge.BadgeName = item.Name;
                    badge.Level = item.Level;
                    badge.ProgressValue = item.ProgressValue;
                    badge.TargetValue = item.TargetValue;
                    badge.Description = item.Description;
                    badge.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }

        private static BadgeProgressItem BuildBadge(
            string key,
            string name,
            string description,
            string icon,
            int value,
            int[] thresholds,
            IReadOnlyDictionary<string, UserBadge> earned)
        {
            var level = value >= thresholds[2] ? "Gold"
                : value >= thresholds[1] ? "Silver"
                : value >= thresholds[0] ? "Bronze"
                : "None";

            var target = level switch
            {
                "Gold" => thresholds[2],
                "Silver" => thresholds[2],
                "Bronze" => thresholds[1],
                _ => thresholds[0]
            };

            return new BadgeProgressItem
            {
                Key = key,
                Name = name,
                Description = description,
                Icon = icon,
                Level = level,
                ProgressValue = value,
                TargetValue = target,
                EarnedBadge = earned.TryGetValue(key, out var badge) ? badge : null
            };
        }

        private async Task<UserBadgeStats> BuildStatsAsync(int userId)
        {
            var acceptedParticipations = await _context.MatchParticipants
                .Include(p => p.Match)
                .Where(p => p.UserID == userId && p.JoinStatus == "Accepted")
                .ToListAsync();

            var acceptedMatchIds = acceptedParticipations.Select(p => p.MatchID).Distinct().ToList();
            var morningMatches = acceptedParticipations.Count(p => p.Match.StartTime < new TimeSpan(10, 0, 0));

            var cancelledParticipations = await _context.MatchParticipants
                .CountAsync(p => p.UserID == userId && p.JoinStatus == "Cancelled");
            var noShowBookings = await _context.Bookings
                .CountAsync(b => b.UserID == userId && b.Status == "NoShow");
            var totalCommitments = acceptedParticipations.Count + cancelledParticipations + noShowBookings;
            var reliabilityScore = totalCommitments == 0
                ? 0
                : (int)Math.Round((acceptedParticipations.Count * 100m) / totalCommitments);

            var hostedMatchesWithPlayers = await _context.Matches
                .Where(m => m.CreatedByUserID == userId)
                .CountAsync(m => m.Participants.Count(p => p.JoinStatus == "Accepted") > 1);

            var handledJoinRequests = await _context.Matches
                .Where(m => m.CreatedByUserID == userId)
                .SelectMany(m => m.Participants)
                .CountAsync(p => p.UserID != userId && p.JoinStatus != "Pending");

            var uniquePartners = acceptedMatchIds.Count == 0
                ? 0
                : await _context.MatchParticipants
                    .Where(p => acceptedMatchIds.Contains(p.MatchID)
                        && p.UserID != userId
                        && p.JoinStatus == "Accepted")
                    .Select(p => p.UserID)
                    .Distinct()
                    .CountAsync();

            // High-rating reviews: PlayerToMatch reviews where average >= 4.0
            var hostReviews = await _context.MatchReviews
                .Where(r => r.ReviewedUserID == userId && r.ReviewType == "PlayerToMatch" && r.IsVisible)
                .ToListAsync();
            var highRatingReviews = hostReviews.Count(r => r.AverageScore >= 4.0m);

            return new UserBadgeStats(morningMatches, reliabilityScore, hostedMatchesWithPlayers, handledJoinRequests, uniquePartners, highRatingReviews);
        }

        private record UserBadgeStats(
            int MorningMatches,
            int ReliabilityScore,
            int HostedMatchesWithPlayers,
            int HandledJoinRequests,
            int UniquePartners,
            int HighRatingReviews);
    }
}
