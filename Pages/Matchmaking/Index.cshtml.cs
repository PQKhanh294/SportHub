using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using SportHub.Services.Interfaces;
using SportHub.Data;

namespace SportHub.Pages.Matchmaking
{
    public class IndexModel : PageModel
    {
        private readonly IMatchService _matchService;
        private readonly ApplicationDbContext _context;

        public IndexModel(IMatchService matchService, ApplicationDbContext context)
        {
            _matchService = matchService;
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Sport { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Skill { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Time { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MinPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        public List<string> SportOptions { get; set; } = new();

        public List<MatchCardItem> Matches { get; set; } = new();
        public List<JoinedMatchItem> UpcomingJoinedMatches { get; set; } = new();
        public List<JoinedMatchItem> JoinedMatchHistory { get; set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
            var currentUserId = GetCurrentUserId();

            var matches = await _matchService.GetRecommendedMatchesAsync(50);

            SportOptions = matches
                .Select(m => NormalizeSportName(m.Sport?.SportName))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            if (!string.IsNullOrWhiteSpace(Sport))
            {
                matches = matches
                    .Where(m => string.Equals(NormalizeSportName(m.Sport?.SportName), Sport, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(Skill) && !string.Equals(Skill, "Any", StringComparison.OrdinalIgnoreCase))
            {
                matches = matches
                    .Where(m => string.Equals(m.SkillRequired ?? "Any", Skill, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(m.SkillRequired ?? string.Empty, "Any", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(Time) && !string.Equals(Time, "Any", StringComparison.OrdinalIgnoreCase))
            {
                matches = matches.Where(m => Time switch
                {
                    "Morning" => m.StartTime < new TimeSpan(12, 0, 0),
                    "Afternoon" => m.StartTime >= new TimeSpan(12, 0, 0) && m.StartTime < new TimeSpan(17, 0, 0),
                    "Evening" => m.StartTime >= new TimeSpan(17, 0, 0),
                    _ => true
                }).ToList();
            }

            if (MinPrice.HasValue || MaxPrice.HasValue)
            {
                matches = matches
                    .Where(m =>
                    {
                        var amount = BuildMatchPriceAmount(m);
                        if (!amount.HasValue) return false;

                        if (MinPrice.HasValue && amount.Value < MinPrice.Value) return false;
                        if (MaxPrice.HasValue && amount.Value > MaxPrice.Value) return false;

                        return true;
                    })
                    .ToList();
            }

            Matches = matches.Select(m => new MatchCardItem
            {
                MatchId = m.MatchID,
                Title = string.IsNullOrWhiteSpace(m.Title) ? $"{m.MatchType} Match" : m.Title,
                MatchType = string.IsNullOrWhiteSpace(m.MatchType) ? "Open Match" : m.MatchType,
                SkillRequired = string.IsNullOrWhiteSpace(m.SkillRequired) ? "Any" : m.SkillRequired,
                MatchScore = 80 + (m.MatchID % 20),
                StartText = $"{m.MatchDate:ddd, dd MMM} {m.StartTime:hh\\:mm}",
                Venue = m.Court?.Venue?.VenueName ?? "TBD Venue",
                PriceDisplay = BuildPriceDisplay(m),
                PriceAmount = BuildMatchPriceAmount(m),
                Participants = m.Participants.Count,
                MaxParticipants = m.MaxParticipants,
                IsJoinedByCurrentUser = currentUserId > 0 && m.Participants.Any(p => p.UserID == currentUserId),
                IsOwnedByCurrentUser = currentUserId > 0 && m.CreatedByUserID == currentUserId,
                HostImage = "https://lh3.googleusercontent.com/aida-public/AB6AXuALTzsl3HIXmUVzYbJp9BGERq98PtuYn7CL1IBPSHx3OrkFpRQTd6zNEwo2EOB2GAHS2n3LC0cu5aySM-IHeJtj0RLOAh-XJIVbRYXNwJnzO_vg_zLdZ1I2DUp8Oau0htb6fbzwIpuzo-z_0f6jhNLsN2kI6hKxv61AU141cH6oO2MWjiEcxo4vPxnS-smCZF-xDE6loPZWlYgw4BRwbJGXrc9jUVUA8j-cmjl61KwL1qwdpecYKC7Bo9cEFPXU1nHtK_XjUeQWQlM"
            }).ToList();

            await LoadJoinedMatchesAsync(currentUserId);
        }

        private async Task LoadJoinedMatchesAsync(int userId)
        {
            if (userId <= 0)
            {
                UpcomingJoinedMatches = new();
                JoinedMatchHistory = new();
                return;
            }

            var joined = await _context.MatchParticipants
                .Where(mp => mp.UserID == userId && mp.JoinStatus == "Accepted")
                .Include(mp => mp.Match).ThenInclude(m => m.Court).ThenInclude(c => c!.Venue)
                .OrderByDescending(mp => mp.Match.MatchDate)
                .ThenByDescending(mp => mp.Match.StartTime)
                .ToListAsync();

            var today = DateTime.Today;

            var mapped = joined.Select(mp => new JoinedMatchItem
            {
                MatchId = mp.MatchID,
                Title = string.IsNullOrWhiteSpace(mp.Match.Title) ? $"{mp.Match.MatchType} Match" : mp.Match.Title,
                StartText = $"{mp.Match.MatchDate:ddd, dd MMM} {mp.Match.StartTime:hh\\:mm}",
                Venue = mp.Match.Court?.Venue?.VenueName ?? "TBD Venue",
                MatchDate = mp.Match.MatchDate
            }).ToList();

            UpcomingJoinedMatches = mapped.Where(m => m.MatchDate >= today)
                .Take(5)
                .ToList();

            JoinedMatchHistory = mapped.Where(m => m.MatchDate < today)
                .Take(5)
                .ToList();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private static string BuildPriceDisplay(Models.Entities.Match match)
        {
            var amount = BuildMatchPriceAmount(match);
            if (amount.HasValue)
            {
                if (match.Booking?.FinalAmount > 0)
                {
                    return $"{amount.Value:N0} VND total";
                }

                return $"From {amount.Value:N0} VND/hour";
            }

            return "Price not set";
        }

        private static decimal? BuildMatchPriceAmount(Models.Entities.Match match)
        {
            var customPrice = ExtractCustomPrice(match.Description);
            if (customPrice.HasValue)
            {
                return customPrice.Value;
            }

            if (match.Booking?.FinalAmount > 0)
            {
                return match.Booking.FinalAmount;
            }

            if (match.Court?.PricingRules != null && match.Court.PricingRules.Any())
            {
                return match.Court.PricingRules.Min(p => p.UnitPrice);
            }

            return null;
        }

        private static decimal? ExtractCustomPrice(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;

            var lines = description
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var line = lines.FirstOrDefault(l => l.StartsWith("Match price VND:", StringComparison.OrdinalIgnoreCase));
            if (line == null) return null;

            var raw = line.Replace("Match price VND:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            var normalized = Regex.Replace(raw, "[^0-9,.-]", string.Empty);

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
            {
                return invariantValue;
            }

            if (decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("vi-VN"), out var viValue))
            {
                return viValue;
            }

            return decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("en-US"), out var enValue)
                ? enValue
                : null;
        }

        private static string NormalizeSportName(string? sportName)
        {
            if (string.IsNullOrWhiteSpace(sportName)) return "Sport";

            return sportName.Trim() switch
            {
                "Cầu lông" => "Badminton",
                _ => sportName.Trim()
            };
        }

        public class MatchCardItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string MatchType { get; set; } = string.Empty;
            public string SkillRequired { get; set; } = "Any";
            public int MatchScore { get; set; }
            public string StartText { get; set; } = string.Empty;
            public string Venue { get; set; } = string.Empty;
            public string PriceDisplay { get; set; } = string.Empty;
            public decimal? PriceAmount { get; set; }
            public int Participants { get; set; }
            public int MaxParticipants { get; set; }
            public bool IsJoinedByCurrentUser { get; set; }
            public bool IsOwnedByCurrentUser { get; set; }
            public string HostImage { get; set; } = string.Empty;
        }

        public class JoinedMatchItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string StartText { get; set; } = string.Empty;
            public string Venue { get; set; } = string.Empty;
            public DateTime MatchDate { get; set; }
        }
    }
}
