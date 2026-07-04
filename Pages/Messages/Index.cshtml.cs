using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
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
        private readonly ApplicationDbContext _context;

        public IndexModel(IFriendshipService friendshipService, IChatService chatService, IUserService userService, ApplicationDbContext context)
        {
            _friendshipService = friendshipService;
            _chatService = chatService;
            _userService = userService;
            _context = context;
        }

        public List<User> Friends { get; set; } = new();
        public List<User> SuggestedFriends { get; set; } = new();
        public User? ActiveChatUser { get; set; }
        public List<ChatMessage> ChatHistory { get; set; } = new();
        public List<ChatBookingProposal> BookingProposals { get; set; } = new();
        public List<SelectListItem> SharedMatchOptions { get; set; } = new();
        public List<SelectListItem> CourtOptions { get; set; } = new();
        public int CurrentUserId { get; set; }
        public Dictionary<int, (ChatMessage? LastMsg, int UnreadCount)> ConversationSummaries { get; set; } = new();

        [BindProperty]
        public BookingProposalInput ProposalInput { get; set; } = new();

        public class BookingProposalInput
        {
            [Required]
            public int ReceiverId { get; set; }

            [Required]
            public int MatchId { get; set; }

            public int? CourtId { get; set; }

            [Required]
            public DateTime BookingDate { get; set; } = DateTime.Today;

            [Required]
            public TimeSpan StartTime { get; set; } = new(18, 0, 0);

            [Required]
            public TimeSpan EndTime { get; set; } = new(19, 0, 0);

            [Range(0, 1000000000)]
            public decimal? EstimatedCost { get; set; }

            public string SplitMode { get; set; } = "Equal";
            public string? Note { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? userId)
        {
            ViewData["ActivePage"] = "Messages";
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdStr, out int currentUserId)) return RedirectToPage("/Auth/Login");

            CurrentUserId = currentUserId;
            Friends = await _friendshipService.GetFriendsAsync(currentUserId);
            ConversationSummaries = await _chatService.GetConversationSummariesAsync(currentUserId);

            // Sort friends: those with messages first (newest last message at top)
            Friends = Friends
                .OrderByDescending(f => ConversationSummaries.TryGetValue(f.UserID, out var s) ? s.LastMsg?.CreatedAt : null)
                .ToList();

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
                        BookingProposals = await _chatService.GetBookingProposalsAsync(currentUserId, userId.Value);
                        await LoadBookingProposalOptionsAsync(currentUserId, userId.Value);
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

        public async Task<IActionResult> OnPostCreateProposalAsync()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0) return RedirectToPage("/Auth/Login");

            if (ProposalInput.EndTime <= ProposalInput.StartTime)
            {
                TempData["ErrorMessage"] = "Giá» káº¿t thÃºc pháº£i sau giá» báº¯t Ä‘áº§u.";
                return RedirectToPage(new { userId = ProposalInput.ReceiverId });
            }

            var isSharedMatch = await IsSharedAcceptedMatchAsync(currentUserId, ProposalInput.ReceiverId, ProposalInput.MatchId);
            if (!isSharedMatch)
            {
                TempData["ErrorMessage"] = "Chá»‰ cÃ³ thá»ƒ Ä‘á» xuáº¥t Ä‘áº·t sÃ¢n cho tráº­n mÃ  cáº£ hai Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t.";
                return RedirectToPage(new { userId = ProposalInput.ReceiverId });
            }

            var proposal = await _chatService.CreateBookingProposalAsync(new ChatBookingProposal
            {
                MatchID = ProposalInput.MatchId,
                SenderID = currentUserId,
                ReceiverID = ProposalInput.ReceiverId,
                CourtID = ProposalInput.CourtId,
                BookingDate = ProposalInput.BookingDate,
                StartTime = ProposalInput.StartTime,
                EndTime = ProposalInput.EndTime,
                EstimatedCost = ProposalInput.EstimatedCost,
                SplitMode = ProposalInput.SplitMode == "HostPays" ? "HostPays" : "Equal",
                Note = string.IsNullOrWhiteSpace(ProposalInput.Note) ? null : ProposalInput.Note.Trim()
            });

            var costText = proposal.EstimatedCost.HasValue ? $"{proposal.EstimatedCost.Value:N0} xu" : "chÆ°a chá»‘t giÃ¡";
            var splitText = proposal.SplitMode == "HostPays" ? "host tráº£" : "chia Ä‘á»u";
            await _chatService.SendMessageAsync(
                currentUserId,
                ProposalInput.ReceiverId,
                $"Äá» xuáº¥t Ä‘áº·t sÃ¢n: {proposal.BookingDate:dd/MM/yyyy} {proposal.StartTime:hh\\:mm}-{proposal.EndTime:hh\\:mm}, {costText}, {splitText}.");

            TempData["SuccessMessage"] = "ÄÃ£ gá»­i Ä‘á» xuáº¥t Ä‘áº·t sÃ¢n trong chat.";
            return RedirectToPage(new { userId = ProposalInput.ReceiverId });
        }

        public async Task<IActionResult> OnPostRespondProposalAsync(int proposalId, int userId, string status)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0) return RedirectToPage("/Auth/Login");

            var updated = await _chatService.RespondToBookingProposalAsync(proposalId, currentUserId, status);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? (status == "Accepted" ? "ÄÃ£ Ä‘á»“ng Ã½ Ä‘á» xuáº¥t Ä‘áº·t sÃ¢n." : "ÄÃ£ tá»« chá»‘i Ä‘á» xuáº¥t Ä‘áº·t sÃ¢n.")
                : "KhÃ´ng thá»ƒ cáº­p nháº­t Ä‘á» xuáº¥t nÃ y.";

            if (updated)
            {
                var message = status == "Accepted"
                    ? "MÃ¬nh Ä‘á»“ng Ã½ Ä‘á» xuáº¥t Ä‘áº·t sÃ¢n."
                    : "MÃ¬nh chÆ°a thá»ƒ theo Ä‘á» xuáº¥t Ä‘áº·t sÃ¢n nÃ y.";
                await _chatService.SendMessageAsync(currentUserId, userId, message);
            }

            return RedirectToPage(new { userId });
        }

        private int GetCurrentUserId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : 0;
        }

        private async Task LoadBookingProposalOptionsAsync(int currentUserId, int activeUserId)
        {
            var matches = await _context.Matches
                .Include(m => m.Sport)
                .Include(m => m.Participants)
                .Where(m => m.MatchDate >= DateTime.Today
                    && m.Participants.Any(p => p.UserID == currentUserId && p.JoinStatus == "Accepted")
                    && m.Participants.Any(p => p.UserID == activeUserId && p.JoinStatus == "Accepted"))
                .OrderBy(m => m.MatchDate)
                .ThenBy(m => m.StartTime)
                .Take(10)
                .ToListAsync();

            SharedMatchOptions = matches.Select(m => new SelectListItem
            {
                Value = m.MatchID.ToString(),
                Text = $"{(string.IsNullOrWhiteSpace(m.Title) ? m.MatchType : m.Title)} - {m.MatchDate:dd/MM} {m.StartTime:hh\\:mm}"
            }).ToList();

            CourtOptions = await _context.Courts
                .Include(c => c.Venue)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Venue.VenueName)
                .ThenBy(c => c.CourtName)
                .Take(30)
                .Select(c => new SelectListItem
                {
                    Value = c.CourtID.ToString(),
                    Text = c.Venue.VenueName + " - " + c.CourtName
                })
                .ToListAsync();
        }

        private async Task<bool> IsSharedAcceptedMatchAsync(int currentUserId, int activeUserId, int matchId)
        {
            return await _context.Matches
                .Where(m => m.MatchID == matchId)
                .AnyAsync(m => m.Participants.Any(p => p.UserID == currentUserId && p.JoinStatus == "Accepted")
                    && m.Participants.Any(p => p.UserID == activeUserId && p.JoinStatus == "Accepted"));
        }
    }
}
