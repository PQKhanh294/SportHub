using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Players
{
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ApplicationDbContext _context;

        public IndexModel(IUserService userService, ISubscriptionService subscriptionService, ApplicationDbContext context)
        {
            _userService = userService;
            _subscriptionService = subscriptionService;
            _context = context;
        }

        public List<PlayerItem> Players { get; set; } = new();
        public bool CanSeePhone { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SportFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? LocationFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalCount { get; set; }
        public List<string> SportOptions { get; set; } = new();
        private const int PageSize = 12;

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
            var currentUserId = GetCurrentUserId();

            if (currentUserId > 0)
                CanSeePhone = await _subscriptionService.CanSeePhoneNumberAsync(currentUserId);

            SportOptions = await _context.Sports
                .OrderBy(s => s.SportName)
                .Select(s => s.SportName)
                .ToListAsync();

            var users = await _userService.GetSuggestedPlayersAsync(currentUserId, 200);

            if (!string.IsNullOrWhiteSpace(SportFilter))
            {
                users = users
                    .Where(u => !string.IsNullOrWhiteSpace(u.FavoriteSport)
                        && u.FavoriteSport.Contains(SportFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(LocationFilter))
            {
                users = users
                    .Where(u => !string.IsNullOrWhiteSpace(u.DefaultAddress)
                        && u.DefaultAddress.Contains(LocationFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            TotalCount = users.Count;
            if (PageNumber < 1) PageNumber = 1;

            var paged = users
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Players = paged.Select(u => new PlayerItem
            {
                UserId = u.UserID,
                FullName = u.FullName,
                SkillLevel = u.SkillLevel ?? "Intermediate",
                AvatarUrl = string.IsNullOrWhiteSpace(u.AvatarUrl)
                    ? "/images/avatar-default.png"
                    : u.AvatarUrl,
                TotalMatches = u.UserID % 30 + 5,
                Phone = CanSeePhone ? u.PhoneNumber : null,
                DefaultAddress = u.DefaultAddress,
                FavoriteSport = u.FavoriteSport
            }).ToList();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        public class PlayerItem
        {
            public int UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string SkillLevel { get; set; } = string.Empty;
            public string AvatarUrl { get; set; } = string.Empty;
            public int TotalMatches { get; set; }
            public string? Phone { get; set; }
            public string? DefaultAddress { get; set; }
            public string? FavoriteSport { get; set; }
        }
    }
}
