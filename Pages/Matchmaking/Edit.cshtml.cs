using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportHub.Common;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly IMatchService _matchService;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public EditModel(IMatchService matchService, INotificationService notificationService, ApplicationDbContext context)
        {
            _matchService = matchService;
            _notificationService = notificationService;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> SportOptions { get; set; } = new();

        // Khóa Môn/Ngày/Giờ/Sân/Loại trận/Trình độ khi đã có cọc hoặc người tham gia (MaxParticipants luôn khóa từ lúc tạo).
        public bool IsLocked { get; set; }

        public class InputModel
        {
            [Required]
            public int MatchId { get; set; }

            [Required(ErrorMessage = "Please enter a match title.")]
            public string Title { get; set; } = string.Empty;

            public int? CourtId { get; set; }

            [StringLength(100)]
            public string? CourtName { get; set; }

            [StringLength(300)]
            public string? CourtAddress { get; set; }

            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }

            [Required(ErrorMessage = "Please select a sport.")]
            public int SportId { get; set; }

            [Required(ErrorMessage = "Please select a match date.")]
            public DateTime MatchDate { get; set; }

            [Required(ErrorMessage = "Please select a start time.")]
            public TimeSpan StartTime { get; set; }

            [Required(ErrorMessage = "Please select an end time.")]
            public TimeSpan EndTime { get; set; }

            [Required(ErrorMessage = "Please select a match type.")]
            public string MatchType { get; set; } = "Doubles";

            [Required(ErrorMessage = "Please select a required skill level.")]
            public string SkillRequired { get; set; } = "Any";

            [Range(2, 20, ErrorMessage = "Max participants must be between 2 and 20.")]
            public int MaxParticipants { get; set; } = 4;

            [Range(0, 1000000000, ErrorMessage = "Price must be greater than or equal to 0.")]
            public decimal? PriceVnd { get; set; }

            public string? Description { get; set; }
            public string PriceMode { get; set; } = "PerPerson";
            public decimal? PriceMaleVnd { get; set; }
            public decimal? PriceFemaleVnd { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ViewData["ActivePage"] = "Matchmaking";
            await LoadSelectionsAsync();

            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var match = await _matchService.GetMatchDetailsAsync(id);
            if (match == null || match.CreatedByUserID != userId)
            {
                TempData["ErrorMessage"] = "You can only edit matches created by you.";
                return RedirectToPage("/Matchmaking/Index");
            }

            IsLocked = match.DepositStatus == "Paid" || match.Participants.Any(p => p.JoinStatus is "Accepted" or "Approved");

            Input = new InputModel
            {
                MatchId = match.MatchID,
                Title = match.Title ?? string.Empty,
                CourtId = match.CourtID,
                CourtName = match.CustomCourtName,
                CourtAddress = match.CustomCourtAddress,
                Latitude = match.CustomLatitude,
                Longitude = match.CustomLongitude,
                SportId = match.SportID,
                MatchDate = match.MatchDate,
                StartTime = match.StartTime,
                EndTime = match.EndTime,
                MatchType = string.IsNullOrWhiteSpace(match.MatchType) ? "Doubles" : match.MatchType,
                SkillRequired = string.IsNullOrWhiteSpace(match.SkillRequired) ? "Any" : match.SkillRequired,
                MaxParticipants = match.MaxParticipants,
                PriceVnd = match.CustomPriceVnd,
                PriceMode = match.PriceMode,
                PriceMaleVnd = match.PriceMaleVnd,
                PriceFemaleVnd = match.PriceFemaleVnd,
                Description = match.Description
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
            await LoadSelectionsAsync();

            var now = VietnamTime.Now;
            if (Input.MatchDate.Date < now.Date)
            {
                ModelState.AddModelError("Input.MatchDate", "Match date cannot be in the past.");
            }

            if (Input.EndTime <= Input.StartTime)
            {
                ModelState.AddModelError("Input.EndTime", "End time must be later than start time.");
            }

            var startAt = Input.MatchDate.Date + Input.StartTime;
            if (startAt <= now)
            {
                ModelState.AddModelError("Input.StartTime", "Start time must be in the future.");
            }

            var duration = Input.EndTime - Input.StartTime;
            if (duration < TimeSpan.FromMinutes(30) || duration > TimeSpan.FromHours(4))
            {
                ModelState.AddModelError("Input.EndTime", "Match duration must be between 30 minutes and 4 hours.");
            }

            var sportExists = await _context.Sports.AnyAsync(s => s.SportID == Input.SportId);
            if (!sportExists)
            {
                ModelState.AddModelError("Input.SportId", "Invalid sport.");
            }

            if (Input.CourtId.HasValue)
            {
                var court = await _context.Courts
                    .Include(c => c.Venue)
                    .FirstOrDefaultAsync(c => c.CourtID == Input.CourtId.Value && c.IsActive);

                if (court == null)
                {
                    ModelState.AddModelError("Input.CourtId", "Invalid court or inactive court.");
                }
                else if (Input.StartTime < court.Venue.OpenTime || Input.EndTime > court.Venue.CloseTime)
                {
                    ModelState.AddModelError("Input.StartTime", $"Match time must be within venue hours ({court.Venue.OpenTime:hh\\:mm} - {court.Venue.CloseTime:hh\\:mm}).");
                }
            }

            var existingMatch = await _matchService.GetMatchDetailsAsync(Input.MatchId);
            IsLocked = existingMatch != null
                && (existingMatch.DepositStatus == "Paid" || existingMatch.Participants.Any(p => p.JoinStatus is "Accepted" or "Approved"));

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Auth/Login");
            }

            // Sanitize coordinates — invalid values (e.g. browser autofill) crash decimal(10,8)/(11,8) columns
            if (Input.Latitude is < -90 or > 90) Input.Latitude = null;
            if (Input.Longitude is < -180 or > 180) Input.Longitude = null;

            var updatedMatch = new Match
            {
                CourtID = Input.CourtId,
                SportID = Input.SportId,
                MatchDate = Input.MatchDate,
                StartTime = Input.StartTime,
                EndTime = Input.EndTime,
                MatchType = string.IsNullOrWhiteSpace(Input.MatchType) ? "Doubles" : Input.MatchType,
                SkillRequired = string.IsNullOrWhiteSpace(Input.SkillRequired) ? "Any" : Input.SkillRequired,
                MaxParticipants = (byte)Input.MaxParticipants,
                Title = Input.Title,
                Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
                CustomCourtName = string.IsNullOrWhiteSpace(Input.CourtName) ? null : Input.CourtName.Trim(),
                CustomCourtAddress = string.IsNullOrWhiteSpace(Input.CourtAddress) ? null : Input.CourtAddress.Trim(),
                PriceMode = Input.PriceMode,
                CustomPriceVnd = Input.PriceMode == "ByGender" ? null : Input.PriceVnd,
                PriceMaleVnd = Input.PriceMode == "ByGender" ? Input.PriceMaleVnd : null,
                PriceFemaleVnd = Input.PriceMode == "ByGender" ? Input.PriceFemaleVnd : null,
                CustomLatitude = Input.Latitude,
                CustomLongitude = Input.Longitude
            };

            var (updated, message) = await _matchService.UpdateMatchAsync(Input.MatchId, userId, updatedMatch);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = message;

            return RedirectToPage("/Matchmaking/Details", new { id = Input.MatchId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var match = await _matchService.GetMatchDetailsAsync(id);
            if (match == null || match.CreatedByUserID != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa trận này.";
                return RedirectToPage("/Matchmaking/Index");
            }

            if (match.Status is "Completed" or "InProgress")
            {
                TempData["ErrorMessage"] = "Không thể xóa trận đang diễn ra hoặc đã hoàn thành.";
                return RedirectToPage("/Matchmaking/Details", new { id });
            }

            var (success, message) = await _matchService.CancelMatchByHostAsync(id, userId, "Host đã xóa trận");
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Đã hủy trận. {message}"
                : message;

            return RedirectToPage("/Matchmaking/Index");
        }

        private async Task LoadSelectionsAsync()
        {
            SportOptions = await _context.Sports
                .OrderBy(s => s.SportName)
                .Select(s => new SelectListItem
                {
                    Value = s.SportID.ToString(),
                    Text = s.SportName
                })
                .ToListAsync();

        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

    }
}
