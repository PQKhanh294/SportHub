using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportHub.Services.Implementations
{
    public class FriendshipService : IFriendshipService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public FriendshipService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<Friendship?> GetFriendshipAsync(int userId1, int userId2)
        {
            return await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.SenderID == userId1 && f.ReceiverID == userId2) ||
                    (f.SenderID == userId2 && f.ReceiverID == userId1));
        }

        public async Task<List<User>> GetFriendsAsync(int userId)
        {
            var friendships = await _context.Friendships
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .Where(f => (f.SenderID == userId || f.ReceiverID == userId) && f.Status == "Accepted")
                .ToListAsync();

            var friends = friendships.Select(f => f.SenderID == userId ? f.Receiver : f.Sender).ToList();
            return friends;
        }

        public async Task<List<Friendship>> GetPendingRequestsAsync(int userId)
        {
            return await _context.Friendships
                .Include(f => f.Sender)
                .Where(f => f.ReceiverID == userId && f.Status == "Pending")
                .ToListAsync();
        }

        public async Task<bool> SendFriendRequestAsync(int senderId, int receiverId)
        {
            if (senderId == receiverId) return false;

            var existing = await GetFriendshipAsync(senderId, receiverId);
            if (existing != null) return false;

            var friendship = new Friendship
            {
                SenderID = senderId,
                ReceiverID = receiverId,
                Status = "Pending"
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            var sender = await _context.Users.FindAsync(senderId);
            string senderName = sender?.FullName ?? "Ai đó";
            await _notificationService.CreateAsync(
                userId: receiverId,
                type: "System",
                title: "Lời mời kết bạn",
                message: $"{senderName} đã gửi cho bạn một lời mời kết bạn.",
                linkUrl: "/Users/Friends"
            );

            return true;
        }

        public async Task<bool> AcceptFriendRequestAsync(int receiverId, int senderId)
        {
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.SenderID == senderId && f.ReceiverID == receiverId && f.Status == "Pending");

            if (friendship == null) return false;

            friendship.Status = "Accepted";
            friendship.UpdatedAt = System.DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            var receiver = await _context.Users.FindAsync(receiverId);
            string receiverName = receiver?.FullName ?? "Ai đó";
            await _notificationService.CreateAsync(
                userId: senderId,
                type: "System",
                title: "Kết bạn thành công",
                message: $"{receiverName} đã chấp nhận lời mời kết bạn của bạn.",
                linkUrl: $"/Users/Profile/{receiverId}"
            );

            return true;
        }

        public async Task<bool> DeclineFriendRequestAsync(int receiverId, int senderId)
        {
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.SenderID == senderId && f.ReceiverID == receiverId && f.Status == "Pending");

            if (friendship == null) return false;

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelFriendRequestAsync(int senderId, int receiverId)
        {
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.SenderID == senderId && f.ReceiverID == receiverId && f.Status == "Pending");

            if (friendship == null) return false;

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfriendAsync(int userId1, int userId2)
        {
            var friendship = await GetFriendshipAsync(userId1, userId2);
            if (friendship == null) return false;

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
