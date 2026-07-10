using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using SportHub.Common;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly IMatchService _matchService;
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly ApplicationDbContext _context;
        private readonly IGeocodingService _geocodingService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IWalletService _walletService;

        public CreateModel(IMatchService matchService, IMatchPaymentService matchPaymentService, ApplicationDbContext context, IGeocodingService geocodingService, ISubscriptionService subscriptionService, IWalletService walletService)
        {
            _matchService = matchService;
            _matchPaymentService = matchPaymentService;
            _context = context;
            _geocodingService = geocodingService;
            _subscriptionService = subscriptionService;
            _walletService = walletService;
        }

        public string? SubscriptionLimitMessage { get; set; }

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

            [StringLength(30)]
            public string? CourtNumber { get; set; }

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

            public string MatchType { get; set; } = "Doubles";

            public string SkillRequired { get; set; } = "Any";

            [Range(2, 20, ErrorMessage = "Max participants must be between 2 and 20.")]
            public int MaxParticipants { get; set; } = 4;

            [Range(0, 1000000000, ErrorMessage = "Price must be greater than or equal to 0.")]
            public decimal? PriceVnd { get; set; }

            public string? Description { get; set; }

            public bool RequiresApproval { get; set; } = true;

            public string PriceMode { get; set; } = "PerPerson";
            public decimal? PriceMaleVnd { get; set; }
            public decimal? PriceFemaleVnd { get; set; }
            public bool IsRecurring { get; set; }
            public string? RecurringDays { get; set; }
            public DateTime? RecurringUntil { get; set; }

            public bool HostJoins { get; set; } = false;
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

            // Sanitize coordinates — invalid values (e.g. browser autofill) crash decimal(10,8)/(11,8) columns
            if (Input.Latitude is < -90 or > 90) Input.Latitude = null;
            if (Input.Longitude is < -180 or > 180) Input.Longitude = null;

            // Server-side geocoding fallback: if JS didn't capture coords before submit, geocode the address now
            if ((Input.Latitude == null || Input.Longitude == null) && !string.IsNullOrWhiteSpace(Input.CourtAddress))
            {
                var geo = await _geocodingService.ResolveAsync(Input.CourtAddress);
                if (geo != null)
                {
                    Input.Latitude  = (decimal)geo.Lat;
                    Input.Longitude = (decimal)geo.Lon;
                }
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Auth/Login");
            }

            // Subscription gate: check monthly create limit
            if (!await _subscriptionService.CanCreateMatchAsync(userId))
            {
                var plan = await _subscriptionService.GetCurrentPlanAsync(userId);
                var count = await _subscriptionService.GetMonthlyCreateCountAsync(userId);
                TempData["ErrorMessage"] = $"Bạn đã tạo {count}/{plan.MonthlyCreateLimit} trận trong tháng này. Nâng cấp gói để tạo thêm!";
                return RedirectToPage("/Subscription/Index");
            }

            var matchType = string.IsNullOrWhiteSpace(Input.MatchType) ? "Doubles" : Input.MatchType.Trim();
            var skillRequired = string.IsNullOrWhiteSpace(Input.SkillRequired) ? "Any" : Input.SkillRequired.Trim();

            var match = new Match
            {
                CourtID = Input.CourtId,
                SportID = Input.SportId,
                MatchDate = Input.MatchDate,
                StartTime = Input.StartTime,
                EndTime = Input.EndTime,
                MatchType = matchType,
                SkillRequired = skillRequired,
                MaxParticipants = (byte)Input.MaxParticipants,
                Title = Input.Title,
                RequiresApproval = true,
                Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
                CustomCourtName = string.IsNullOrWhiteSpace(Input.CourtName) ? null : Input.CourtName.Trim(),
                CustomCourtAddress = string.IsNullOrWhiteSpace(Input.CourtAddress) ? null : Input.CourtAddress.Trim(),
                CourtNumber = string.IsNullOrWhiteSpace(Input.CourtNumber) ? null : Input.CourtNumber.Trim(),
                PriceMode = Input.PriceMode,
                CustomPriceVnd = Input.PriceMode == "ByGender" ? null : Input.PriceVnd,
                PriceMaleVnd = Input.PriceMode == "ByGender" ? Input.PriceMaleVnd : null,
                PriceFemaleVnd = Input.PriceMode == "ByGender" ? Input.PriceFemaleVnd : null,
                CustomLatitude = Input.Latitude,
                CustomLongitude = Input.Longitude,
                IsRecurring = Input.IsRecurring,
                RecurringDays = Input.IsRecurring && !string.IsNullOrWhiteSpace(Input.RecurringDays) ? Input.RecurringDays : null,
                RecurringUntil = Input.IsRecurring ? Input.RecurringUntil : null,
            };

            var matchId = await _matchService.CreateMatchAsync(match, userId, Input.HostJoins);
            await _matchPaymentService.CreateHostDepositAsync(matchId, userId);
            await _subscriptionService.RecordCreateAsync(userId);

            var depositAmount = _matchPaymentService.CalculateHostDeposit(Input.MaxParticipants);

            // Tự động trừ ví nếu số dư đủ — chỉ hiện trang thanh toán (QR/chuyển khoản) khi ví không đủ tiền.
            var autoPaid = await _walletService.PayMatchFeeFromWalletAsync(userId, matchId, "HostDeposit");
            if (autoPaid)
            {
                TempData["SuccessMessage"] = $"Trận được tạo! Đã tự động trừ {depositAmount:N0} xu tiền cọc từ ví. Trận đã sẵn sàng!";
                return RedirectToPage("/Matchmaking/Details", new { id = matchId });
            }

            TempData["SuccessMessage"] = $"Trận được tạo! Đặt cọc {depositAmount:N0} xu để đăng trận.";
            return RedirectToPage("/Matchmaking/Payment", new { matchId, type = "deposit" });
        }

        private async Task LoadSelectionsAsync()
        {
            var sports = await _context.Sports.OrderBy(s => s.SportName).ToListAsync();
            
            // Ensure core sports exist (Seed if needed)
            var coreSports = new[] { "Cầu lông", "Bóng đá", "Pickleball", "Bóng bàn", "Tennis" };
            bool changed = false;
            foreach (var coreSport in coreSports)
            {
                if (!sports.Any(s => s.SportName.Equals(coreSport, StringComparison.OrdinalIgnoreCase)))
                {
                    _context.Sports.Add(new Sport { SportName = coreSport });
                    changed = true;
                }
            }
            if (changed)
            {
                await _context.SaveChangesAsync();
                sports = await _context.Sports.OrderBy(s => s.SportName).ToListAsync();
            }

            SportOptions = sports.Select(s => new SelectListItem
            {
                Value = s.SportID.ToString(),
                Text = s.SportName
            }).ToList();

            SkillOptions = new List<SelectListItem>
            {
                new() { Value = "Any", Text = "Mọi trình độ" },
                new() { Value = "Beginner", Text = "Người mới (Beginner)" },
                new() { Value = "Intermediate", Text = "Trung bình (Intermediate)" },
                new() { Value = "Advanced", Text = "Nâng cao (Advanced)" },
                new() { Value = "Professional", Text = "Chuyên nghiệp (Professional)" }
            };
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private static string NormalizeSportName(string sportName)
        {
            return sportName.Trim();
        }
    }
}
