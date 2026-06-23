using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Players
{
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly ISubscriptionService _subscriptionService;

        public IndexModel(IUserService userService, ISubscriptionService subscriptionService)
        {
            _userService = userService;
            _subscriptionService = subscriptionService;
        }

        public List<PlayerItem> Players { get; set; } = new();
        public bool CanSeePhone { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
            var currentUserId = GetCurrentUserId();

            if (currentUserId > 0)
                CanSeePhone = await _subscriptionService.CanSeePhoneNumberAsync(currentUserId);

            var users = await _userService.GetSuggestedPlayersAsync(currentUserId, 12);

            Players = users.Select((u, idx) => new PlayerItem
            {
                UserId = u.UserID,
                FullName = u.FullName,
                SkillLevel = u.SkillLevel ?? "Intermediate",
                AvatarUrl = string.IsNullOrWhiteSpace(u.AvatarUrl)
                    ? "/images/avatar-default.png"
                    : u.AvatarUrl,
                TotalMatches = u.UserID % 30 + 5,
                Phone = CanSeePhone ? u.PhoneNumber : null
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
        }
    }
}
