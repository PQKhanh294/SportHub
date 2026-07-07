using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportHub.Services.Interfaces;

namespace SportHub.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public class NotificationApiController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationApiController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("mark-read/{id}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();

            await _notificationService.MarkReadAsync(id, userId);
            return Ok();
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();

            await _notificationService.MarkAllReadAsync(userId);
            return Ok();
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
