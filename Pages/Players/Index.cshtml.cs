using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Players
{
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;

        public IndexModel(IUserService userService)
        {
            _userService = userService;
        }

        public List<PlayerItem> Players { get; set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
            var currentUserId = GetCurrentUserId();

            var users = await _userService.GetSuggestedPlayersAsync(currentUserId, 12);

            Players = users.Select((u, idx) => new PlayerItem
            {
                UserId = u.UserID,
                FullName = u.FullName,
                SkillLevel = u.SkillLevel ?? "Intermediate",
                AvatarUrl = string.IsNullOrWhiteSpace(u.AvatarUrl)
                    ? "/images/avatar-default.png"
                    : u.AvatarUrl,
                TotalMatches = u.UserID % 30 + 5
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
        }
    }
}
