using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using SportHub.Services;
using SportHub.Services.Interfaces;
using SportHub.Data;

namespace SportHub.Pages.Matchmaking
{
    public class IndexModel : PageModel
    {
        private readonly IMatchService _matchService;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;
        private readonly IGeocodingService _geocoding;
        private readonly IMatchReviewService _reviewService;

        public IndexModel(
            IMatchService matchService,
            INotificationService notificationService,
            ApplicationDbContext context,
            IGeocodingService geocoding,
            IMatchReviewService reviewService)
        {
            _matchService = matchService;
            _notificationService = notificationService;
            _context = context;
            _geocoding = geocoding;
            _reviewService = reviewService;
        }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Sport { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Skill { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Time { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MinPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        // New time range filters
        [BindProperty(SupportsGet = true)]
        public string? TimeFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TimeTo { get; set; }

        // Date filter
        [BindProperty(SupportsGet = true)]
        public DateTime? MatchDate { get; set; }

        // New location filters
        [BindProperty(SupportsGet = true)]
        public string? Location { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? Radius { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? District { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? City { get; set; }

        public List<string> SportOptions { get; set; } = new();

        public List<MatchCardItem> Matches { get; set; } = new();
        public List<JoinedMatchItem> UpcomingJoinedMatches { get; set; } = new();
        public List<JoinedMatchItem> JoinedMatchHistory { get; set; } = new();

        public string? UserDefaultAddress { get; set; }
        public decimal? UserDefaultLatitude { get; set; }
        public decimal? UserDefaultLongitude { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
            var currentUserId = GetCurrentUserId();

            if (currentUserId > 0)
            {
                var profileUser = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.UserID == currentUserId)
                    .Select(u => new { u.DefaultAddress, u.DefaultLatitude, u.DefaultLongitude })
                    .FirstOrDefaultAsync();
                if (profileUser != null)
                {
                    UserDefaultAddress = string.IsNullOrWhiteSpace(profileUser.DefaultAddress)
                        ? null
                        : profileUser.DefaultAddress.Trim();
                    UserDefaultLatitude = profileUser.DefaultLatitude;
                    UserDefaultLongitude = profileUser.DefaultLongitude;
                }
            };

            var currentUser = currentUserId > 0
                ? await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserId)
                : null;

            var matches = currentUserId > 0
                ? await _matchService.GetRecommendedMatchesForUserAsync(currentUserId, 50)
                : await _matchService.GetRecommendedMatchesAsync(50);

            SportOptions = matches
                .Select(m => NormalizeSportName(m.Sport?.SportName))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            if (!string.IsNullOrWhiteSpace(Sport))
            {
                matches = matches
                    .Where(m => string.Equals(NormalizeSportName(m.Sport?.SportName), Sport, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(Skill) && !string.Equals(Skill, "Any", StringComparison.OrdinalIgnoreCase))
            {
                matches = matches
                    .Where(m => string.Equals(m.SkillRequired ?? "Any", Skill, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(m.SkillRequired ?? string.Empty, "Any", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply time range filter
            if (!string.IsNullOrWhiteSpace(TimeFrom) && TimeSpan.TryParse(TimeFrom, out var fromTime))
            {
                matches = matches.Where(m => m.StartTime >= fromTime).ToList();
            }

            if (!string.IsNullOrWhiteSpace(TimeTo) && TimeSpan.TryParse(TimeTo, out var toTime))
            {
                matches = matches.Where(m => m.StartTime <= toTime).ToList();
            }

            // Apply specific date filter
            if (MatchDate.HasValue)
            {
                matches = matches.Where(m => m.MatchDate.Date == MatchDate.Value.Date).ToList();
            }

            // Apply location filter (basic string matching for now)
            if (!string.IsNullOrWhiteSpace(Location) || !string.IsNullOrWhiteSpace(District) || !string.IsNullOrWhiteSpace(City))
            {
                matches = matches.Where(m => 
                {
                    var venue = m.Court?.Venue?.VenueName ?? "";
                    var matchesLocation = string.IsNullOrWhiteSpace(Location) || 
                                        venue.Contains(Location, StringComparison.OrdinalIgnoreCase);
                    var matchesDistrict = string.IsNullOrWhiteSpace(District) || 
                                        venue.Contains(District, StringComparison.OrdinalIgnoreCase);
                    var matchesCity = string.IsNullOrWhiteSpace(City) || 
                                    venue.Contains(City, StringComparison.OrdinalIgnoreCase);
                    
                    return matchesLocation && matchesDistrict && matchesCity;
                }).ToList();
            }

            if (!string.IsNullOrWhiteSpace(Time) && !string.Equals(Time, "Any", StringComparison.OrdinalIgnoreCase))
            {
                matches = matches.Where(m => Time switch
                {
                    "Morning" => m.StartTime < new TimeSpan(12, 0, 0),
                    "Afternoon" => m.StartTime >= new TimeSpan(12, 0, 0) && m.StartTime < new TimeSpan(17, 0, 0),
                    "Evening" => m.StartTime >= new TimeSpan(17, 0, 0),
                    _ => true
                }).ToList();
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && !string.Equals(StatusFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                matches = ApplyStatusFilter(matches, StatusFilter, currentUserId);
            }

            if (MinPrice.HasValue || MaxPrice.HasValue)
            {
                matches = matches
                    .Where(m =>
                    {
                        var amount = BuildMatchPriceAmount(m);
                        if (!amount.HasValue) return false;

                        if (MinPrice.HasValue && amount.Value < MinPrice.Value) return false;
                        if (MaxPrice.HasValue && amount.Value > MaxPrice.Value) return false;

                        return true;
                    })
                    .ToList();
            }

            var hostIds = matches.Select(m => m.CreatedByUserID).Distinct();
            var hostRatings = await _reviewService.GetBatchHostRatingsAsync(hostIds);

            Matches = matches.Select(m => {
                var acceptedCount = m.Participants.Count(p => p.JoinStatus == "Accepted");
                var myParticipation = currentUserId > 0
                    ? m.Participants.FirstOrDefault(p => p.UserID == currentUserId)
                    : null;
                var isJoined = myParticipation?.JoinStatus == "Accepted";
                var isPending = myParticipation?.JoinStatus == "Pending";
                var venue = BuildVenueName(m);
                var venueAddress = m.CustomCourtAddress
                    ?? ExtractCustomCourtAddress(m.Description)
                    ?? m.Court?.Venue?.Address
                    ?? venue;

                return new MatchCardItem
                {
                    MatchId = m.MatchID,
                    Title = string.IsNullOrWhiteSpace(m.Title) ? $"{m.MatchType} Match" : m.Title,
                    MatchType = string.IsNullOrWhiteSpace(m.MatchType) ? "Open Match" : m.MatchType,
                    SportName = m.Sport?.SportName ?? "Sport",
                    SkillRequired = GetSkillDisplay(m.SkillRequired),
                    MatchScore = CalculateMatchScore(m, currentUser),
                    StartText = $"{m.MatchDate.ToString("ddd, dd/MM", new System.Globalization.CultureInfo("vi-VN"))}",
                    StartTime = m.StartTime.ToString(@"hh\:mm"),
                    Venue = venue,
                    VenueAddress = venueAddress,
                    PriceDisplay = BuildPriceDisplay(m),
                    PriceAmount = BuildMatchPriceAmount(m),
                    Participants = acceptedCount,
                    MaxParticipants = m.MaxParticipants,
                    IsJoinedByCurrentUser = isJoined,
                    IsPendingByCurrentUser = isPending,
                    IsOwnedByCurrentUser = currentUserId > 0 && m.CreatedByUserID == currentUserId,
                    CanJoin = currentUserId > 0 && myParticipation == null && m.Status == "Open" && acceptedCount < m.MaxParticipants,
                    HostImage = !string.IsNullOrEmpty(m.CreatedByUser?.AvatarUrl)
                                ? m.CreatedByUser.AvatarUrl
                                : "/images/avatar-default.png",
                    Latitude = m.CustomLatitude ?? ExtractCustomLatitude(m.Description) ?? m.Court?.Venue?.Latitude,
                    Longitude = m.CustomLongitude ?? ExtractCustomLongitude(m.Description) ?? m.Court?.Venue?.Longitude,
                    HostRatingAvg = hostRatings.ContainsKey(m.CreatedByUserID) ? hostRatings[m.CreatedByUserID].Avg : null,
                    HostRatingCount = hostRatings.ContainsKey(m.CreatedByUserID) ? hostRatings[m.CreatedByUserID].Count : 0
                };
            }).ToList();

            await ApplyProfileBasedDistancesAsync(currentUserId);

            await LoadJoinedMatchesAsync(currentUserId);
        }

        public async Task<IActionResult> OnGetGeocodeAsync(string q)
        {
            var result = await _geocoding.ResolveAsync(q);
            if (result == null)
                return new JsonResult(new { });

            return new JsonResult(new { lat = result.Lat, lon = result.Lon });
        }

        public async Task<IActionResult> OnPostJoinAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return RedirectToPage("/Auth/Login", new { returnUrl = "/Matchmaking" });

            var match = await _matchService.GetMatchDetailsAsync(id);
            if (match == null) return RedirectToPage("/Matchmaking/Index");

            var joined = await _matchService.JoinMatchAsync(id, userId);
            if (joined)
            {
                SuccessMessage = "Yêu cầu đã gửi. Bạn đang trong hàng chờ - host sẽ duyệt trong vòng 2 giờ.";
                var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "Người chơi";
                await _notificationService.CreateAsync(
                    match.CreatedByUserID,
                    "MatchJoin",
                    "Có người muốn tham gia trận",
                    $"{currentUser} gửi yêu cầu tham gia trận \"{match.Title ?? match.MatchType}\". Vui lòng duyệt trong 2 giờ.",
                    $"/Matchmaking/Details?id={id}");
            }
            else
            {
                ErrorMessage = "Không thể tham gia trận đấu. Kiểm tra lại trình độ hoặc trận đấu đã đầy/đóng.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSkipAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return RedirectToPage("/Auth/Login", new { returnUrl = "/Matchmaking" });

            var skipped = await _matchService.SkipMatchAsync(id, userId);
            TempData[skipped ? "SuccessMessage" : "ErrorMessage"] = skipped
                ? "Đã bỏ qua trận này. Feed của bạn sẽ gọn hơn."
                : "Không thể bỏ qua trận đấu này.";

            return RedirectToPage();
        }

        private async Task LoadJoinedMatchesAsync(int userId)
        {
            if (userId <= 0)
            {
                UpcomingJoinedMatches = new();
                JoinedMatchHistory = new();
                return;
            }

            var joined = await _context.MatchParticipants
                .Where(mp => mp.UserID == userId && mp.JoinStatus == "Accepted")
                .Include(mp => mp.Match).ThenInclude(m => m.Court).ThenInclude(c => c!.Venue)
                .OrderByDescending(mp => mp.Match.MatchDate)
                .ThenByDescending(mp => mp.Match.StartTime)
                .ToListAsync();

            var today = DateTime.Today;

            var mapped = joined.Select(mp => new JoinedMatchItem
            {
                MatchId = mp.MatchID,
                Title = string.IsNullOrWhiteSpace(mp.Match.Title) ? $"{mp.Match.MatchType} Match" : mp.Match.Title,
                StartText = $"{mp.Match.MatchDate:ddd, dd MMM} {mp.Match.StartTime:hh\\:mm}",
                Venue = BuildVenueName(mp.Match),
                MatchDate = mp.Match.MatchDate
            }).ToList();

            UpcomingJoinedMatches = mapped.Where(m => m.MatchDate >= today)
                .Take(5)
                .ToList();

            JoinedMatchHistory = mapped.Where(m => m.MatchDate < today)
                .Take(5)
                .ToList();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private async Task ApplyProfileBasedDistancesAsync(int userId)
        {
            var origin = await ResolveUserOriginAsync(userId);
            if (origin == null)
                return;

            foreach (var match in Matches)
            {
                var venueLat = match.Latitude.HasValue ? (double?)match.Latitude.Value : null;
                var venueLon = match.Longitude.HasValue ? (double?)match.Longitude.Value : null;

                if (!venueLat.HasValue || !venueLon.HasValue)
                {
                    var venueResult = await _geocoding.ResolveAsync(match.VenueAddress);
                    if (venueResult == null)
                        continue;

                    venueLat = venueResult.Lat;
                    venueLon = venueResult.Lon;
                    match.Latitude = (decimal)venueLat.Value;
                    match.Longitude = (decimal)venueLon.Value;
                }

                var km = HaversineKm(origin.Value.Lat, origin.Value.Lon, venueLat.Value, venueLon.Value);
                match.DistanceKm = km;
                match.DistanceDisplay = FormatDistanceKm(km);
            }

            Matches = Matches
                .OrderBy(m => m.DistanceKm ?? double.MaxValue)
                .ThenByDescending(m => m.MatchScore)
                .ToList();
        }

        private async Task<(double Lat, double Lon)?> ResolveUserOriginAsync(int userId)
        {
            if (UserDefaultLatitude.HasValue && UserDefaultLongitude.HasValue)
            {
                return ((double)UserDefaultLatitude.Value, (double)UserDefaultLongitude.Value);
            }

            if (!string.IsNullOrWhiteSpace(UserDefaultAddress))
            {
                var geocoded = await _geocoding.ResolveAsync(UserDefaultAddress);
                if (geocoded != null)
                    return (geocoded.Lat, geocoded.Lon);
            }

            return null;
        }

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static string FormatDistanceKm(double km) =>
            km < 1 ? $"~{(km * 1000):F0}m" : $"~{km:F1} km";

        private static string BuildPriceDisplay(Models.Entities.Match match)
        {
            var amount = BuildMatchPriceAmount(match);
            if (amount.HasValue)
            {
                if (match.Booking?.FinalAmount > 0)
                {
                    return $"Tổng: {amount.Value:N0} VNĐ";
                }

                return $"Từ {amount.Value:N0} VNĐ/giờ";
            }

            return "Chưa có giá";
        }

        private static decimal? BuildMatchPriceAmount(Models.Entities.Match match)
        {
            var customPrice = match.CustomPriceVnd ?? ExtractCustomPrice(match.Description);
            if (customPrice.HasValue)
            {
                return customPrice.Value;
            }

            if (match.Booking?.FinalAmount > 0)
            {
                return match.Booking.FinalAmount;
            }

            if (match.Court?.PricingRules != null && match.Court.PricingRules.Any())
            {
                return match.Court.PricingRules.Min(p => p.UnitPrice);
            }

            return null;
        }

        private static decimal? ExtractCustomPrice(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;

            var lines = description
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var line = lines.FirstOrDefault(l => l.StartsWith("Match price VND:", StringComparison.OrdinalIgnoreCase));
            if (line == null) return null;

            var raw = line.Replace("Match price VND:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            var normalized = Regex.Replace(raw, "[^0-9,.-]", string.Empty);

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
            {
                return invariantValue;
            }

            if (decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("vi-VN"), out var viValue))
            {
                return viValue;
            }

            return decimal.TryParse(normalized, NumberStyles.Number, new CultureInfo("en-US"), out var enValue)
                ? enValue
                : null;
        }

        private static string NormalizeSportName(string? sportName)
        {
            if (string.IsNullOrWhiteSpace(sportName)) return "Sport";
            return sportName.Trim();
        }

        public static string GetSkillDisplay(string? skillRequired)
        {
            if (string.IsNullOrWhiteSpace(skillRequired)) return "Mọi trình độ";
            return skillRequired.Trim().ToLower() switch
            {
                "any" => "Mọi trình độ",
                "beginner" => "Người mới (Beginner)",
                "intermediate" => "Trung bình (Intermediate)",
                "advanced" => "Nâng cao (Advanced)",
                "professional" => "Chuyên nghiệp (Professional)",
                _ => skillRequired
            };
        }

        private static string? ExtractCustomVenue(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var name = lines.FirstOrDefault(l => l.StartsWith("Court name:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court name:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            var addr = lines.FirstOrDefault(l => l.StartsWith("Court address:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court address:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(addr))
                return $"{name} - {addr}";
            return !string.IsNullOrWhiteSpace(name) ? name : addr;
        }

        private static List<Models.Entities.Match> ApplyStatusFilter(List<Models.Entities.Match> matches, string statusFilter, int currentUserId)
        {
            var today = DateTime.Today;
            var nextSevenDays = today.AddDays(7);

            return statusFilter switch
            {
                "OpenSlots" => matches
                    .Where(m => m.Status == "Open" && m.Participants.Count(p => p.JoinStatus == "Accepted") < m.MaxParticipants)
                    .ToList(),
                "AlmostFull" => matches
                    .Where(m =>
                    {
                        var accepted = m.Participants.Count(p => p.JoinStatus == "Accepted");
                        return accepted > 0 && accepted < m.MaxParticipants && accepted >= m.MaxParticipants - 1;
                    })
                    .ToList(),
                "Upcoming" => matches
                    .Where(m => m.MatchDate.Date >= today && m.MatchDate.Date <= nextSevenDays)
                    .ToList(),
                "Pending" => currentUserId <= 0
                    ? new List<Models.Entities.Match>()
                    : matches.Where(m => m.Participants.Any(p => p.UserID == currentUserId && p.JoinStatus == "Pending")).ToList(),
                "Joined" => currentUserId <= 0
                    ? new List<Models.Entities.Match>()
                    : matches.Where(m => m.Participants.Any(p => p.UserID == currentUserId && p.JoinStatus == "Accepted")).ToList(),
                "Owned" => currentUserId <= 0
                    ? new List<Models.Entities.Match>()
                    : matches.Where(m => m.CreatedByUserID == currentUserId).ToList(),
                _ => matches
            };
        }

        private static string BuildVenueName(Models.Entities.Match match)
        {
            if (!string.IsNullOrWhiteSpace(match.CustomCourtName) && !string.IsNullOrWhiteSpace(match.CustomCourtAddress))
                return $"{match.CustomCourtName} - {match.CustomCourtAddress}";

            if (!string.IsNullOrWhiteSpace(match.CustomCourtName))
                return match.CustomCourtName;

            if (!string.IsNullOrWhiteSpace(match.CustomCourtAddress))
                return match.CustomCourtAddress;

            return ExtractCustomVenue(match.Description) ?? match.Court?.Venue?.VenueName ?? "TBD Venue";
        }

        private int CalculateMatchScore(Models.Entities.Match match, Models.Entities.User? currentUser)
        {
            if (currentUser == null) return 70;

            var score = 0;

            if (!string.IsNullOrWhiteSpace(currentUser.FavoriteSport)
                && match.Sport?.SportName?.Contains(currentUser.FavoriteSport, StringComparison.OrdinalIgnoreCase) == true)
            {
                score += 30;
            }
            else if (string.IsNullOrWhiteSpace(currentUser.FavoriteSport))
            {
                score += 18;
            }

            score += CalculateSkillScore(currentUser.SkillLevel, match.SkillRequired);
            score += CalculateTimeScore(match.StartTime);

            if (currentUser.DefaultLatitude.HasValue && currentUser.DefaultLongitude.HasValue)
            {
                var lat = match.CustomLatitude ?? match.Court?.Venue?.Latitude;
                var lon = match.CustomLongitude ?? match.Court?.Venue?.Longitude;
                if (lat.HasValue && lon.HasValue)
                {
                    var km = HaversineKm((double)currentUser.DefaultLatitude.Value, (double)currentUser.DefaultLongitude.Value, (double)lat.Value, (double)lon.Value);
                    score += km <= 3 ? 15 : km <= 7 ? 10 : km <= 15 ? 6 : 2;
                }
                else
                {
                    score += 6;
                }
            }
            else
            {
                score += 8;
            }

            var acceptedCount = match.Participants.Count(p => p.JoinStatus == "Accepted");
            score += acceptedCount > 0 && acceptedCount < match.MaxParticipants ? 10 : 6;

            return Math.Clamp(score, 35, 99);
        }

        private static int CalculateSkillScore(string? userSkill, string? requiredSkill)
        {
            if (string.IsNullOrWhiteSpace(requiredSkill) || requiredSkill.Equals("Any", StringComparison.OrdinalIgnoreCase))
                return 22;

            if (string.IsNullOrWhiteSpace(userSkill)) return 10;

            var userValue = SkillToRank(userSkill);
            var requiredValue = SkillToRank(requiredSkill);
            if (!userValue.HasValue || !requiredValue.HasValue) return 15;

            var diff = Math.Abs(userValue.Value - requiredValue.Value);
            return diff switch
            {
                0 => 25,
                1 => 20,
                2 => 14,
                _ => 8
            };
        }

        private static int? SkillToRank(string value)
        {
            return value.Trim().ToLowerInvariant() switch
            {
                "newbie" or "beginner" => 1,
                "yếu" or "intermediate" => 2,
                "yếu+" => 3,
                "tby/tb-" or "advanced" => 4,
                "trung bình" => 5,
                "tb+/khá" or "professional" => 6,
                _ => null
            };
        }

        private static int CalculateTimeScore(TimeSpan startTime)
        {
            if (startTime >= new TimeSpan(17, 0, 0) && startTime <= new TimeSpan(21, 0, 0))
                return 20;

            if (startTime >= new TimeSpan(6, 0, 0) && startTime < new TimeSpan(10, 0, 0))
                return 18;

            if (startTime >= new TimeSpan(10, 0, 0) && startTime < new TimeSpan(17, 0, 0))
                return 14;

            return 8;
        }

        private static string? ExtractCustomCourtAddress(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return lines.FirstOrDefault(l => l.StartsWith("Court address:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court address:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        private static decimal? ExtractCustomLatitude(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var line = lines.FirstOrDefault(l => l.StartsWith("Court latitude:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court latitude:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return decimal.TryParse(line, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static decimal? ExtractCustomLongitude(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var line = lines.FirstOrDefault(l => l.StartsWith("Court longitude:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court longitude:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return decimal.TryParse(line, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        public class MatchCardItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string MatchType { get; set; } = string.Empty;
            public string SportName { get; set; } = string.Empty;
            public string SkillRequired { get; set; } = "Any";
            public int MatchScore { get; set; }
            public string StartText { get; set; } = string.Empty;
            public string StartTime { get; set; } = string.Empty;
            public string Venue { get; set; } = string.Empty;
            public string VenueAddress { get; set; } = string.Empty;
            public string PriceDisplay { get; set; } = string.Empty;
            public decimal? PriceAmount { get; set; }
            public int Participants { get; set; }
            public int MaxParticipants { get; set; }
            public bool IsJoinedByCurrentUser { get; set; }
            public bool IsPendingByCurrentUser { get; set; }
            public bool IsOwnedByCurrentUser { get; set; }
            public bool CanJoin { get; set; }
            public string HostImage { get; set; } = string.Empty;
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public double? DistanceKm { get; set; }
            public string? DistanceDisplay { get; set; }
            public decimal? HostRatingAvg { get; set; }
            public int HostRatingCount { get; set; }
            public bool ShowHostRating => HostRatingCount >= 3 && HostRatingAvg.HasValue;
        }

        public class JoinedMatchItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string StartText { get; set; } = string.Empty;
            public string Venue { get; set; } = string.Empty;
            public DateTime MatchDate { get; set; }
        }
    }
}

