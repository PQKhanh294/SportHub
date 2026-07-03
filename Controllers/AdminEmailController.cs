using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportHub.Services.Interfaces;

namespace SportHub.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    public class AdminEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public AdminEmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // Chẩn đoán Resend: gửi mail thử và trả nguyên văn phản hồi API.
        // Domain chưa verify → Resend trả 403 kèm message rõ ràng trong detail.
        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail([FromQuery] string? to)
        {
            var target = string.IsNullOrWhiteSpace(to)
                ? User.FindFirstValue(ClaimTypes.Email)
                : to.Trim();
            if (string.IsNullOrWhiteSpace(target))
                return BadRequest(new { success = false, detail = "Không xác định được địa chỉ nhận." });

            var (success, detail) = await _emailService.SendTestAsync(target);
            return Ok(new { success, to = target, detail });
        }
    }
}
