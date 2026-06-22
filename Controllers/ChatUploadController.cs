using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SportHub.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public ChatUploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Không có file." });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "Ảnh quá lớn. Tối đa 5MB." });

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { error = "Định dạng không hỗ trợ." });

            var fileName = $"{userIdStr}_{Guid.NewGuid():N}{ext}";
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "chat");
            Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            return Ok(new { url = $"/uploads/chat/{fileName}" });
        }
    }
}
