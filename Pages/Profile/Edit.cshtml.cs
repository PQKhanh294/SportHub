using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Profile
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;

        public EditModel(IUserService userService, IWebHostEnvironment environment)
        {
            _userService = userService;
            _environment = environment;
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

            public string? AvatarUrl { get; set; }

            public string? SkillLevel { get; set; }
        }

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
                AvatarUrl = user.AvatarUrl,
                SkillLevel = user.SkillLevel
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["ActivePage"] = "Profile";
            ValidateAvatarInput();
            if (!ModelState.IsValid) return Page();

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

            var updated = await _userService.UpdateUserProfileAsync(
                userId,
                Input.FullName,
                Input.PhoneNumber,
                Input.AvatarUrl,
                Input.SkillLevel);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? "Profile updated successfully."
                : "Failed to update profile.";

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
    }
}
