using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace SportHub.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ApplicationDbContext _context;

        public ChatHub(IChatService chatService, ApplicationDbContext context)
        {
            _chatService = chatService;
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:{userIdStr}");
                if (int.TryParse(userIdStr, out var uid))
                {
                    var user = await _context.Users.FindAsync(uid);
                    if (user?.IsBanned == true && (user.BanEndAt == null || user.BanEndAt > DateTime.UtcNow))
                    { Context.Abort(); return; }
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{userId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(int receiverId, string content, int? replyToMessageId = null)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId)) return;

            var msg = await _chatService.SendMessageAsync(senderId, receiverId, content, "Text",
                replyToMessageId: replyToMessageId);
            var payload = BuildPayload(msg);

            await Clients.Group($"chat:{receiverId}").SendAsync("ReceiveMessage", payload);
            await Clients.Group($"chat:{senderId}").SendAsync("MessageSent", payload);

        }

        public async Task SendImage(int receiverId, string imageUrl)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId)) return;
            if (string.IsNullOrWhiteSpace(imageUrl)) return;

            var msg = await _chatService.SendMessageAsync(senderId, receiverId, "[Ảnh]", "Image", imageUrl);
            var payload = BuildPayload(msg);

            await Clients.Group($"chat:{receiverId}").SendAsync("ReceiveMessage", payload);
            await Clients.Group($"chat:{senderId}").SendAsync("MessageSent", payload);
        }

        public async Task SendMatchCard(int receiverId, int matchId)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId)) return;

            var match = await _context.Matches
                .Include(m => m.Participants)
                .Include(m => m.Sport)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);
            if (match == null) return;

            var participantCount = match.Participants.Count(p => p.JoinStatus == "Accepted");
            var matchCardData = new
            {
                id = match.MatchID,
                title = match.Title ?? match.MatchType,
                type = match.MatchType,
                sport = match.Sport?.SportName ?? "",
                date = match.MatchDate.ToString("dd/MM/yyyy"),
                time = match.StartTime.ToString(@"hh\:mm"),
                venue = match.CustomCourtName ?? match.CustomCourtAddress ?? "",
                price = match.CustomPriceVnd.HasValue ? $"{match.CustomPriceVnd.Value:N0} VND" : "Thỏa thuận",
                participants = participantCount,
                max = match.MaxParticipants,
                skill = match.SkillRequired ?? "Mọi cấp độ",
                status = match.Status
            };

            var matchCardJson = JsonSerializer.Serialize(matchCardData);
            var msg = await _chatService.SendMessageAsync(senderId, receiverId, "[Thẻ trận đấu]", "MatchCard", null, matchCardJson);
            var payload = BuildPayload(msg);

            await Clients.Group($"chat:{receiverId}").SendAsync("ReceiveMessage", payload);
            await Clients.Group($"chat:{senderId}").SendAsync("MessageSent", payload);
        }

        // Read receipt: caller marks all messages from senderId as read
        public async Task MarkMessagesRead(int senderId)
        {
            var readerIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(readerIdStr, out int readerId)) return;

            var now = DateTime.UtcNow;
            var updated = await _context.ChatMessages
                .Where(m => m.SenderID == senderId && m.ReceiverID == readerId && !m.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.IsRead, true)
                    .SetProperty(m => m.ReadAt, now));

            if (updated > 0)
            {
                // Notify sender that reader has read messages
                await Clients.Group($"chat:{senderId}").SendAsync("MessagesRead", new
                {
                    readerId,
                    updatedCount = updated
                });
            }
        }

        // ---- Phase 2 Hub methods ----

        public async Task TypingStart(int receiverId)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId)) return;
            await Clients.Group($"chat:{receiverId}").SendAsync("UserTyping", senderId);
        }

        public async Task TypingStop(int receiverId)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId)) return;
            await Clients.Group($"chat:{receiverId}").SendAsync("UserStoppedTyping", senderId);
        }

        public async Task ReactToMessage(int messageId, int receiverId, string reactionType)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId)) return;

            var (rtype, added) = await _chatService.ToggleReactionAsync(messageId, senderId, reactionType);
            var reactions = await _chatService.GetMessageReactionsAsync(messageId);

            var payload = new { messageId, reactions, reactorId = senderId, reactionType = rtype, added };
            await Clients.Group($"chat:{receiverId}").SendAsync("ReactionUpdated", payload);
            await Clients.Group($"chat:{senderId}").SendAsync("ReactionUpdated", payload);
        }

        public async Task DeleteMessage(int messageId, int receiverId)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId)) return;

            await _chatService.DeleteMessageAsync(messageId, senderId);

            var payload = new { messageId };
            await Clients.Group($"chat:{receiverId}").SendAsync("MessageDeleted", payload);
            await Clients.Group($"chat:{senderId}").SendAsync("MessageDeleted", payload);
        }

        public async Task PinMessage(int messageId, int receiverId)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId)) return;

            var isPinned = await _chatService.TogglePinMessageAsync(messageId, senderId, receiverId);
            var msg = await _context.ChatMessages.FindAsync(messageId);

            var payload = new { messageId, isPinned, previewText = msg?.Content ?? "" };
            await Clients.Group($"chat:{receiverId}").SendAsync("MessagePinned", payload);
            await Clients.Group($"chat:{senderId}").SendAsync("MessagePinned", payload);
        }

        public async Task ForwardMessage(int originalMessageId, int receiverId)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(senderIdStr, out int senderId)) return;

            try
            {
                var msg = await _chatService.ForwardMessageAsync(originalMessageId, senderId, receiverId);
                var payload = BuildPayload(msg);
                await Clients.Group($"chat:{receiverId}").SendAsync("ReceiveMessage", payload);
                await Clients.Group($"chat:{senderId}").SendAsync("MessageSent", payload);
            }
            catch (InvalidOperationException ex)
            {
                await Clients.Caller.SendAsync("ForwardError", new { error = ex.Message });
            }
        }

        private static object BuildPayload(ChatMessage msg)
        {
            var isDeleted = msg.IsDeleted;
            string? replyContent = null;
            if (msg.ReplyToMessage != null)
                replyContent = msg.ReplyToMessage.IsDeleted ? "Tin nhắn đã bị xóa" : msg.ReplyToMessage.Content;

            return new
            {
                messageID = msg.MessageID,
                senderID = msg.SenderID,
                receiverID = msg.ReceiverID,
                content = isDeleted ? null : msg.Content,
                createdAt = msg.CreatedAt.ToString("o"),
                senderName = msg.Sender?.FullName ?? "",
                messageType = isDeleted ? "Deleted" : msg.MessageType,
                imageUrl = isDeleted ? null : msg.ImageUrl,
                matchCardJson = isDeleted ? null : msg.MatchCardJson,
                isDeleted,
                replyToMessageID = msg.ReplyToMessageID,
                replyPreview = msg.ReplyToMessage != null
                    ? new { id = msg.ReplyToMessage.MessageID, content = replyContent }
                    : null
            };
        }
    }
}
