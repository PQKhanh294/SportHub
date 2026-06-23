using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Hubs;
using SportHub.Services.Implementations;
using SportHub.Services.Interfaces;
using System.Security.Claims;

namespace SportHub.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatApiController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IFriendshipService _friendshipService;
        private readonly ApplicationDbContext _context;
        private readonly IMessageReportService _reportService;
        private readonly ChatModerationService _moderation;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly IAiChatService _aiChat;

        public ChatApiController(IChatService chatService, IFriendshipService friendshipService,
            ApplicationDbContext context, IMessageReportService reportService,
            ChatModerationService moderation, IHubContext<ChatHub> chatHub, IAiChatService aiChat)
        {
            _chatService = chatService;
            _friendshipService = friendshipService;
            _context = context;
            _reportService = reportService;
            _moderation = moderation;
            _chatHub = chatHub;
            _aiChat = aiChat;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

            var friends = await _friendshipService.GetFriendsAsync(userId);
            var summaries = await _chatService.GetConversationSummariesAsync(userId);

            var result = friends
                .OrderByDescending(f => summaries.TryGetValue(f.UserID, out var s) ? s.LastMsg?.CreatedAt : null)
                .Select(f =>
                {
                    summaries.TryGetValue(f.UserID, out var summary);
                    var lastMsg = summary.LastMsg;
                    string previewText = lastMsg == null ? "" :
                        lastMsg.IsDeleted ? "Tin nhắn đã bị xóa" :
                        lastMsg.MessageType == "Image" ? "Đã gửi một ảnh" :
                        lastMsg.MessageType == "MatchCard" ? "Đã chia sẻ thẻ trận" :
                        (lastMsg.Content.Length > 40 ? lastMsg.Content[..40] + "…" : lastMsg.Content);

                    return new
                    {
                        userId = f.UserID,
                        name = f.FullName,
                        avatarUrl = f.AvatarUrl,
                        lastMessage = previewText,
                        lastMessageAt = lastMsg?.CreatedAt.ToString("o"),
                        lastMessageType = lastMsg?.MessageType,
                        lastSenderId = lastMsg?.SenderID,
                        lastIsRead = lastMsg?.IsRead,
                        unreadCount = summary.UnreadCount
                    };
                })
                .ToList();

            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

            var count = await _chatService.GetUnreadMessageCountAsync(userId);
            return Ok(new { count });
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromQuery] int userId)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            await _chatService.MarkMessagesAsReadAsync(userId, myId);
            var count = await _chatService.GetUnreadMessageCountAsync(myId);
            // Notify the sender that their messages were read
            await _chatHub.Clients.Group($"chat:{userId}")
                .SendAsync("MessagesRead", new { conversationWith = myId });
            return Ok(new { count });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int userId)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            var msgs = await _chatService.GetChatHistoryAsync(myId, userId, 50);
            var result = msgs.Select(m => new
            {
                messageID = m.MessageID,
                senderID = m.SenderID,
                receiverID = m.ReceiverID,
                content = m.IsDeleted ? null : m.Content,
                createdAt = m.CreatedAt.ToString("o"),
                senderName = m.Sender?.FullName ?? "",
                messageType = m.IsDeleted ? "Deleted" : m.MessageType,
                imageUrl = m.IsDeleted ? null : m.ImageUrl,
                matchCardJson = m.IsDeleted ? null : m.MatchCardJson,
                isDeleted = m.IsDeleted,
                isPinned = m.IsPinned,
                isRead = m.IsRead,
                replyToMessageID = m.ReplyToMessageID,
                replyPreview = m.ReplyToMessage != null
                    ? new
                    {
                        id = m.ReplyToMessage.MessageID,
                        content = m.ReplyToMessage.IsDeleted ? "Tin nhắn đã bị xóa" : m.ReplyToMessage.Content
                    }
                    : null
            });
            return Ok(result);
        }

        // ---- Phase 2 endpoints ----

        [HttpPost("messages/{id}/react")]
        public async Task<IActionResult> ReactToMessage(int id, [FromQuery] string type)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            var msg = await _context.ChatMessages.FindAsync(id);
            if (msg == null) return NotFound();
            if (msg.IsDeleted) return BadRequest(new { error = "Không thể react tin nhắn đã bị xóa." });

            var (rtype, added) = await _chatService.ToggleReactionAsync(id, myId, type);
            var reactions = await _chatService.GetMessageReactionsAsync(id);
            return Ok(new { messageId = id, reactions, reactionType = rtype, added });
        }

        [HttpDelete("messages/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            await _chatService.DeleteMessageAsync(id, myId);
            return Ok(new { messageId = id });
        }

        [HttpPost("messages/{id}/pin")]
        public async Task<IActionResult> PinMessage(int id, [FromQuery] int withUserId)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            var isPinned = await _chatService.TogglePinMessageAsync(id, myId, withUserId);
            return Ok(new { messageId = id, isPinned });
        }

        [HttpPost("messages/{id}/forward")]
        public async Task<IActionResult> ForwardMessage(int id, [FromQuery] int toUserId)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            try
            {
                var msg = await _chatService.ForwardMessageAsync(id, myId, toUserId);
                return Ok(new { messageId = msg.MessageID });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("messages/{id}/report")]
        public async Task<IActionResult> ReportMessage(int id, [FromBody] ReportRequest req)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            var msg = await _context.ChatMessages.FindAsync(id);
            if (msg == null) return NotFound();

            var (score, rec, analysis) = await _moderation.AnalyzeMessageAsync(msg.Content);
            var report = await _reportService.CreateReportAsync(id, myId, req.Reason, analysis, score, rec);
            return Ok(new { reportId = report.ReportID, aiScore = score, aiRecommendation = rec });
        }

        [HttpGet("media")]
        public async Task<IActionResult> GetSharedMedia([FromQuery] int userId)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            var media = await _chatService.GetSharedMediaAsync(myId, userId);
            return Ok(media.Select(m => new { m.MessageID, m.ImageUrl, m.CreatedAt, m.SenderID }));
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] int userId, [FromQuery] string q)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            var msgs = await _chatService.SearchMessagesAsync(myId, userId, q);
            return Ok(msgs.Select(m => new
            {
                m.MessageID, m.Content, m.CreatedAt, m.SenderID,
                senderName = m.Sender?.FullName ?? ""
            }));
        }

        [HttpGet("pinned")]
        public async Task<IActionResult> GetPinnedMessage([FromQuery] int userId)
        {
            var myIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(myIdStr, out var myId)) return Unauthorized();

            var msg = await _chatService.GetPinnedMessageAsync(myId, userId);
            if (msg == null) return Ok(null);
            return Ok(new {
                msg.MessageID,
                content = msg.IsDeleted ? "Tin nhắn đã bị xóa" : msg.Content,
                msg.MessageType,
                msg.SenderID,
                senderName = msg.Sender?.FullName ?? ""
            });
        }

        [HttpGet("match-desc")]
        public async Task<IActionResult> GenerateMatchDescription([FromQuery] string? sport, [FromQuery] string? skill, [FromQuery] string? type, [FromQuery] string? date)
        {
            try
            {
                var systemPrompt = "Bạn là trợ lý thể thao. Viết mô tả ngắn gọn (2-3 câu tiếng Việt) cho một trận đấu. Chỉ trả về nội dung mô tả, không thêm gì khác.";
                var userMessage = $"Tạo mô tả trận đấu:\n- Môn: {sport ?? "thể thao"}\n- Trình độ: {skill ?? "mọi trình độ"}\n- Thể thức: {type ?? "thi đấu"}\n- Ngày: {date ?? "sắp tới"}";
                var description = await _aiChat.ChatAsync(systemPrompt, new List<AiChatHistoryItem>(), userMessage);
                return Ok(new { description = description?.Trim() });
            }
            catch
            {
                return StatusCode(500, new { error = "AI không phản hồi" });
            }
        }

        public class ReportRequest
        {
            public string? Reason { get; set; }
        }
    }
}
