using SportHub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using Microsoft.AspNetCore.SignalR;
using SportHub.Hubs;

namespace SportHub.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateAsync(int userId, string type, string title, string message, string? linkUrl = null);
        Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 30);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAllReadAsync(int userId);
        Task MarkReadAsync(int notificationId, int userId);
    }
}

namespace SportHub.Services.Implementations
{
    public class NotificationService : Interfaces.INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateAsync(int userId, string type, string title, string message, string? linkUrl = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserID = userId,
                Type = type,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserID == userId && !n.IsRead);

            await _hubContext.Clients.Group($"user:{userId}")
                .SendAsync("notification_received", new
                {
                    type,
                    title,
                    message,
                    linkUrl,
                    createdAt = DateTime.UtcNow,
                    unreadCount
                });
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 30)
        {
            return await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserID == userId && !n.IsRead);
        }

        public async Task MarkAllReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();
            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"user:{userId}")
                .SendAsync("notifications_read", new { unreadCount = 0 });
        }

        public async Task MarkReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == notificationId && n.UserID == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserID == userId && !n.IsRead);
                await _hubContext.Clients.Group($"user:{userId}")
                    .SendAsync("notifications_read", new { unreadCount });
            }
        }
    }
}
