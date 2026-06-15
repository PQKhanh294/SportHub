using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SportHub.Pages.Users
{
    public class ProfileModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IFriendshipService _friendshipService;
        private readonly IBadgeService _badgeService;

        public ProfileModel(IUserService userService, IFriendshipService friendshipService, IBadgeService badgeService)
        {
            _userService = userService;
            _friendshipService = friendshipService;
            _badgeService = badgeService;
        }

        public User TargetUser { get; set; } = null!;
        public string FriendshipStatus { get; set; } = "None"; // None, Pending, Accepted
        public bool IsSender { get; set; } = false;
        public List<BadgeProgressItem> Badges { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var target = await _userService.GetUserByIdAsync(id);
            if (target == null) return NotFound();

            TargetUser = target;
            await _badgeService.SyncEarnedBadgesAsync(id);
            Badges = await _badgeService.GetBadgeProgressAsync(id);

            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId) && currentUserId != id)
            {
                var friendship = await _friendshipService.GetFriendshipAsync(currentUserId, id);
                if (friendship != null)
                {
                    FriendshipStatus = friendship.Status;
                    IsSender = friendship.SenderID == currentUserId;
                }
            }
            else
            {
                FriendshipStatus = "Self";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddFriendAsync(int id)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                await _friendshipService.SendFriendRequestAsync(currentUserId, id);
            }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostCancelRequestAsync(int id)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                await _friendshipService.CancelFriendRequestAsync(currentUserId, id);
            }
            return RedirectToPage(new { id });
        }
        
        public async Task<IActionResult> OnPostAcceptRequestAsync(int id)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                await _friendshipService.AcceptFriendRequestAsync(currentUserId, id);
            }
            return RedirectToPage(new { id });
        }
        
        public async Task<IActionResult> OnPostDeclineRequestAsync(int id)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                await _friendshipService.DeclineFriendRequestAsync(currentUserId, id);
            }
            return RedirectToPage(new { id });
        }
        
        public async Task<IActionResult> OnPostUnfriendAsync(int id)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                await _friendshipService.UnfriendAsync(currentUserId, id);
            }
            return RedirectToPage(new { id });
        }
    }
}
