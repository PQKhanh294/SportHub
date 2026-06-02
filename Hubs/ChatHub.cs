using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SportHub.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SportHub.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly INotificationService _notificationService;
        
        // Cần lưu connectionId của user đang online để gửi trực tiếp (1-1)
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();

        public ChatHub(IChatService chatService, INotificationService notificationService)
        {
            _chatService = chatService;
            _notificationService = notificationService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections[userId] = Context.ConnectionId;
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections.TryRemove(userId, out _);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(int receiverId, string content)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(senderIdStr, out int senderId))
            {
                // Lưu vào database
                var msg = await _chatService.SendMessageAsync(senderId, receiverId, content);

                // Nếu receiver đang online thì bắn socket sang
                if (UserConnections.TryGetValue(receiverId.ToString(), out string receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", new
                    {
                        MessageID = msg.MessageID,
                        SenderID = msg.SenderID,
                        ReceiverID = msg.ReceiverID,
                        Content = msg.Content,
                        CreatedAt = msg.CreatedAt.ToString("o"),
                        SenderName = msg.Sender.FullName
                    });
                }

                // Gửi thông báo hệ thống và lưu vào DB
                await _notificationService.CreateAsync(
                    userId: receiverId,
                    type: "Chat",
                    title: msg.Sender.FullName,
                    message: msg.Content,
                    linkUrl: $"/Messages/Index?userId={msg.SenderID}"
                );
                
                // Đồng thời gửi lại cho sender để hiện thị lên UI (nếu mở nhiều tab)
                await Clients.Caller.SendAsync("MessageSent", new
                {
                    MessageID = msg.MessageID,
                    SenderID = msg.SenderID,
                    ReceiverID = msg.ReceiverID,
                    Content = msg.Content,
                    CreatedAt = msg.CreatedAt.ToString("o"),
                    SenderName = msg.Sender.FullName
                });
            }
        }
    }
}
