using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;

namespace SportHub.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/profile")]
    public class ProfileApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("meta")]
        public async Task<IActionResult> GetMeta()
        {
            var sports = await _context.Sports
                .OrderBy(s => s.SportName)
                .Select(s => new { id = s.SportID, name = s.SportName })
                .ToListAsync();
            return Ok(new { sports });
        }

        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] CompleteProfileRequest req)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (userId <= 0) return Unauthorized();

            var validSports = (req.Sports ?? new()).Where(s => s.SportId > 0 && !string.IsNullOrWhiteSpace(s.SkillLevel))
                .GroupBy(s => s.SportId).Select(g => g.First()).ToList();
            if (validSports.Count == 0)
                return BadRequest(new { success = false, message = "Chọn ít nhất 1 môn thể thao và trình độ." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(req.PhoneNumber))
                user.PhoneNumber = req.PhoneNumber.Trim();
            if (!string.IsNullOrWhiteSpace(req.DefaultAddress))
            {
                user.DefaultAddress = req.DefaultAddress.Trim();
                if (req.Latitude is >= -90 and <= 90) user.DefaultLatitude = req.Latitude;
                if (req.Longitude is >= -180 and <= 180) user.DefaultLongitude = req.Longitude;
            }

            var existing = await _context.UserSportProfiles
                .Where(p => p.UserID == userId)
                .ToListAsync();

            foreach (var sub in validSports)
            {
                var current = existing.FirstOrDefault(p => p.SportID == sub.SportId);
                if (current != null)
                {
                    current.SkillLevel = sub.SkillLevel.Trim();
                    current.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.UserSportProfiles.Add(new UserSportProfile
                    {
                        UserID = userId,
                        SportID = sub.SportId,
                        SkillLevel = sub.SkillLevel.Trim(),
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // Sync legacy fields — dùng bởi join validation fallback và hiển thị hồ sơ
            var firstSport = await _context.Sports.FirstOrDefaultAsync(s => s.SportID == validSports[0].SportId);
            user.FavoriteSport = firstSport?.SportName;
            user.SkillLevel = validSports[0].SkillLevel.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        public record CompleteProfileRequest(
            string? PhoneNumber,
            string? DefaultAddress,
            decimal? Latitude,
            decimal? Longitude,
            List<SportSkillDto>? Sports);

        public record SportSkillDto(int SportId, string SkillLevel);
    }
}
