using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public List<MatchCardViewModel> RecommendedMatches { get; set; } = new();
        public List<PlayerCardViewModel> SuggestedPlayers { get; set; } = new();
        public List<CourtCardViewModel> NearbyCourts { get; set; } = new();

        public IndexModel(
            ILogger<IndexModel> logger,
            IMatchService matchService,
            IUserService userService,
            ICourtService courtService)
        {
            _logger = logger;
            _matchService = matchService;
            _userService = userService;
            _courtService = courtService;
        }

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Home";

            var currentUserId = GetCurrentUserId();

            await LoadRecommendedMatchesAsync();
            await LoadSuggestedPlayersAsync(currentUserId);
            await LoadNearbyCourtsAsync();
        }

        private async Task LoadRecommendedMatchesAsync()
        {
            var matches = await _matchService.GetRecommendedMatchesAsync(6);

            RecommendedMatches = matches.Select(m => new MatchCardViewModel
            {
                MatchID = m.MatchID,
                Title = string.IsNullOrWhiteSpace(m.Title) ? $"{m.MatchType} Match" : m.Title,
                SportName = m.Sport?.SportName ?? "Sport",
                MatchCategory = GetCategoryFromType(m.MatchType),
                MatchDate = m.MatchDate,
                StartTime = m.StartTime,
                EndTime = m.EndTime,
                VenueName = m.Court?.Venue?.VenueName ?? "TBD Venue",
                CourtImageUrl = m.Court?.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsMain)?.ImageUrl
                                ?? m.Court?.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl
                                ?? "https://lh3.googleusercontent.com/aida-public/AB6AXuDCZms0q2ESpaDHRdZkf9dE4qMQZVkgjJF0HnN65HWF8MiraPWap2EeqIu5B7lpZay82on8EwiajwpFEaLc1mBLtzup2a-2NvPWKA3XU36SNDcXt-gXNlhyrefVLm2peEdMau0QNC2KvvV6JmiocZGK85Vy0y1YJaMXMWTYDMndO9e6k4o50HhcXXRpw7Pmk8OkE_yboqgbxG0tyqV8PUsxVgj6n3ll8iXu_RU0HbcH2QNCTKjyz_4eA2XCg2RBtRGz0zIqIBFgcMk",
                MaxParticipants = m.MaxParticipants,
                CurrentParticipants = m.Participants.Count,
                ParticipantAvatars = m.Participants
                    .Select(p => string.IsNullOrWhiteSpace(p.User.AvatarUrl)
                        ? "https://lh3.googleusercontent.com/aida-public/AB6AXuBJltQB4lJgS6elN1iftfCyl_n5HBEP0j_xJkKu8o8SUu28nW-ZpKxhvmTE5KvRTwL06e1t1hYxppuU14VoLYRyrB7-Khb8Iy7AVm4zWPRFRqv9uusxNIXrIciMsOBnbaRk3XB3t4g7Jb1THJtTTp_I1VcyPYXo_gHWpr-tj95_hcTr9OHAD7S1w26jBmsoEijoqrSvhaA_0uXVETsD7iDV2UWhjKppEjq0kQ89mvU1yke0NQsJMfzIqwDtzzd2xjTr0n_GHfwCkeE"
                        : p.User.AvatarUrl!)
                    .Take(4)
                    .ToList()
            }).ToList();
        }

        private async Task LoadSuggestedPlayersAsync(int currentUserId)
        {
            var users = await _userService.GetSuggestedPlayersAsync(currentUserId, 4);

            var sportFallback = new[] { "Tennis", "Badminton", "Pickleball" };
            SuggestedPlayers = users.Select((u, idx) => new PlayerCardViewModel
            {
                UserID = u.UserID,
                FullName = u.FullName,
                SkillLevel = u.SkillLevel ?? "Intermediate",
                FavoriteSport = sportFallback[idx % sportFallback.Length],
                AverageRating = 4.5,
                TotalMatches = 10 + (u.UserID % 30),
                AvatarUrl = string.IsNullOrWhiteSpace(u.AvatarUrl)
                    ? "https://lh3.googleusercontent.com/aida-public/AB6AXuANCPA1RsIoRkhzqqh-JRBEY5jifMop0t_XlHA99NH2WTa1vOcXXGJyT4ojWhc5eqon5c3CBeB_BXEEs1GmCOwGkza4AOTFOrUCnFdoRz0cr5O3qZYj_T-XkUOJ9_2ktWjlgVPkfGvmo242d93BG4-fYYnqTki_EQzyDWIxHw90-I5KPodkgOFcr2OuJ2zvJOTEjJHmL77YOUq7VFAtHrdgDUmNLJgjYhyAI9QbZaj_urfOXxuGCEk28JCU4Ft1f6VYEm4E1BFSOHI"
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
