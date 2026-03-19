using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Globalization;
using System.Text.RegularExpressions;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    public class DetailsModel : PageModel
    {
        private readonly IMatchService _matchService;

        public DetailsModel(IMatchService matchService)
        {
            _matchService = matchService;
        }

        public MatchDetailItem? Item { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            ViewData["ActivePage"] = "Matchmaking";
            if (!id.HasValue)
            {
                return RedirectToPage("/Matchmaking/Index");
            }

            var match = await _matchService.GetMatchDetailsAsync(id.Value);
            if (match == null)
            {
                return RedirectToPage("/Matchmaking/Index");
            }

            var currentUserId = GetCurrentUserId();
            var alreadyJoined = currentUserId > 0 && match.Participants.Any(p => p.UserID == currentUserId);
            var canJoin = currentUserId > 0
                          && !alreadyJoined
                          && string.Equals(match.Status, "Open", StringComparison.OrdinalIgnoreCase)
                          && match.Participants.Count < match.MaxParticipants;

            var customCourtName = ExtractCustomCourtName(match.Description);
            var customCourtAddress = ExtractCustomCourtAddress(match.Description);
            var fallbackLocation = BuildCustomLocation(customCourtName, customCourtAddress);

            Item = new MatchDetailItem
            {
                MatchId = match.MatchID,
                Title = string.IsNullOrWhiteSpace(match.Title) ? $"{match.MatchType} Match" : match.Title,
                MatchType = string.IsNullOrWhiteSpace(match.MatchType) ? "Open Match" : match.MatchType,
                SkillRequired = string.IsNullOrWhiteSpace(match.SkillRequired) ? "Any" : match.SkillRequired,
                DateText = $"{match.MatchDate:dddd, dd MMM yyyy}",
                TimeText = $"{match.StartTime:hh\\:mm} - {match.EndTime:hh\\:mm}",
                Venue = fallbackLocation ?? match.Court?.Venue?.VenueName ?? "TBD Venue",
                CourtName = customCourtName ?? match.Court?.CourtName ?? "Not specified",
                CourtAddress = customCourtAddress ?? match.Court?.Venue?.Address ?? "Not specified",
                PriceDisplay = BuildPriceDisplay(match),
                Participants = match.Participants.Select(p => p.User.FullName).ToList(),
                MaxParticipants = match.MaxParticipants,
                SpotsLeft = Math.Max(0, match.MaxParticipants - match.Participants.Count),
                IsJoined = alreadyJoined,
                CanJoin = canJoin,
                IsOwner = currentUserId > 0 && match.CreatedByUserID == currentUserId
            };

            return Page();
        }

        public async Task<IActionResult> OnPostJoinAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = $"/Matchmaking/Details/{id}" });
            }

            var joined = await _matchService.JoinMatchAsync(id, userId);
            TempData[joined ? "SuccessMessage" : "ErrorMessage"] = joined
                ? "You have joined this match."
                : "Unable to join this match.";

            return RedirectToPage(new { id });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private static string? ExtractCustomCourtName(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;

            var lines = description
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return lines.FirstOrDefault(l => l.StartsWith("Court name:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court name:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        private static string? ExtractCustomCourtAddress(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;

            var lines = description
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return lines.FirstOrDefault(l => l.StartsWith("Court address:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court address:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        private static string? BuildCustomLocation(string? courtName, string? courtAddress)
        {
            if (!string.IsNullOrWhiteSpace(courtName) && !string.IsNullOrWhiteSpace(courtAddress))
            {
                return $"{courtName} - {courtAddress}";
            }

            return !string.IsNullOrWhiteSpace(courtName) ? courtName : courtAddress;
        }

        private static string BuildPriceDisplay(Models.Entities.Match match)
        {
            var customPrice = ExtractCustomPrice(match.Description);
            if (customPrice.HasValue)
            {
                return $"{customPrice.Value:N0} VND";
            }

            if (match.Booking?.FinalAmount > 0)
            {
                return $"{match.Booking.FinalAmount:N0} VND total";
            }

            if (match.Court?.PricingRules != null && match.Court.PricingRules.Any())
            {
                var min = match.Court.PricingRules.Min(p => p.UnitPrice);
                return $"From {min:N0} VND/hour";
            }

            return "Price not set";
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

        public class MatchDetailItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string MatchType { get; set; } = string.Empty;
            public string SkillRequired { get; set; } = "Any";
            public string DateText { get; set; } = string.Empty;
            public string TimeText { get; set; } = string.Empty;
            public string Venue { get; set; } = string.Empty;
            public string CourtName { get; set; } = string.Empty;
            public string CourtAddress { get; set; } = string.Empty;
            public string PriceDisplay { get; set; } = string.Empty;
            public List<string> Participants { get; set; } = new();
            public int MaxParticipants { get; set; }
            public int SpotsLeft { get; set; }
            public bool IsJoined { get; set; }
            public bool CanJoin { get; set; }
            public bool IsOwner { get; set; }
        }
    }
}
