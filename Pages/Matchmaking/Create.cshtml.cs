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
    public class CreateModel : PageModel
    {
        private readonly IMatchService _matchService;
        private readonly ApplicationDbContext _context;

        public CreateModel(IMatchService matchService, ApplicationDbContext context)
        {
            _matchService = matchService;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new()
        {
            MatchDate = DateTime.Today,
            StartTime = new TimeSpan(18, 0, 0),
            EndTime = new TimeSpan(19, 0, 0),
            MatchType = "Doubles",
            SkillRequired = "Any",
            MaxParticipants = 4
        };

        public List<SelectListItem> SportOptions { get; set; } = new();
        public List<SelectListItem> SkillOptions { get; set; } = new();

        public class InputModel
        {
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

            public bool RequiresApproval { get; set; } = false;
        }

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
            await LoadSelectionsAsync();
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
                else
                {
                    if (Input.StartTime < court.Venue.OpenTime || Input.EndTime > court.Venue.CloseTime)
                    {
                        ModelState.AddModelError("Input.StartTime", $"Match time must be within venue hours ({court.Venue.OpenTime:hh\\:mm} - {court.Venue.CloseTime:hh\\:mm}).");
                    }
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

            var match = new Match
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
                RequiresApproval = Input.RequiresApproval,
                Description = BuildDescriptionWithCustomCourt(Input.Description, Input.CourtName, Input.CourtAddress, Input.PriceVnd)
            };

            var matchId = await _matchService.CreateMatchAsync(match, userId);
            TempData["SuccessMessage"] = "Match created successfully.";
            return RedirectToPage("/Matchmaking/Details", new { id = matchId });
        }

        private async Task LoadSelectionsAsync()
        {
            SportOptions = await _context.Sports
                .OrderBy(s => s.SportName)
                .Select(s => new SelectListItem
                {
                    Value = s.SportID.ToString(),
                    Text = NormalizeSportName(s.SportName)
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

        private static string? BuildDescriptionWithCustomCourt(string? description, string? courtName, string? courtAddress, decimal? priceVnd)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(courtName))
            {
                parts.Add($"Court name: {courtName.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(courtAddress))
            {
                parts.Add($"Court address: {courtAddress.Trim()}");
            }

            if (priceVnd.HasValue)
            {
                parts.Add($"Match price VND: {priceVnd.Value.ToString("0.##", CultureInfo.InvariantCulture)}");
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                parts.Add(description.Trim());
            }

            return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);
        }

        private static string NormalizeSportName(string sportName)
        {
            return sportName.Trim() switch
            {
                "Cầu lông" => "Badminton",
                _ => sportName.Trim()
            };
        }
    }
}
