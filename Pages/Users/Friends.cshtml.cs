using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SportHub.Pages.Users
{
    [Authorize]
    public class FriendsModel : PageModel
    {
        private readonly IFriendshipService _friendshipService;

        public FriendsModel(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        public List<User> Friends { get; set; } = new();
        public List<Friendship> PendingRequests { get; set; } = new();

        public async Task OnGetAsync()
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                Friends = await _friendshipService.GetFriendsAsync(currentUserId);
                PendingRequests = await _friendshipService.GetPendingRequestsAsync(currentUserId);
            }
        }

        public async Task<IActionResult> OnPostAcceptAsync(int senderId)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                await _friendshipService.AcceptFriendRequestAsync(currentUserId, senderId);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeclineAsync(int senderId)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId))
            {
                await _friendshipService.DeclineFriendRequestAsync(currentUserId, senderId);
            }
            return RedirectToPage();
        }
    }
}
