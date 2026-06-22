using SportHub.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SportHub.Services.Interfaces
{
    public interface IChatService
    {
        Task<List<ChatMessage>> GetChatHistoryAsync(int userId1, int userId2, int limit = 50);
        Task<ChatMessage> SendMessageAsync(int senderId, int receiverId, string content,
            string messageType = "Text", string? imageUrl = null, string? matchCardJson = null,
            int? replyToMessageId = null);
        Task MarkMessagesAsReadAsync(int senderId, int receiverId);
        Task<int> GetUnreadMessageCountAsync(int userId);
        Task<Dictionary<int, (ChatMessage? LastMsg, int UnreadCount)>> GetConversationSummariesAsync(int userId);
        Task<List<ChatBookingProposal>> GetBookingProposalsAsync(int userId1, int userId2, int limit = 10);
        Task<ChatBookingProposal> CreateBookingProposalAsync(ChatBookingProposal proposal);
        Task<bool> RespondToBookingProposalAsync(int proposalId, int userId, string status);

        // Phase 2 enhancements
        Task<(string ReactionType, bool Added)> ToggleReactionAsync(int messageId, int userId, string reactionType);
        Task<Dictionary<string, List<int>>> GetMessageReactionsAsync(int messageId);
        Task DeleteMessageAsync(int messageId, int userId);
        Task<bool> TogglePinMessageAsync(int messageId, int userId1, int userId2);
        Task<ChatMessage?> GetPinnedMessageAsync(int userId1, int userId2);
        Task<List<ChatMessage>> GetSharedMediaAsync(int userId1, int userId2, int limit = 50);
        Task<List<ChatMessage>> SearchMessagesAsync(int userId1, int userId2, string query, int limit = 30);
        Task<ChatMessage> ForwardMessageAsync(int originalMessageId, int senderId, int receiverId);
    }
}
