using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace SportHub.Pages.Messages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IFriendshipService _friendshipService;
        private readonly IChatService _chatService;
        private readonly IUserService _userService;

        public IndexModel(IFriendshipService friendshipService, IChatService chatService, IUserService userService)
        {
            _friendshipService = friendshipService;
            _chatService = chatService;
            _userService = userService;
        }

        public List<User> Friends { get; set; } = new();
        public List<User> SuggestedFriends { get; set; } = new();
        public User? ActiveChatUser { get; set; }
        public List<ChatMessage> ChatHistory { get; set; } = new();
        public int CurrentUserId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? userId)
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdStr, out int currentUserId)) return RedirectToPage("/Auth/Login");

            CurrentUserId = currentUserId;
            Friends = await _friendshipService.GetFriendsAsync(currentUserId);
            
            if (!Friends.Any())
            {
                SuggestedFriends = await _userService.GetSuggestedPlayersAsync(currentUserId, 5);
            }

            if (userId.HasValue)
            {
                // Check if they are friends
                if (Friends.Any(f => f.UserID == userId.Value))
                {
                    ActiveChatUser = await _userService.GetUserByIdAsync(userId.Value);
                    if (ActiveChatUser != null)
                    {
                        ChatHistory = await _chatService.GetChatHistoryAsync(currentUserId, userId.Value);
                        await _chatService.MarkMessagesAsReadAsync(userId.Value, currentUserId);
                    }
                }
                else
                {
                    return RedirectToPage("/Users/Friends"); // Not friends
                }
            }

            return Page();
        }
    }
}
