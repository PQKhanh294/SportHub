using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly IMatchService _matchService;

        public CreateModel(IMatchService matchService)
        {
            _matchService = matchService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new()
        {
            MatchDate = DateTime.Today,
            StartTime = new TimeSpan(18, 0, 0),
            EndTime = new TimeSpan(19, 0, 0),
            MatchType = "Doubles",
            MaxParticipants = 4
        };

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nh?p tiêu ?? tr?n.")]
            public string Title { get; set; } = string.Empty;

            public int? CourtId { get; set; }

            [Required(ErrorMessage = "Vui lòng nh?p SportID.")]
            public int SportId { get; set; }

            [Required]
            public DateTime MatchDate { get; set; }

            [Required]
            public TimeSpan StartTime { get; set; }

            [Required]
            public TimeSpan EndTime { get; set; }

            [Required]
            public string MatchType { get; set; } = "Doubles";

            [Range(2, 20)]
            public int MaxParticipants { get; set; } = 4;

            public string? Description { get; set; }
        }

        public void OnGet()
        {
            ViewData["ActivePage"] = "Matchmaking";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
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
                SkillRequired = "Any",
                MaxParticipants = (byte)Input.MaxParticipants,
                Title = Input.Title,
                Description = Input.Description
            };

            var matchId = await _matchService.CreateMatchAsync(match, userId);
            TempData["SuccessMessage"] = "T?o tr?n thành công.";
            return RedirectToPage("/Matchmaking/Details", new { id = matchId });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
