using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

            Item = new MatchDetailItem
            {
                MatchId = match.MatchID,
                Title = string.IsNullOrWhiteSpace(match.Title) ? $"{match.MatchType} Match" : match.Title,
                MatchType = string.IsNullOrWhiteSpace(match.MatchType) ? "Open Match" : match.MatchType,
                DateText = $"{match.MatchDate:dddd, dd MMM yyyy}",
                TimeText = $"{match.StartTime:hh\\:mm} - {match.EndTime:hh\\:mm}",
                Venue = match.Court?.Venue?.VenueName ?? "TBD Venue",
                Participants = match.Participants.Select(p => p.User.FullName).ToList(),
                MaxParticipants = match.MaxParticipants,
                SpotsLeft = Math.Max(0, match.MaxParticipants - match.Participants.Count)
            };

            return Page();
        }

        public class MatchDetailItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string MatchType { get; set; } = string.Empty;
            public string DateText { get; set; } = string.Empty;
            public string TimeText { get; set; } = string.Empty;
            public string Venue { get; set; } = string.Empty;
            public List<string> Participants { get; set; } = new();
            public int MaxParticipants { get; set; }
            public int SpotsLeft { get; set; }
        }
    }
}
