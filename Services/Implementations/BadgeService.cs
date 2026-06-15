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
                BuildBadge("morning-player", "Morning Player", "ChÆ¡i tráº­n báº¯t Ä‘áº§u trÆ°á»›c 10:00.", "wb_twilight", stats.MorningMatches, new[] { 3, 10, 25 }, earned),
                BuildBadge("reliable-teammate", "Reliable Teammate", "Giá»¯ lá»‹ch háº¹n tá»‘t, Ã­t há»§y/no-show.", "verified", stats.ReliabilityScore, new[] { 80, 90, 95 }, earned),
                BuildBadge("friendly-host", "Friendly Host", "Táº¡o tráº­n vÃ  kÃ©o Ä‘Æ°á»£c ngÆ°á»i chÆ¡i tham gia.", "diversity_3", stats.HostedMatchesWithPlayers, new[] { 3, 10, 25 }, earned),
                BuildBadge("quick-responder", "Quick Responder", "Xá»­ lÃ½ yÃªu cáº§u tham gia thay vÃ¬ Ä‘á»ƒ chá».", "bolt", stats.HandledJoinRequests, new[] { 5, 20, 50 }, earned),
                BuildBadge("community-builder", "Community Builder", "ÄÃ£ chÆ¡i cÃ¹ng nhiá»u ngÆ°á»i khÃ¡c nhau.", "groups", stats.UniquePartners, new[] { 5, 15, 40 }, earned)
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

            return new UserBadgeStats(morningMatches, reliabilityScore, hostedMatchesWithPlayers, handledJoinRequests, uniquePartners);
        }

        private record UserBadgeStats(
            int MorningMatches,
            int ReliabilityScore,
            int HostedMatchesWithPlayers,
            int HandledJoinRequests,
            int UniquePartners);
    }
}
