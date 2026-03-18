using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    public class IndexModel : PageModel
    {
        private readonly IMatchService _matchService;

        public IndexModel(IMatchService matchService)
        {
            _matchService = matchService;
        }

        public List<MatchCardItem> Matches { get; set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";

            var matches = await _matchService.GetRecommendedMatchesAsync(12);
            Matches = matches.Select(m => new MatchCardItem
            {
                MatchId = m.MatchID,
                Title = string.IsNullOrWhiteSpace(m.Title) ? $"{m.MatchType} Match" : m.Title,
                MatchType = string.IsNullOrWhiteSpace(m.MatchType) ? "Open Match" : m.MatchType,
                MatchScore = 80 + (m.MatchID % 20),
                StartText = $"{m.MatchDate:ddd, dd MMM} {m.StartTime:hh\\:mm}",
                Venue = m.Court?.Venue?.VenueName ?? "TBD Venue",
                Participants = m.Participants.Count,
                MaxParticipants = m.MaxParticipants,
                HostImage = "https://lh3.googleusercontent.com/aida-public/AB6AXuALTzsl3HIXmUVzYbJp9BGERq98PtuYn7CL1IBPSHx3OrkFpRQTd6zNEwo2EOB2GAHS2n3LC0cu5aySM-IHeJtj0RLOAh-XJIVbRYXNwJnzO_vg_zLdZ1I2DUp8Oau0htb6fbzwIpuzo-z_0f6jhNLsN2kI6hKxv61AU141cH6oO2MWjiEcxo4vPxnS-smCZF-xDE6loPZWlYgw4BRwbJGXrc9jUVUA8j-cmjl61KwL1qwdpecYKC7Bo9cEFPXU1nHtK_XjUeQWQlM"
            }).ToList();
        }

        public class MatchCardItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string MatchType { get; set; } = string.Empty;
            public int MatchScore { get; set; }
            public string StartText { get; set; } = string.Empty;
            public string Venue { get; set; } = string.Empty;
            public int Participants { get; set; }
            public int MaxParticipants { get; set; }
            public string HostImage { get; set; } = string.Empty;
        }
    }
}
