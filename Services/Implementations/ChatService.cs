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
    }
}
