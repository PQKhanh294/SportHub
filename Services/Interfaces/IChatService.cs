using SportHub.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SportHub.Services.Interfaces
{
    public interface IChatService
    {
        Task<List<ChatMessage>> GetChatHistoryAsync(int userId1, int userId2, int limit = 50);
        Task<ChatMessage> SendMessageAsync(int senderId, int receiverId, string content);
        Task MarkMessagesAsReadAsync(int senderId, int receiverId);
        Task<int> GetUnreadMessageCountAsync(int userId);
    }
}
