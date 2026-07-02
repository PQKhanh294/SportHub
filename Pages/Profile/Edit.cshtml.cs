using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using SportHub.Services.Interfaces;
using SportHub.Models.Entities;
using SportHub.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace SportHub.Pages.Profile
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public EditModel(IUserService userService, IWebHostEnvironment environment, ApplicationDbContext context)
        {
            _userService = userService;
            _environment = environment;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? AvatarFile { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Full name is required.")]
            [StringLength(100)]
            public string FullName { get; set; } = string.Empty;

            [StringLength(20)]
            public string? PhoneNumber { get; set; }

            [StringLength(50)]
            public string? ZaloContact { get; set; }

            public string? AvatarUrl { get; set; }

            [StringLength(300)]
            public string? DefaultAddress { get; set; }

            public decimal? DefaultLatitude { get; set; }
            public decimal? DefaultLongitude { get; set; }

            public string? Gender { get; set; }
            public DateTime? DateOfBirth { get; set; }

            public bool NotifyByEmail { get; set; } = true;

            public List<SportSkillInput> SportSkills { get; set; } = new();
        }

        public class SportSkillInput
        {
            public int SportId { get; set; }
            public string SkillLevel { get; set; } = "Any";
        }

        public List<Sport> DbSports { get; set; } = new();
        public List<string> SkillOptions { get; set; } = new() 
        { 
            "Newbie", "Yếu", "Yếu+", "TBY/TB-", "Trung Bình", "TB+/Khá",
            "Beginner", "Intermediate", "Advanced", "Professional"
        };

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["ActivePage"] = "Profile";
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return RedirectToPage("/Auth/Login");

            Input = new InputModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                ZaloContact = user.ZaloContact,
                AvatarUrl = user.AvatarUrl,
                DefaultAddress = user.DefaultAddress,
                DefaultLatitude = user.DefaultLatitude,
                DefaultLongitude = user.DefaultLongitude,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                NotifyByEmail = user.NotifyByEmail,
                SportSkills = await _context.UserSportProfiles
                    .Where(usp => usp.UserID == userId)
                    .Select(usp => new SportSkillInput
                    {
                        SportId = usp.SportID,
                        SkillLevel = usp.SkillLevel ?? "Any"
                    })
                    .ToListAsync()
            };

            await LoadOptionsAsync();
            return Page();
        }

        private async Task LoadOptionsAsync()
        {
            DbSports = await _context.Sports
                .OrderBy(s => s.SportName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["ActivePage"] = "Profile";
            NormalizeLocationCoordinatesFromForm();
            ValidateAvatarInput();
            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                return Page();
            }

            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            if (AvatarFile is { Length: > 0 })
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsFolder);

                var extension = Path.GetExtension(AvatarFile.FileName).ToLowerInvariant();
                var fileName = $"avatar_{userId}_{Guid.NewGuid():N}{extension}";
                var savePath = Path.Combine(uploadsFolder, fileName);

                await using var stream = System.IO.File.Create(savePath);
                await AvatarFile.CopyToAsync(stream);

                Input.AvatarUrl = $"/uploads/avatars/{fileName}";
            }

            // Load user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return RedirectToPage("/Auth/Login");

            // Update user properties
            user.FullName = Input.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();
            user.ZaloContact = string.IsNullOrWhiteSpace(Input.ZaloContact) ? null : Input.ZaloContact.Trim();
            if (!string.IsNullOrWhiteSpace(Input.AvatarUrl))
            {
                user.AvatarUrl = Input.AvatarUrl.Trim();
            }
            user.DefaultAddress = string.IsNullOrWhiteSpace(Input.DefaultAddress) ? null : Input.DefaultAddress.Trim();
            user.DefaultLatitude = Input.DefaultLatitude;
            user.DefaultLongitude = Input.DefaultLongitude;
            user.Gender = string.IsNullOrWhiteSpace(Input.Gender) ? null : Input.Gender.Trim();
            user.DateOfBirth = Input.DateOfBirth;
            user.NotifyByEmail = Input.NotifyByEmail;

            // Filter out empty or duplicate sport selections
            var submittedSkills = Input.SportSkills
                .Where(ss => ss.SportId > 0)
                .GroupBy(ss => ss.SportId)
                .Select(g => g.First())
                .ToList();

            // Sync with primary fields for backward compatibility
            if (submittedSkills.Any())
            {
                var first = submittedSkills.First();
                var sport = await _context.Sports.FirstOrDefaultAsync(s => s.SportID == first.SportId);
                user.FavoriteSport = sport?.SportName;
                user.SkillLevel = first.SkillLevel;
            }
            else
            {
                user.FavoriteSport = null;
                user.SkillLevel = null;
            }
            user.UpdatedAt = DateTime.UtcNow;

            // Load existing UserSportProfiles
            var existingProfiles = await _context.UserSportProfiles
                .Where(usp => usp.UserID == userId)
                .ToListAsync();

            // Profiles to delete
            var submittedSportIds = submittedSkills.Select(s => s.SportId).ToList();
            var profilesToDelete = existingProfiles.Where(ep => !submittedSportIds.Contains(ep.SportID)).ToList();
            _context.UserSportProfiles.RemoveRange(profilesToDelete);

            // Profiles to add/update
            foreach (var sub in submittedSkills)
            {
                var existing = existingProfiles.FirstOrDefault(ep => ep.SportID == sub.SportId);
                if (existing != null)
                {
                    existing.SkillLevel = sub.SkillLevel;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.UserSportProfiles.Add(new UserSportProfile
                    {
                        UserID = userId,
                        SportID = sub.SportId,
                        SkillLevel = sub.SkillLevel,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thành công! Hồ sơ của bạn đã được cập nhật.";

            return RedirectToPage("/Profile/Index");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private void ValidateAvatarInput()
        {
            if (AvatarFile is { Length: > 0 })
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(AvatarFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(AvatarFile), "Avatar file must be .jpg, .jpeg, .png or .webp.");
                }

                if (AvatarFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(AvatarFile), "Avatar file size must be less than 2MB.");
                }
            }

            if (!string.IsNullOrWhiteSpace(Input.AvatarUrl))
            {
                var isHttpUrl = Uri.TryCreate(Input.AvatarUrl, UriKind.Absolute, out var uri)
                                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                var isLocalUploadPath = Input.AvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase);

                if (!isHttpUrl && !isLocalUploadPath)
                {
                    ModelState.AddModelError("Input.AvatarUrl", "Avatar URL must be a valid http/https link.");
                }
            }
        }

        private void NormalizeLocationCoordinatesFromForm()
        {
            // Hidden inputs từ map luôn gửi dạng "12.345678"; với vi-VN binder decimal có thể coi là invalid.
            // Ở đây parse thủ công để chấp nhận cả dấu "." và ",".
            var latRaw = Request.Form["Input.DefaultLatitude"].ToString();
            var lonRaw = Request.Form["Input.DefaultLongitude"].ToString();

            ModelState.Remove("Input.DefaultLatitude");
            ModelState.Remove("Input.DefaultLongitude");

            Input.DefaultLatitude = ParseFlexibleDecimal(latRaw);
            Input.DefaultLongitude = ParseFlexibleDecimal(lonRaw);
        }

        private static decimal? ParseFlexibleDecimal(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var normalized = raw.Trim();
            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var inv))
                return inv;

            if (decimal.TryParse(normalized, NumberStyles.Any, new CultureInfo("vi-VN"), out var vi))
                return vi;

            normalized = normalized.Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var fallback))
                return fallback;

            return null;
        }
    }
}
