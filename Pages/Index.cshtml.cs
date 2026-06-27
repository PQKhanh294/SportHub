using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Models.ViewModels;
using SportHub.Services.Interfaces;

namespace SportHub.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IMatchService _matchService;
        private readonly IUserService _userService;
        private readonly ICourtService _courtService;
        private readonly IMatchReviewService _reviewService;
        private readonly ApplicationDbContext _context;

        public List<MatchCardViewModel> RecommendedMatches { get; set; } = new();
        public List<PlayerCardViewModel> SuggestedPlayers { get; set; } = new();
        public List<CourtCardViewModel> NearbyCourts { get; set; } = new();

        // Stats thực từ DB
        public int TotalUsers { get; set; }
        public int MatchesThisWeek { get; set; }
        public int TotalVenues { get; set; }
        public double? AvgRating { get; set; }

        public record MatchQuickItem(int MatchId, string Title, DateTime Date, TimeSpan Start, string HostName, bool IsHost);
        public List<MatchQuickItem> PendingRequests { get; set; } = new();
        public List<MatchQuickItem> UpcomingMyMatches { get; set; } = new();

        public IndexModel(
            ILogger<IndexModel> logger,
            IMatchService matchService,
            IUserService userService,
            ICourtService courtService,
            IMatchReviewService reviewService,
            ApplicationDbContext context)
        {
            _logger = logger;
            _matchService = matchService;
            _userService = userService;
            _courtService = courtService;
            _reviewService = reviewService;
            _context = context;
        }

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Home";

            var currentUserId = GetCurrentUserId();

            await LoadRecommendedMatchesAsync();
            await LoadSuggestedPlayersAsync(currentUserId);
            await LoadNearbyCourtsAsync();
            await LoadStatsAsync();
            if (currentUserId > 0)
                await LoadPersonalSectionsAsync(currentUserId);
        }

        private async Task LoadPersonalSectionsAsync(int userId)
        {
            var now = DateTime.UtcNow;

            PendingRequests = await _context.MatchParticipants
                .Where(mp => mp.UserID == userId && mp.JoinStatus == "Pending" && mp.Match.MatchDate >= now)
                .OrderBy(mp => mp.Match.MatchDate)
                .Take(5)
                .Select(mp => new MatchQuickItem(
                    mp.Match.MatchID,
                    mp.Match.Title ?? $"Trận #{mp.Match.MatchID}",
                    mp.Match.MatchDate,
                    mp.Match.StartTime,
                    mp.Match.CreatedByUser.FullName,
                    false
                ))
                .ToListAsync();

            var participating = await _context.MatchParticipants
                .Where(mp => mp.UserID == userId
                    && (mp.JoinStatus == "Approved" || mp.JoinStatus == "Accepted")
                    && mp.Match.MatchDate >= now
                    && mp.Match.Status != "Cancelled")
                .OrderBy(mp => mp.Match.MatchDate)
                .Take(5)
                .Select(mp => new MatchQuickItem(
                    mp.Match.MatchID,
                    mp.Match.Title ?? $"Trận #{mp.Match.MatchID}",
                    mp.Match.MatchDate,
                    mp.Match.StartTime,
                    mp.Match.CreatedByUser.FullName,
                    false
                ))
                .ToListAsync();

            var hosted = await _context.Matches
                .Where(m => m.CreatedByUserID == userId && m.MatchDate >= now && m.Status != "Cancelled")
                .OrderBy(m => m.MatchDate)
                .Take(5)
                .Select(m => new MatchQuickItem(
                    m.MatchID,
                    m.Title ?? $"Trận #{m.MatchID}",
                    m.MatchDate,
                    m.StartTime,
                    m.CreatedByUser.FullName,
                    true
                ))
                .ToListAsync();

            UpcomingMyMatches = participating.Concat(hosted)
                .DistinctBy(m => m.MatchId)
                .OrderBy(m => m.Date)
                .Take(5)
                .ToList();
        }

        private async Task LoadStatsAsync()
        {
            var now = DateTime.UtcNow;
            var weekAgo = now.AddDays(-7);

            TotalUsers = await _context.Users.CountAsync(u => u.IsActive);
            MatchesThisWeek = await _context.Matches
                .CountAsync(m => m.MatchDate >= weekAgo);
            TotalVenues = await _context.CourtVenues.CountAsync(v => v.IsActive);
            // AverageScore là computed property — cần chọn các cột thô và tính phía client
            var reviewScores = await _context.MatchReviews
                .Select(r => new {
                    r.ReviewType,
                    r.ScoreOrganization, r.ScoreEquipment, r.ScoreAtmosphere, r.ScoreHost, r.ScoreValueForMoney,
                    r.ScorePunctuality, r.ScoreSportsmanship, r.ScoreSkillAccuracy
                })
                .ToListAsync();
            if (reviewScores.Count > 0)
            {
                var allAvgs = reviewScores.Select(r => (double)new MatchReview {
                    ReviewType = r.ReviewType,
                    ScoreOrganization = r.ScoreOrganization, ScoreEquipment = r.ScoreEquipment,
                    ScoreAtmosphere = r.ScoreAtmosphere, ScoreHost = r.ScoreHost, ScoreValueForMoney = r.ScoreValueForMoney,
                    ScorePunctuality = r.ScorePunctuality, ScoreSportsmanship = r.ScoreSportsmanship,
                    ScoreSkillAccuracy = r.ScoreSkillAccuracy
                }.AverageScore).Where(v => v > 0).ToList();
                AvgRating = allAvgs.Count > 0 ? Math.Round(allAvgs.Average(), 1) : null;
            }
        }

        private async Task LoadRecommendedMatchesAsync()
        {
            var matches = await _matchService.GetRecommendedMatchesAsync(6);
            var hostIds = matches.Select(m => m.CreatedByUserID).Distinct();
            var ratings = await _reviewService.GetBatchHostRatingsAsync(hostIds);

            RecommendedMatches = matches.Select(m => new MatchCardViewModel
            {
                MatchID = m.MatchID,
                Title = string.IsNullOrWhiteSpace(m.Title) ? $"{m.MatchType} Match" : m.Title,
                SportName = m.Sport?.SportName ?? "Sport",
                MatchCategory = GetCategoryFromType(m.MatchType),
                MatchDate = m.MatchDate,
                StartTime = m.StartTime,
                EndTime = m.EndTime,
                VenueName = ExtractCustomVenue(m.Description) ?? m.Court?.Venue?.VenueName ?? "TBD Venue",
                CourtImageUrl = m.Sport?.SportName != null && (m.Sport.SportName.Contains("Cầu lông", StringComparison.OrdinalIgnoreCase) || m.Sport.SportName.Contains("Badminton", StringComparison.OrdinalIgnoreCase))
                                ? "/images/badminton_bg.png"
                                : (m.Court?.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsMain)?.ImageUrl
                                ?? m.Court?.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl
                                ?? "https://lh3.googleusercontent.com/aida-public/AB6AXuDCZms0q2ESpaDHRdZkf9dE4qMQZVkgjJF0HnN65HWF8MiraPWap2EeqIu5B7lpZay82on8EwiajwpFEaLc1mBLtzup2a-2NvPWKA3XU36SNDcXt-gXNlhyrefVLm2peEdMau0QNC2KvvV6JmiocZGK85Vy0y1YJaMXMWTYDMndO9e6k4o50HhcXXRpw7Pmk8OkE_yboqgbxG0tyqV8PUsxVgj6n3ll8iXu_RU0HbcH2QNCTKjyz_4eA2XCg2RBtRGz0zIqIBFgcMk"),
                MaxParticipants = m.MaxParticipants,
                CurrentParticipants = m.Participants.Count,
                ParticipantAvatars = m.Participants
                    .Select(p => string.IsNullOrWhiteSpace(p.User.AvatarUrl)
                        ? "/images/avatar-default.png"
                        : p.User.AvatarUrl!)
                    .Take(4)
                    .ToList(),
                HostId = m.CreatedByUserID,
                HostRatingAvg = ratings.ContainsKey(m.CreatedByUserID) ? ratings[m.CreatedByUserID].Avg : null,
                HostRatingCount = ratings.ContainsKey(m.CreatedByUserID) ? ratings[m.CreatedByUserID].Count : 0
            }).ToList();
        }

        private async Task LoadSuggestedPlayersAsync(int currentUserId)
        {
            var users = await _userService.GetSuggestedPlayersAsync(currentUserId, 4);

            // Lấy số trận và rating thực từ DB
            var userIds = users.Select(u => u.UserID).ToList();
            var matchCounts = await _context.MatchParticipants
                .Where(mp => userIds.Contains(mp.UserID))
                .GroupBy(mp => mp.UserID)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            // AverageScore là computed property — lấy raw scores rồi tính client-side
            var rawReviews = await _context.MatchReviews
                .Where(r => userIds.Contains(r.ReviewedUserID))
                .Select(r => new {
                    r.ReviewedUserID, r.ReviewType,
                    r.ScoreOrganization, r.ScoreEquipment, r.ScoreAtmosphere, r.ScoreHost, r.ScoreValueForMoney,
                    r.ScorePunctuality, r.ScoreSportsmanship, r.ScoreSkillAccuracy
                })
                .ToListAsync();
            var ratings = rawReviews
                .GroupBy(r => r.ReviewedUserID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(r => (double)new MatchReview {
                        ReviewType = r.ReviewType,
                        ScoreOrganization = r.ScoreOrganization, ScoreEquipment = r.ScoreEquipment,
                        ScoreAtmosphere = r.ScoreAtmosphere, ScoreHost = r.ScoreHost, ScoreValueForMoney = r.ScoreValueForMoney,
                        ScorePunctuality = r.ScorePunctuality, ScoreSportsmanship = r.ScoreSportsmanship,
                        ScoreSkillAccuracy = r.ScoreSkillAccuracy
                    }.AverageScore).Where(v => v > 0).DefaultIfEmpty(0).Average()
                );

            SuggestedPlayers = users.Select(u => new PlayerCardViewModel
            {
                UserID = u.UserID,
                FullName = u.FullName,
                SkillLevel = string.IsNullOrWhiteSpace(u.SkillLevel) ? "Chưa cập nhật" : u.SkillLevel,
                FavoriteSport = string.IsNullOrWhiteSpace(u.FavoriteSport) ? "Chưa cập nhật" : u.FavoriteSport,
                AverageRating = ratings.TryGetValue(u.UserID, out var avg) ? avg : 0,
                TotalMatches = matchCounts.TryGetValue(u.UserID, out var cnt) ? cnt : 0,
                AvatarUrl = string.IsNullOrWhiteSpace(u.AvatarUrl)
                    ? "/images/avatar-default.png"
                    : u.AvatarUrl,
                AlreadyConnected = false
            }).ToList();
        }

        private async Task LoadNearbyCourtsAsync()
        {
            var venues = await _courtService.GetNearbyVenuesAsync(16.0471, 108.2068, 3);

            NearbyCourts = venues
                .Select((v, idx) => new { Venue = v, Court = v.Courts.FirstOrDefault(c => c.IsActive) })
                .Where(x => x.Court != null)
                .Select((x, idx) => new CourtCardViewModel
                {
                    CourtID = x.Court!.CourtID,
                    VenueID = x.Venue.VenueID,
                    CourtName = x.Court.CourtName,
                    VenueName = x.Venue.VenueName,
                    SportName = x.Court.Sport?.SportName ?? "Sport",
                    District = x.Venue.District,
                    City = x.Venue.City,
                    DistanceKm = Math.Round(1.2 + (idx * 1.1), 1),
                    Latitude = x.Venue.Latitude,
                    Longitude = x.Venue.Longitude,
                    TotalCourts = x.Venue.Courts.Count,
                    CourtType = x.Court.CourtType ?? "Indoor",
                    MinPricePerHour = x.Court.PricingRules.Any() ? x.Court.PricingRules.Min(p => p.UnitPrice) : 150000,
                    MainImageUrl = x.Court.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsMain)?.ImageUrl
                                   ?? x.Court.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl
                                   ?? "https://lh3.googleusercontent.com/aida-public/AB6AXuCVuMggvVL3K3LQJ9NvhMzCvM9r4CE_680-nm1iKD6EUH5RT-Q-APnpgK9Jxri9WpYYaL5UgafCC3OPzfZLvzoEjucgH020pN6eeVEjBytzt3sZglMu0HXfaXVk86ReKi0Iy0t3E4Jz88odvKlByH4u3K4QS6MEWc3CplCGFLwdrZ6ZA2BB0utxIjfkxSSAmBaNPCrhRRInJ5Cj6CYzasX8dJxDoJ_AzqxKegjuPwRWlYYS0mJlR6XjMsb8qSaIlCHa6_XssgM_B7U",
                    Availability = idx switch
                    {
                        0 => CourtCardViewModel.AvailabilityStatus.Available,
                        1 => CourtCardViewModel.AvailabilityStatus.Limited,
                        _ => CourtCardViewModel.AvailabilityStatus.Available
                    }
                })
                .ToList();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private static string? ExtractCustomVenue(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var name = lines.FirstOrDefault(l => l.StartsWith("Court name:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court name:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            var addr = lines.FirstOrDefault(l => l.StartsWith("Court address:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court address:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(addr))
                return $"{name} - {addr}";
            return !string.IsNullOrWhiteSpace(name) ? name : addr;
        }

        private static string GetCategoryFromType(string? matchType)
        {
            return matchType switch
            {
                "Singles" => "Competitive",
                "Doubles" => "Friendly",
                "Mixed" => "Casual",
                _ => "Friendly"
            };
        }
    }
}
