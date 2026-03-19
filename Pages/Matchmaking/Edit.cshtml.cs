using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly IMatchService _matchService;
        private readonly ApplicationDbContext _context;

        public EditModel(IMatchService matchService, ApplicationDbContext context)
        {
            _matchService = matchService;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> SportOptions { get; set; } = new();
        public List<SelectListItem> SkillOptions { get; set; } = new();

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

            Input = new InputModel
            {
                MatchId = match.MatchID,
                Title = match.Title ?? string.Empty,
                CourtId = match.CourtID,
                CourtName = ExtractMeta(match.Description, "Court name:"),
                CourtAddress = ExtractMeta(match.Description, "Court address:"),
                SportId = match.SportID,
                MatchDate = match.MatchDate,
                StartTime = match.StartTime,
                EndTime = match.EndTime,
                MatchType = string.IsNullOrWhiteSpace(match.MatchType) ? "Doubles" : match.MatchType,
                SkillRequired = string.IsNullOrWhiteSpace(match.SkillRequired) ? "Any" : match.SkillRequired,
                MaxParticipants = match.MaxParticipants,
                PriceVnd = ExtractPrice(match.Description),
                Description = ExtractFreeDescription(match.Description)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
            await LoadSelectionsAsync();

            var now = DateTime.Now;
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

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Auth/Login");
            }

            var updatedMatch = new Match
            {
                CourtID = Input.CourtId,
                SportID = Input.SportId,
                MatchDate = Input.MatchDate,
                StartTime = Input.StartTime,
                EndTime = Input.EndTime,
                MatchType = Input.MatchType,
                SkillRequired = Input.SkillRequired,
                MaxParticipants = (byte)Input.MaxParticipants,
                Title = Input.Title,
                Description = BuildDescription(Input.Description, Input.CourtName, Input.CourtAddress, Input.PriceVnd)
            };

            var updated = await _matchService.UpdateMatchAsync(Input.MatchId, userId, updatedMatch);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? "Match updated successfully."
                : "Unable to update this match.";

            return RedirectToPage("/Matchmaking/Details", new { id = Input.MatchId });
        }

        private async Task LoadSelectionsAsync()
        {
            SportOptions = await _context.Sports
                .OrderBy(s => s.SportName)
                .Select(s => new SelectListItem
                {
                    Value = s.SportID.ToString(),
                    Text = s.SportName == "Cầu lông" ? "Badminton" : s.SportName
                })
                .ToListAsync();

            SkillOptions = new List<SelectListItem>
            {
                new() { Value = "Any", Text = "Any level" },
                new() { Value = "Beginner", Text = "Beginner" },
                new() { Value = "Intermediate", Text = "Intermediate" },
                new() { Value = "Advanced", Text = "Advanced" },
                new() { Value = "Professional", Text = "Professional" }
            };
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private static string? BuildDescription(string? description, string? courtName, string? courtAddress, decimal? priceVnd)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(courtName)) parts.Add($"Court name: {courtName.Trim()}");
            if (!string.IsNullOrWhiteSpace(courtAddress)) parts.Add($"Court address: {courtAddress.Trim()}");
            if (priceVnd.HasValue) parts.Add($"Match price VND: {priceVnd.Value.ToString("0.##", CultureInfo.InvariantCulture)}");
            if (!string.IsNullOrWhiteSpace(description)) parts.Add(description.Trim());

            return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);
        }

        private static string? ExtractMeta(string? description, string key)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;

            var lines = description.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var line = lines.FirstOrDefault(l => l.StartsWith(key, StringComparison.OrdinalIgnoreCase));
            return line?.Replace(key, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        private static decimal? ExtractPrice(string? description)
        {
            var raw = ExtractMeta(description, "Match price VND:");
            return decimal.TryParse(raw, out var value) ? value : null;
        }

        private static string? ExtractFreeDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;

            var lines = description
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l =>
                    !l.StartsWith("Court name:", StringComparison.OrdinalIgnoreCase)
                    && !l.StartsWith("Court address:", StringComparison.OrdinalIgnoreCase)
                    && !l.StartsWith("Match price VND:", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
        }
    }
}
