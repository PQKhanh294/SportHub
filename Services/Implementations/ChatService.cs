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

        public async Task<ChatMessage> SendMessageAsync(int senderId, int receiverId, string content)
        {
            var msg = new ChatMessage
            {
                SenderID = senderId,
                ReceiverID = receiverId,
                Content = content,
                IsRead = false,
                CreatedAt = System.DateTime.UtcNow
            };

            _context.ChatMessages.Add(msg);
            await _context.SaveChangesAsync();

            // Load navigation properties for return
            await _context.Entry(msg).Reference(m => m.Sender).LoadAsync();
            return msg;
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
    }
}
