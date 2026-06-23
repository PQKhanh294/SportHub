using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Stats
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;

        public IndexModel(ApplicationDbContext context, ISubscriptionService subscriptionService)
        {
            _context = context;
            _subscriptionService = subscriptionService;
        }

        public bool HasAccess { get; set; }
        public string? CurrentPlanKey { get; set; }

        // Stats
        public int TotalMatchesJoined { get; set; }
        public int TotalMatchesCreated { get; set; }
        public int TotalMatchesCompleted { get; set; }
        public int TotalMatchesCancelled { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsReceived { get; set; }
        public int FriendsCount { get; set; }
        public int MonthlyJoinCount { get; set; }
        public int MonthlyCreateCount { get; set; }
        public decimal WalletBalance { get; set; }
        public int MatchCredits { get; set; }

        // Top sports played
        public List<SportStat> TopSports { get; set; } = new();

        // Monthly activity (last 6 months)
        public List<MonthlyActivity> MonthlyActivities { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Profile";
            var userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            CurrentPlanKey = (await _subscriptionService.GetActiveSubscriptionAsync(userId))?.PlanKey;
            HasAccess = await _subscriptionService.HasDetailedStatsAsync(userId);

            if (!HasAccess)
                return Page();

            // Basic counts
            TotalMatchesJoined = await _context.MatchParticipants
                .CountAsync(mp => mp.UserID == userId && mp.JoinStatus == "Accepted");

            TotalMatchesCreated = await _context.Matches
                .CountAsync(m => m.CreatedByUserID == userId);

            TotalMatchesCompleted = await _context.Matches
                .CountAsync(m => m.CreatedByUserID == userId && m.Status == "Completed");

            TotalMatchesCancelled = await _context.Matches
                .CountAsync(m => m.CreatedByUserID == userId && m.Status == "Cancelled");

            var reviews = await _context.MatchReviews
                .Where(r => r.ReviewedUserID == userId && r.ReviewType == "PlayerToMatch")
                .ToListAsync();
            ReviewsReceived = reviews.Count;
            AverageRating = reviews.Count > 0
                ? Math.Round(reviews.Average(r =>
                    new[] {
                        r.ScoreOrganization, r.ScoreEquipment, r.ScoreAtmosphere,
                        r.ScoreHost, r.ScorePunctuality, r.ScoreSportsmanship
                    }.Where(s => s.HasValue).Select(s => (double)s!.Value).DefaultIfEmpty(0).Average()
                  ), 1)
                : 0;

            FriendsCount = await _context.Friendships
                .CountAsync(f => (f.SenderID == userId || f.ReceiverID == userId) && f.Status == "Accepted");

            MonthlyJoinCount = await _subscriptionService.GetMonthlyJoinCountAsync(userId);
            MonthlyCreateCount = await _subscriptionService.GetMonthlyCreateCountAsync(userId);
            WalletBalance = await _context.Users.Where(u => u.UserID == userId).Select(u => u.WalletBalance).FirstOrDefaultAsync();
            MatchCredits = await _subscriptionService.GetRemainingCreditsAsync(userId);

            // Top sports
            TopSports = await _context.MatchParticipants
                .Where(mp => mp.UserID == userId && mp.JoinStatus == "Accepted")
                .Join(_context.Matches, mp => mp.MatchID, m => m.MatchID, (mp, m) => m.SportID)
                .Join(_context.Sports, sid => sid, s => s.SportID, (sid, s) => s.SportName)
                .GroupBy(name => name)
                .Select(g => new SportStat { SportName = g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .Take(5)
                .ToListAsync();

            // Monthly activity (last 6 months)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var activities = await _context.SubscriptionUsages
                .Where(u => u.UserID == userId && string.Compare(u.YearMonth, sixMonthsAgo.ToString("yyyy-MM")) >= 0)
                .OrderBy(u => u.YearMonth)
                .ToListAsync();

            MonthlyActivities = activities.Select(u => new MonthlyActivity
            {
                YearMonth = u.YearMonth,
                Joins = u.JoinCount,
                Creates = u.CreateCount
            }).ToList();

            return Page();
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        public class SportStat
        {
            public string SportName { get; set; } = "";
            public int Count { get; set; }
        }

        public class MonthlyActivity
        {
            public string YearMonth { get; set; } = "";
            public int Joins { get; set; }
            public int Creates { get; set; }
        }
    }
}
