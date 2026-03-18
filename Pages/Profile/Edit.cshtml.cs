using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Profile
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly IUserService _userService;

        public EditModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nh?p h? và tên.")]
            [StringLength(100)]
            public string FullName { get; set; } = string.Empty;

            [StringLength(20)]
            public string? PhoneNumber { get; set; }

            [Url(ErrorMessage = "URL ?nh ??i di?n không h?p l?.")]
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
            if (!ModelState.IsValid) return Page();

            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var updated = await _userService.UpdateUserProfileAsync(userId, Input.FullName, Input.PhoneNumber, Input.AvatarUrl, Input.SkillLevel);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? "C?p nh?t h? s? thành công."
                : "Không th? c?p nh?t h? s?.";

            return RedirectToPage("/Profile/Index");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
