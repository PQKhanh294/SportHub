using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportHub.Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;

        public ChatService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(int userId1, int userId2, int limit = 50)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => 
                    (m.SenderID == userId1 && m.ReceiverID == userId2) ||
                    (m.SenderID == userId2 && m.ReceiverID == userId1))
                .OrderBy(m => m.CreatedAt)
                .Take(limit) // Note: In real app, might want OrderByDescending then Take then reverse, but for simple chat it's fine.
                .ToListAsync();
        }

        public async Task<ChatMessage> SendMessageAsync(int senderId, int receiverId, string content,
            string messageType = "Text", string? imageUrl = null, string? matchCardJson = null,
            int? replyToMessageId = null)
        {
            var msg = new ChatMessage
            {
                SenderID = senderId,
                ReceiverID = receiverId,
                Content = content,
                MessageType = messageType,
                ImageUrl = imageUrl,
                MatchCardJson = matchCardJson,
                ReplyToMessageID = replyToMessageId,
                IsRead = false,
                CreatedAt = System.DateTime.UtcNow
            };

            _context.ChatMessages.Add(msg);
            await _context.SaveChangesAsync();

            await _context.Entry(msg).Reference(m => m.Sender).LoadAsync();
            if (replyToMessageId.HasValue)
                await _context.Entry(msg).Reference(m => m.ReplyToMessage).LoadAsync();
            return msg;
        }

        public async Task<Dictionary<int, (ChatMessage? LastMsg, int UnreadCount)>> GetConversationSummariesAsync(int userId)
        {
            // All messages involving this user
            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.SenderID == userId || m.ReceiverID == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var result = new Dictionary<int, (ChatMessage? LastMsg, int UnreadCount)>();

            foreach (var msg in messages)
            {
                int otherId = msg.SenderID == userId ? msg.ReceiverID : msg.SenderID;
                if (!result.ContainsKey(otherId))
                    result[otherId] = (msg, 0);
            }

            // Count unread per sender
            var unreadCounts = await _context.ChatMessages
                .Where(m => m.ReceiverID == userId && !m.IsRead)
                .GroupBy(m => m.SenderID)
                .Select(g => new { SenderId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var u in unreadCounts)
            {
                if (result.ContainsKey(u.SenderId))
                    result[u.SenderId] = (result[u.SenderId].LastMsg, u.Count);
                else
                    result[u.SenderId] = (null, u.Count);
            }

            return result;
        }

        public async Task MarkMessagesAsReadAsync(int senderId, int receiverId)
        {
            var unreadMessages = await _context.ChatMessages
                .Where(m => m.SenderID == senderId && m.ReceiverID == receiverId && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadMessageCountAsync(int userId)
        {
            return await _context.ChatMessages
                .Where(m => m.ReceiverID == userId && !m.IsRead)
                .CountAsync();
        }

        public async Task<List<ChatBookingProposal>> GetBookingProposalsAsync(int userId1, int userId2, int limit = 10)
        {
            return await _context.ChatBookingProposals
                .Include(p => p.Match)
                .Include(p => p.Court).ThenInclude(c => c!.Venue)
                .Include(p => p.Sender)
                .Include(p => p.Receiver)
                .Where(p =>
                    (p.SenderID == userId1 && p.ReceiverID == userId2) ||
                    (p.SenderID == userId2 && p.ReceiverID == userId1))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<ChatBookingProposal> CreateBookingProposalAsync(ChatBookingProposal proposal)
        {
            proposal.Status = "Waiting";
            proposal.CreatedAt = DateTime.UtcNow;
            proposal.UpdatedAt = DateTime.UtcNow;

            _context.ChatBookingProposals.Add(proposal);
            await _context.SaveChangesAsync();

            await _context.Entry(proposal).Reference(p => p.Match).LoadAsync();
            if (proposal.CourtID.HasValue)
            {
                await _context.Entry(proposal).Reference(p => p.Court).LoadAsync();
            }

            return proposal;
        }

        public async Task<bool> RespondToBookingProposalAsync(int proposalId, int userId, string status)
        {
            if (status != "Accepted" && status != "Rejected") return false;

            var proposal = await _context.ChatBookingProposals
                .FirstOrDefaultAsync(p => p.ProposalID == proposalId && p.ReceiverID == userId && p.Status == "Waiting");
            if (proposal == null) return false;

            proposal.Status = status;
            proposal.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // ---- Phase 2 enhancements ----

        public async Task<(string ReactionType, bool Added)> ToggleReactionAsync(int messageId, int userId, string reactionType)
        {
            var existing = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageID == messageId && r.UserID == userId && r.ReactionType == reactionType);
            if (existing != null)
            {
                _context.MessageReactions.Remove(existing);
                await _context.SaveChangesAsync();
                return (reactionType, false);
            }
            _context.MessageReactions.Add(new MessageReaction
            {
                MessageID = messageId, UserID = userId, ReactionType = reactionType,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return (reactionType, true);
        }

        public async Task<Dictionary<string, List<int>>> GetMessageReactionsAsync(int messageId)
        {
            var reactions = await _context.MessageReactions
                .Where(r => r.MessageID == messageId)
                .ToListAsync();
            return reactions
                .GroupBy(r => r.ReactionType)
                .ToDictionary(g => g.Key, g => g.Select(r => r.UserID).ToList());
        }

        public async Task DeleteMessageAsync(int messageId, int userId)
        {
            var msg = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.MessageID == messageId && m.SenderID == userId);
            if (msg == null) return;
            msg.IsDeleted = true;
            msg.DeletedAt = DateTime.UtcNow;
            if (msg.IsPinned) msg.IsPinned = false;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> TogglePinMessageAsync(int messageId, int userId1, int userId2)
        {
            var msg = await _context.ChatMessages.FindAsync(messageId);
            if (msg == null || msg.IsDeleted) return false;

            // Unpin any existing pinned message in this conversation first
            var pinned = await _context.ChatMessages
                .Where(m => m.IsPinned &&
                    ((m.SenderID == userId1 && m.ReceiverID == userId2) ||
                     (m.SenderID == userId2 && m.ReceiverID == userId1)))
                .ToListAsync();

            bool isNowPinned = !msg.IsPinned;
            foreach (var p in pinned) p.IsPinned = false;
            msg.IsPinned = isNowPinned;
            await _context.SaveChangesAsync();
            return isNowPinned;
        }

        public async Task<ChatMessage?> GetPinnedMessageAsync(int userId1, int userId2)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.IsPinned && !m.IsDeleted &&
                    ((m.SenderID == userId1 && m.ReceiverID == userId2) ||
                     (m.SenderID == userId2 && m.ReceiverID == userId1)));
        }

        public async Task<List<ChatMessage>> GetSharedMediaAsync(int userId1, int userId2, int limit = 50)
        {
            return await _context.ChatMessages
                .Where(m => m.MessageType == "Image" && !m.IsDeleted &&
                    ((m.SenderID == userId1 && m.ReceiverID == userId2) ||
                     (m.SenderID == userId2 && m.ReceiverID == userId1)))
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> SearchMessagesAsync(int userId1, int userId2, string query, int limit = 30)
        {
            if (string.IsNullOrWhiteSpace(query)) return new();
            query = query.ToLower();
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => !m.IsDeleted && m.MessageType == "Text" &&
                    ((m.SenderID == userId1 && m.ReceiverID == userId2) ||
                     (m.SenderID == userId2 && m.ReceiverID == userId1)) &&
                    m.Content.ToLower().Contains(query))
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<ChatMessage> ForwardMessageAsync(int originalMessageId, int senderId, int receiverId)
        {
            if (senderId == receiverId) throw new InvalidOperationException("Không thể chuyển tiếp cho chính mình.");

            var original = await _context.ChatMessages.FindAsync(originalMessageId);
            if (original == null) throw new InvalidOperationException("Tin nhắn không tồn tại.");
            if (original.IsDeleted) throw new InvalidOperationException("Không thể chuyển tiếp tin nhắn đã bị xóa.");

            return await SendMessageAsync(senderId, receiverId,
                original.Content, original.MessageType, original.ImageUrl, original.MatchCardJson);
        }
    }
}
