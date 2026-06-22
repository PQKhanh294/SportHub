using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SportHub.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userIdRaw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdRaw, out var userId) && userId > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
                if (Context.User!.IsInRole("Admin"))
                    await Groups.AddToGroupAsync(Context.ConnectionId, "role:Admin");
            }

            await base.OnConnectedAsync();
        }
    }
}

