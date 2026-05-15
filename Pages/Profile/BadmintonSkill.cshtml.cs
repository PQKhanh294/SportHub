using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;

namespace SportHub.Pages.Profile
{
    [Authorize]
    public class BadmintonSkillModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BadmintonSkillModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SkillInputModel Input { get; set; } = new();

        public bool HasExistingProfile { get; set; }

        public class SkillInputModel
        {
            // BackCourt=Phông cao | FrontCourt=Nữa sân | AllRound=Đa năng
            public string? CourtPosition { get; set; }

            // Aggressive=Tấn công | Defensive=Phòng thủ | Balanced=Cân bằng
            public string? PlayStyle { get; set; }

            // Smash | Drop | Drive | AllRound=Đa năng
            public string? StrokeStrength { get; set; }

            // 1-5 sao
            public int? SelfRatedLevel { get; set; }

            public int? ExperienceYears { get; set; }

            public string? Notes { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Profile";
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var badminton = await _context.Sports
                .FirstOrDefaultAsync(s => s.SportName.ToLower().Contains("cầu") || s.SportName.ToLower().Contains("badminton"));

            if (badminton != null)
            {
                var existing = await _context.UserSportProfiles
                    .FirstOrDefaultAsync(u => u.UserID == userId && u.SportID == badminton.SportID);

                if (existing != null)
                {
                    HasExistingProfile = true;
                    Input = new SkillInputModel
                    {
                        CourtPosition   = existing.CourtPosition,
                        PlayStyle       = existing.PlayStyle,
                        StrokeStrength  = existing.StrokeStrength,
                        SelfRatedLevel  = existing.SelfRatedLevel,
                        ExperienceYears = existing.ExperienceYears,
                        Notes           = existing.Notes
                    };
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["ActivePage"] = "Profile";
            if (!ModelState.IsValid) return Page();

            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var badminton = await _context.Sports
                .FirstOrDefaultAsync(s => s.SportName.ToLower().Contains("cầu") || s.SportName.ToLower().Contains("badminton"));

            if (badminton == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy môn Cầu lông trong hệ thống.";
                return Page();
            }

            var existing = await _context.UserSportProfiles
                .FirstOrDefaultAsync(u => u.UserID == userId && u.SportID == badminton.SportID);

            if (existing == null)
            {
                _context.UserSportProfiles.Add(new UserSportProfile
                {
                    UserID          = userId,
                    SportID         = badminton.SportID,
                    CourtPosition   = Input.CourtPosition,
                    PlayStyle       = Input.PlayStyle,
                    StrokeStrength  = Input.StrokeStrength,
                    SelfRatedLevel  = Input.SelfRatedLevel,
                    ExperienceYears = Input.ExperienceYears,
                    Notes           = Input.Notes,
                    UpdatedAt       = DateTime.UtcNow
                });
            }
            else
            {
                existing.CourtPosition   = Input.CourtPosition;
                existing.PlayStyle       = Input.PlayStyle;
                existing.StrokeStrength  = Input.StrokeStrength;
                existing.SelfRatedLevel  = Input.SelfRatedLevel;
                existing.ExperienceYears = Input.ExperienceYears;
                existing.Notes           = Input.Notes;
                existing.UpdatedAt       = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Hồ sơ kỹ năng cầu lông đã được cập nhật!";
            return RedirectToPage("/Profile/Index");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
