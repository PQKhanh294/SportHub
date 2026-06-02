using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using System.Security.Claims;

namespace SportHub.Pages.Users
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IFriendshipService _friendshipService;
        private readonly ApplicationDbContext _context;

        public IndexModel(IUserService userService, IFriendshipService friendshipService, ApplicationDbContext context)
        {
            _userService = userService;
            _friendshipService = friendshipService;
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SkillLevel { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sport { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; } // "newest", "distance"

        public List<UserCardViewModel> Users { get; set; } = new();
        public bool HasLocation { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var currentUserId)) return RedirectToPage("/Account/Login");

            var currentUser = await _userService.GetUserByIdAsync(currentUserId);
            HasLocation = currentUser?.DefaultLatitude.HasValue == true && currentUser?.DefaultLongitude.HasValue == true;

            var rawUsers = await _userService.SearchUsersAsync(currentUserId, Keyword, SkillLevel, Sport, SortBy);

            // Fetch friendships for current user
            var friendships = await _context.Friendships
                .Where(f => f.SenderID == currentUserId || f.ReceiverID == currentUserId)
                .ToListAsync();

            foreach (var u in rawUsers)
            {
                var f = friendships.FirstOrDefault(x => (x.SenderID == currentUserId && x.ReceiverID == u.UserID) || 
                                                        (x.ReceiverID == currentUserId && x.SenderID == u.UserID));

                string relationStatus = "None";
                if (f != null)
                {
                    if (f.Status == "Accepted") relationStatus = "Friend";
                    else if (f.Status == "Pending" && f.SenderID == currentUserId) relationStatus = "Sent";
                    else if (f.Status == "Pending" && f.ReceiverID == currentUserId) relationStatus = "Received";
                }

                double? distance = null;
                if (HasLocation && u.DefaultLatitude.HasValue && u.DefaultLongitude.HasValue)
                {
                    distance = CalculateHaversineDistance((double)currentUser!.DefaultLatitude!.Value, (double)currentUser.DefaultLongitude!.Value, 
                                                          (double)u.DefaultLatitude.Value, (double)u.DefaultLongitude.Value);
                }

                Users.Add(new UserCardViewModel
                {
                    UserId = u.UserID,
                    FullName = u.FullName,
                    AvatarUrl = u.AvatarUrl,
                    SkillLevel = u.SkillLevel ?? "Chưa rõ",
                    FavoriteSport = u.FavoriteSport ?? "Chưa rõ",
                    DistanceKm = distance,
                    RelationStatus = relationStatus
                });
            }

            return Page();
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371d; 
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            return R * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }

    public class UserCardViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string SkillLevel { get; set; } = string.Empty;
        public string FavoriteSport { get; set; } = string.Empty;
        public double? DistanceKm { get; set; }
        public string RelationStatus { get; set; } = "None"; // None, Sent, Received, Friend
    }
}
