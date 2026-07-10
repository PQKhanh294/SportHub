using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using SportHub.Hubs;
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
        private readonly ISubscriptionService _subscriptionService;
        private readonly IAiChatService _aiChat;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public IndexModel(
            IMatchService matchService,
            INotificationService notificationService,
            ApplicationDbContext context,
            IGeocodingService geocoding,
            IMatchReviewService reviewService,
            ISubscriptionService subscriptionService,
            IAiChatService aiChat,
            IHubContext<NotificationHub> hubContext,
            IEmailService emailService,
            IConfiguration config)
        {
            _matchService = matchService;
            _notificationService = notificationService;
            _context = context;
            _geocoding = geocoding;
            _reviewService = reviewService;
            _subscriptionService = subscriptionService;
            _aiChat = aiChat;
            _hubContext = hubContext;
            _emailService = emailService;
            _config = config;
        }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

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

        // Distance filter (Starter+): user lat/lon submitted from JS
        [BindProperty(SupportsGet = true)]
        public double? UserLat { get; set; }

        [BindProperty(SupportsGet = true)]
        public double? UserLon { get; set; }

        public bool CanFilterByDistance { get; set; }
        public bool CanSeeMatchScore { get; set; }
        public bool HasAiFeature { get; set; }

        public List<string> SportOptions { get; set; } = new();

        public List<MatchCardItem> Matches { get; set; } = new();
        public List<CommunityListingCardItem> CommunityListings { get; set; } = new();
        public List<JoinedMatchItem> UpcomingJoinedMatches { get; set; } = new();
        public List<JoinedMatchItem> JoinedMatchHistory { get; set; } = new();
        public List<PendingRequestItem> PendingRequestMatches { get; set; } = new();
        public List<MyHostMatchItem> MyHostedMatches { get; set; } = new();

        public string? UserDefaultAddress { get; set; }
        public decimal? UserDefaultLatitude { get; set; }
        public decimal? UserDefaultLongitude { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalMatchCount { get; set; }
        private const int PageSize = 12;

        public async Task OnGetAsync()
        {
            ViewData["ActivePage"] = "Matchmaking";
            var currentUserId = GetCurrentUserId();

            CanFilterByDistance = currentUserId > 0 && await _subscriptionService.CanFilterByDistanceAsync(currentUserId);
            CanSeeMatchScore = CanFilterByDistance;
            HasAiFeature = currentUserId > 0 && await _subscriptionService.HasAiSuggestionsAsync(currentUserId);

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

            List<SportHub.Models.Entities.Match> matches;
            if (currentUserId > 0 && StatusFilter is "Joined" or "Owned" or "Pending")
            {
                var baseQuery = _context.Matches
                    .Include(m => m.CreatedByUser)
                    .Include(m => m.Court).ThenInclude(c => c!.Venue)
                    .Include(m => m.Court).ThenInclude(c => c!.Images)
                    .Include(m => m.Court).ThenInclude(c => c!.PricingRules)
                    .Include(m => m.Booking)
                    .Include(m => m.Sport)
                    .Include(m => m.Participants).ThenInclude(p => p.User);

                matches = StatusFilter switch
                {
                    "Joined" => await baseQuery
                        .Where(m => m.Participants.Any(p => p.UserID == currentUserId && p.JoinStatus == "Accepted"))
                        .OrderByDescending(m => m.MatchDate).Take(200).ToListAsync(),
                    "Owned" => await baseQuery
                        .Where(m => m.CreatedByUserID == currentUserId)
                        .OrderByDescending(m => m.MatchDate).Take(200).ToListAsync(),
                    _ => await baseQuery
                        .Where(m => m.Participants.Any(p => p.UserID == currentUserId && p.JoinStatus == "Pending"))
                        .OrderByDescending(m => m.MatchDate).Take(200).ToListAsync()
                };
            }
            else
            {
                matches = currentUserId > 0
                    ? await _matchService.GetRecommendedMatchesForUserAsync(currentUserId, 200)
                    : await _matchService.GetRecommendedMatchesAsync(200);
            }

            SportOptions = matches
                .Select(m => NormalizeSportName(m.Sport?.SportName))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                matches = matches
                    .Where(m => (m.Title != null && m.Title.Contains(Q, StringComparison.OrdinalIgnoreCase))
                             || (m.Description != null && m.Description.Contains(Q, StringComparison.OrdinalIgnoreCase))
                             || (m.CustomCourtAddress != null && m.CustomCourtAddress.Contains(Q, StringComparison.OrdinalIgnoreCase))
                             || (m.Court?.Venue?.VenueName != null && m.Court.Venue.VenueName.Contains(Q, StringComparison.OrdinalIgnoreCase))
                             || (m.Sport?.SportName != null && m.Sport.SportName.Contains(Q, StringComparison.OrdinalIgnoreCase))
                             || (m.MatchType != null && m.MatchType.Contains(Q, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(Sport))
            {
                matches = matches
                    .Where(m => string.Equals(NormalizeSportName(m.Sport?.SportName), Sport, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(Skill) && !string.Equals(Skill, "Any", StringComparison.OrdinalIgnoreCase))
            {
                matches = matches
                    .Where(m => MatchesSkillFilter(m.SkillRequired, Skill))
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

            if (!string.IsNullOrWhiteSpace(Location))
            {
                matches = matches.Where(m =>
                {
                    var allText = string.Join(" ", new[] {
                        m.CustomCourtName,
                        m.CustomCourtAddress,
                        m.Court?.Venue?.VenueName,
                        m.Court?.Venue?.Address
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    return allText.Contains(Location, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // Distance filter — Starter/Pro/Club only
            if (CanFilterByDistance && UserLat.HasValue && UserLon.HasValue)
            {
                var radiusKm = (double)(Radius ?? 10m);
                matches = matches.Where(m =>
                {
                    var lat = (double?)m.CustomLatitude ?? (double?)m.Court?.Venue?.Latitude;
                    var lon = (double?)m.CustomLongitude ?? (double?)m.Court?.Venue?.Longitude;
                    // Trận thiếu tọa độ vẫn hiện (không loại oan) — sẽ không có DistanceDisplay
                    if (lat == null || lon == null) return true;
                    return HaversineKm(UserLat.Value, UserLon.Value, lat.Value, lon.Value) <= radiusKm;
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
                        if (!amount.HasValue) return !MinPrice.HasValue || MinPrice.Value <= 0;
                        // Trận chia đều: card hiển thị giá/người nên filter cũng phải so giá/người
                        if (m.PriceMode == "SplitEven" && m.MaxParticipants > 0)
                            amount = Math.Ceiling(amount.Value / m.MaxParticipants);
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
                var isApproved = myParticipation?.JoinStatus == "Approved";
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
                    CourtNumber = m.CourtNumber,
                    PriceDisplay = BuildPriceDisplay(m),
                    PriceAmount = BuildMatchPriceAmount(m),
                    Participants = acceptedCount,
                    MaxParticipants = m.MaxParticipants,
                    IsJoinedByCurrentUser = isJoined,
                    IsPendingByCurrentUser = isPending,
                    IsApprovedByHost = isApproved,
                    IsOwnedByCurrentUser = currentUserId > 0 && m.CreatedByUserID == currentUserId,
                    IsLockedByHost = m.IsLockedByHost,
                    CanJoin = currentUserId > 0 && myParticipation == null && m.Status == "Open" && !m.IsLockedByHost && acceptedCount < m.MaxParticipants,
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

            // For users without location, sort purely by score
            if (Matches.All(m => m.DistanceKm == null))
                Matches = Matches.OrderByDescending(m => m.MatchScore).ToList();

            // AI boost for Pro/Club users
            if (HasAiFeature && Matches.Count >= 3)
                await ApplyAiBoostAsync(currentUserId);

            TotalMatchCount = Matches.Count;
            if (PageNumber < 1) PageNumber = 1;
            Matches = Matches
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            await LoadJoinedMatchesAsync(currentUserId);
            await LoadCommunityListingsAsync();
        }

        private async Task LoadCommunityListingsAsync()
        {
            var listings = await _context.CommunityListings
                .Include(c => c.Sport)
                .Where(c => c.Status == "Active" && c.ExpiresAt > DateTime.UtcNow)
                .OrderBy(c => c.MatchDate ?? DateOnly.MaxValue)
                .Take(100)
                .ToListAsync();

            // Áp cùng bộ lọc như trận thật. Nguyên tắc: tin thiếu dữ liệu ở trường đang lọc thì GIỮ lại
            // (không loại oan) — trừ giá, mô phỏng đúng cách filter trận thật xử lý trận không có giá.
            if (!string.IsNullOrWhiteSpace(Sport))
                listings = listings.Where(c => c.Sport != null
                    && string.Equals(NormalizeSportName(c.Sport.SportName), Sport, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(Q))
                listings = listings.Where(c => CommunityContains(c.Title, Q) || CommunityContains(c.VenueName, Q)
                    || CommunityContains(c.Address, Q) || CommunityContains(c.RawText, Q)).ToList();

            if (!string.IsNullOrWhiteSpace(Skill) && !string.Equals(Skill, "Any", StringComparison.OrdinalIgnoreCase))
                listings = listings.Where(c => MatchesSkillFilter(c.SkillRequired, Skill)).ToList();

            if (MatchDate.HasValue)
            {
                var d = DateOnly.FromDateTime(MatchDate.Value);
                listings = listings.Where(c => !c.MatchDate.HasValue || c.MatchDate.Value == d).ToList();
            }

            if (!string.IsNullOrWhiteSpace(TimeFrom) && TimeSpan.TryParse(TimeFrom, out var fromTime))
                listings = listings.Where(c => !c.StartTime.HasValue || c.StartTime.Value >= fromTime).ToList();

            if (!string.IsNullOrWhiteSpace(TimeTo) && TimeSpan.TryParse(TimeTo, out var toTime))
                listings = listings.Where(c => !c.StartTime.HasValue || c.StartTime.Value <= toTime).ToList();

            if (!string.IsNullOrWhiteSpace(Time) && !string.Equals(Time, "Any", StringComparison.OrdinalIgnoreCase))
                listings = listings.Where(c => !c.StartTime.HasValue || Time switch
                {
                    "Morning" => c.StartTime.Value < new TimeSpan(12, 0, 0),
                    "Afternoon" => c.StartTime.Value >= new TimeSpan(12, 0, 0) && c.StartTime.Value < new TimeSpan(17, 0, 0),
                    "Evening" => c.StartTime.Value >= new TimeSpan(17, 0, 0),
                    _ => true
                }).ToList();

            if (!string.IsNullOrWhiteSpace(Location))
                listings = listings.Where(c =>
                {
                    var all = string.Join(" ", new[] { c.VenueName, c.Address }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    return all.Contains(Location, StringComparison.OrdinalIgnoreCase);
                }).ToList();

            if (MinPrice.HasValue || MaxPrice.HasValue)
                listings = listings.Where(c =>
                {
                    var amount = CommunityPriceAmount(c);
                    if (!amount.HasValue) return !MinPrice.HasValue || MinPrice.Value <= 0;
                    if (MinPrice.HasValue && amount.Value < MinPrice.Value) return false;
                    if (MaxPrice.HasValue && amount.Value > MaxPrice.Value) return false;
                    return true;
                }).ToList();

            if (CanFilterByDistance && UserLat.HasValue && UserLon.HasValue)
            {
                var radiusKm = (double)(Radius ?? 10m);
                listings = listings.Where(c =>
                {
                    if (!c.Latitude.HasValue || !c.Longitude.HasValue) return true; // thiếu tọa độ vẫn hiện
                    return HaversineKm(UserLat.Value, UserLon.Value, (double)c.Latitude.Value, (double)c.Longitude.Value) <= radiusKm;
                }).ToList();
            }

            CommunityListings = listings.Take(30).Select(c => new CommunityListingCardItem
            {
                Title = string.IsNullOrWhiteSpace(c.Title) ? "Tin tuyển vãng lai" : c.Title,
                SportName = c.Sport?.SportName,
                ParseStatus = c.ParseStatus,
                StartText = BuildCommunityStartText(c),
                Venue = string.IsNullOrWhiteSpace(c.VenueName) ? c.Address : c.VenueName,
                SkillRequired = c.SkillRequired,
                PriceDisplay = BuildCommunityPriceDisplay(c),
                RawTextPreview = TruncateRaw(c.RawText, 220),
                SourceAuthorName = c.SourceAuthorName,
                SourceUrl = c.SourceUrl,
                Latitude = c.Latitude,
                Longitude = c.Longitude
            }).ToList();
        }

        private static bool CommunityContains(string? haystack, string needle) =>
            !string.IsNullOrEmpty(haystack) && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

        private static decimal? CommunityPriceAmount(SportHub.Models.Entities.CommunityListing c)
        {
            if (c.CostMaleVnd.HasValue && c.CostFemaleVnd.HasValue)
                return Math.Min(c.CostMaleVnd.Value, c.CostFemaleVnd.Value);
            return c.CostMaleVnd ?? c.CostFemaleVnd;
        }

        private static string? BuildCommunityStartText(SportHub.Models.Entities.CommunityListing c)
        {
            var vi = new CultureInfo("vi-VN");
            if (c.MatchDate.HasValue && c.StartTime.HasValue)
                return $"{c.MatchDate.Value.ToDateTime(TimeOnly.MinValue).ToString("ddd, dd/MM", vi)} · {c.StartTime.Value:hh\\:mm}";
            if (c.MatchDate.HasValue)
                return c.MatchDate.Value.ToDateTime(TimeOnly.MinValue).ToString("ddd, dd/MM", vi);
            if (c.StartTime.HasValue)
                return c.StartTime.Value.ToString(@"hh\:mm");
            return null;
        }

        private static string? BuildCommunityPriceDisplay(SportHub.Models.Entities.CommunityListing c)
        {
            if (c.CostMaleVnd.HasValue && c.CostFemaleVnd.HasValue)
                return $"Nam {c.CostMaleVnd.Value:N0}đ · Nữ {c.CostFemaleVnd.Value:N0}đ";
            if (c.CostMaleVnd.HasValue) return $"{c.CostMaleVnd.Value:N0}đ";
            if (c.CostFemaleVnd.HasValue) return $"{c.CostFemaleVnd.Value:N0}đ";
            return null;
        }

        private static string TruncateRaw(string text, int maxLen) =>
            text.Length <= maxLen ? text : text[..maxLen] + "…";

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

            var joinResult = await _matchService.JoinMatchAsync(id, userId);
            if (joinResult.Success)
            {
                SuccessMessage = "Yêu cầu đã gửi. Bạn đang trong hàng chờ - host sẽ duyệt trong vòng 2 giờ.";
                var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "Người chơi";
                await _notificationService.CreateAsync(
                    match.CreatedByUserID,
                    "MatchJoin",
                    "Có người muốn tham gia trận",
                    $"{currentUser} gửi yêu cầu tham gia trận \"{match.Title ?? match.MatchType}\". Vui lòng duyệt trong 2 giờ.",
                    $"/Matchmaking/Details?id={id}");
                await _hubContext.Clients.Group($"user:{match.CreatedByUserID}").SendAsync("match_join_request", new {
                    matchId = id,
                    matchTitle = match.Title ?? match.MatchType,
                    playerName = currentUser,
                    playerUserId = userId
                });

                if (match.CreatedByUser?.NotifyByEmail == true && match.CreatedByUser.NotifyMatchJoinRequest && !string.IsNullOrWhiteSpace(match.CreatedByUser.Email))
                {
                    var baseUrl = (_config["App:BaseUrl"] ?? "https://sporthub-dn.id.vn/").TrimEnd('/');
                    await _emailService.SendMatchJoinRequestAsync(match.CreatedByUser.Email, match.CreatedByUser.FullName,
                        match.Title ?? match.MatchType, currentUser, $"{baseUrl}/Matchmaking/Details?id={id}");
                }
            }
            else
            {
                ErrorMessage = joinResult.Reason.ToUserMessage();
                if (joinResult.Reason == JoinMatchReason.ProfileIncomplete)
                    TempData["OpenProfileModal"] = true;
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
                PendingRequestMatches = new();
                MyHostedMatches = new();
                return;
            }

            var today = DateTime.Today;

            // Accepted player matches (existing sections)
            var joined = await _context.MatchParticipants
                .Where(mp => mp.UserID == userId && mp.JoinStatus == "Accepted")
                .Include(mp => mp.Match).ThenInclude(m => m.Court).ThenInclude(c => c!.Venue)
                .OrderByDescending(mp => mp.Match.MatchDate)
                .ThenByDescending(mp => mp.Match.StartTime)
                .ToListAsync();

            var mapped = joined.Select(mp => new JoinedMatchItem
            {
                MatchId = mp.MatchID,
                Title = string.IsNullOrWhiteSpace(mp.Match.Title) ? $"{mp.Match.MatchType} Match" : mp.Match.Title,
                StartText = $"{mp.Match.MatchDate:ddd, dd MMM} {mp.Match.StartTime:hh\\:mm}",
                Venue = BuildVenueName(mp.Match),
                MatchDate = mp.Match.MatchDate,
                MatchStatus = mp.Match.Status
            }).ToList();

            UpcomingJoinedMatches = mapped.Where(m => m.MatchDate >= today).Take(5).ToList();
            JoinedMatchHistory = mapped.Where(m => m.MatchDate < today).Take(5).ToList();

            // Pending join requests (player waiting for host approval)
            var pendingParticipants = await _context.MatchParticipants
                .Where(mp => mp.UserID == userId && mp.JoinStatus == "Pending")
                .Include(mp => mp.Match).ThenInclude(m => m.CreatedByUser)
                .OrderBy(mp => mp.Match.MatchDate)
                .ToListAsync();

            PendingRequestMatches = pendingParticipants
                .Where(mp => mp.Match.Status != "Cancelled" && mp.Match.Status != "Completed")
                .Select(mp => new PendingRequestItem
                {
                    MatchId = mp.MatchID,
                    Title = string.IsNullOrWhiteSpace(mp.Match.Title) ? $"{mp.Match.MatchType} Match" : mp.Match.Title,
                    StartText = $"{mp.Match.MatchDate:ddd, dd/MM} {mp.Match.StartTime:hh\\:mm}",
                    HostName = mp.Match.CreatedByUser?.FullName ?? "Host",
                    MatchDate = mp.Match.MatchDate,
                    MatchStatus = mp.Match.Status
                }).ToList();

            // Host's own matches (upcoming + recent 30 days + all cancelled)
            var cutoff = today.AddDays(-30);
            var hostedMatches = await _context.Matches
                .Where(m => m.CreatedByUserID == userId &&
                            (m.MatchDate >= cutoff || m.Status == "Cancelled"))
                .Include(m => m.Participants)
                .OrderBy(m => m.MatchDate)
                .Take(15)
                .ToListAsync();

            MyHostedMatches = hostedMatches.Select(m => new MyHostMatchItem
            {
                MatchId = m.MatchID,
                Title = string.IsNullOrWhiteSpace(m.Title) ? $"{m.MatchType} Match" : m.Title,
                StartText = $"{m.MatchDate:ddd, dd/MM} {m.StartTime:hh\\:mm}",
                Status = m.Status,
                PendingCount = m.Participants.Count(p => p.JoinStatus == "Pending"),
                AcceptedCount = m.Participants.Count(p => p.JoinStatus == "Accepted"),
                MaxParticipants = m.MaxParticipants,
                MatchDate = m.MatchDate
            }).ToList();
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
                .OrderByDescending(m => m.MatchScore)
                .ThenBy(m => m.DistanceKm ?? double.MaxValue)
                .ToList();
        }

        private async Task ApplyAiBoostAsync(int userId)
        {
            try
            {
                var top5 = Matches.Take(5).ToList();
                var matchSummaries = string.Join("\n", top5.Select((m, i) =>
                    $"[{i}] id={m.MatchId} sport={m.SportName} skill={m.SkillRequired} score={m.MatchScore} dist={m.DistanceDisplay}"));

                var systemPrompt = "You are a sports match recommender. Return ONLY a JSON array, no other text.";
                var userMessage = $"Given these matches, return a JSON array of objects {{idx, boost}} where boost is -5 to 10 (higher=better fit):\n{matchSummaries}";

                var json = await _aiChat.ChatAsync(systemPrompt, new List<AiChatHistoryItem>(), userMessage);
                if (string.IsNullOrWhiteSpace(json)) return;

                // Parse JSON array of {idx, boost}
                var start = json.IndexOf('[');
                var end = json.LastIndexOf(']');
                if (start < 0 || end <= start) return;

                var arr = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(json[start..(end + 1)]);
                if (arr == null) return;

                foreach (var el in arr)
                {
                    if (!el.TryGetProperty("idx", out var idxEl) || !el.TryGetProperty("boost", out var boostEl)) continue;
                    var idx = idxEl.GetInt32();
                    var boost = boostEl.GetInt32();
                    if (idx >= 0 && idx < top5.Count)
                    {
                        var blended = (int)Math.Round(top5[idx].MatchScore * 0.75 + boost * 2.5);
                        top5[idx].MatchScore = Math.Clamp(blended, 35, 99);
                    }
                }

                // Re-sort after boost
                Matches = Matches
                    .OrderByDescending(m => m.MatchScore)
                    .ThenBy(m => m.DistanceKm ?? double.MaxValue)
                    .ToList();
            }
            catch
            {
                // AI boost is best-effort — ignore errors silently
            }
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
            if (match.PriceMode == "ByGender" && (match.PriceMaleVnd.HasValue || match.PriceFemaleVnd.HasValue))
            {
                var parts = new List<string>();
                if (match.PriceMaleVnd.HasValue) parts.Add($"Nam {match.PriceMaleVnd.Value:N0}đ");
                if (match.PriceFemaleVnd.HasValue) parts.Add($"Nữ {match.PriceFemaleVnd.Value:N0}đ");
                return string.Join(" · ", parts);
            }

            if (match.PriceMode == "SplitEven") return "Chia đều cuối buổi";

            var amount = BuildMatchPriceAmount(match);
            if (amount.HasValue)
            {
                if (match.Booking?.FinalAmount > 0)
                    return $"Tổng: {amount.Value:N0} xu";

                if (match.CustomPriceVnd.HasValue)
                    return $"{amount.Value:N0} xu/người";

                return $"Từ {amount.Value:N0} xu/giờ";
            }

            return "Chưa có giá";
        }

        // Màu nhận diện riêng từng môn — dùng cho dải màu card + chip môn (sports identity)
        public static string GetSportColor(string? sportName)
        {
            var s = (sportName ?? "").ToLowerInvariant();
            if (s.Contains("cầu lông") || s.Contains("badminton")) return "#059669";
            if (s.Contains("bóng bàn") || s.Contains("table tennis") || s.Contains("ping")) return "#2563eb";
            if (s.Contains("bóng đá") || s.Contains("football") || s.Contains("soccer")) return "#16a34a";
            if (s.Contains("tennis")) return "#d97706";
            if (s.Contains("pickleball")) return "#7c3aed";
            if (s.Contains("bóng rổ") || s.Contains("basketball")) return "#ea580c";
            return "#50A5B1";
        }

        public static string GetSportEmoji(string? sportName)
        {
            var s = (sportName ?? "").ToLowerInvariant();
            if (s.Contains("cầu lông") || s.Contains("badminton")) return "🏸";
            if (s.Contains("bóng bàn") || s.Contains("table tennis") || s.Contains("ping")) return "🏓";
            if (s.Contains("bóng đá") || s.Contains("football") || s.Contains("soccer")) return "⚽";
            if (s.Contains("tennis")) return "🎾";
            if (s.Contains("pickleball")) return "🥒";
            if (s.Contains("bóng rổ") || s.Contains("basketball")) return "🏀";
            if (s.Contains("bơi") || s.Contains("swim")) return "🏊";
            return "🏅";
        }

        // Trận cầu lông lưu SkillRequired composite "Nam:Yếu,Trung Bình|Nữ:Yếu" — so exact string sẽ không bao giờ khớp.
        private static bool MatchesSkillFilter(string? skillRequired, string filter)
        {
            if (string.IsNullOrWhiteSpace(skillRequired)) return true;
            skillRequired = skillRequired.Trim();
            if (skillRequired.Equals("Any", StringComparison.OrdinalIgnoreCase)) return true;

            if (!skillRequired.Contains(':'))
                return string.Equals(skillRequired, filter, StringComparison.OrdinalIgnoreCase);

            foreach (var part in skillRequired.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = part.IndexOf(':');
                var levels = idx >= 0 ? part[(idx + 1)..] : part;
                foreach (var level in levels.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = level.Trim();
                    if (trimmed.Equals("Any", StringComparison.OrdinalIgnoreCase)) return true;
                    if (trimmed.Equals(filter, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        private static decimal? BuildMatchPriceAmount(Models.Entities.Match match)
        {
            // ByGender: lấy giá thấp hơn giữa Nam/Nữ để filter khoảng giá không loại oan
            // trận mà 1 trong 2 giới tính vẫn nằm trong khoảng lọc
            if (match.PriceMode == "ByGender")
            {
                if (match.PriceMaleVnd.HasValue && match.PriceFemaleVnd.HasValue)
                    return Math.Min(match.PriceMaleVnd.Value, match.PriceFemaleVnd.Value);
                if (match.PriceMaleVnd.HasValue || match.PriceFemaleVnd.HasValue)
                    return match.PriceMaleVnd ?? match.PriceFemaleVnd;
            }

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

            var line = lines.FirstOrDefault(l => l.StartsWith("Match price xu:", StringComparison.OrdinalIgnoreCase));
            if (line == null) return null;

            var raw = line.Replace("Match price xu:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
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
            value = value.Trim();

            // Composite cầu lông "Nam:Yếu,Trung Bình|Nữ:Yếu" → lấy hạng thấp nhất được chấp nhận
            if (value.Contains(':'))
            {
                int? best = null;
                foreach (var part in value.Split('|', StringSplitOptions.RemoveEmptyEntries))
                {
                    var idx = part.IndexOf(':');
                    var levels = idx >= 0 ? part[(idx + 1)..] : part;
                    foreach (var level in levels.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var rank = SkillToRank(level);
                        if (rank.HasValue && (best == null || rank < best)) best = rank;
                    }
                }
                return best;
            }

            return value.ToLowerInvariant() switch
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
            public string? CourtNumber { get; set; }
            public string PriceDisplay { get; set; } = string.Empty;
            public decimal? PriceAmount { get; set; }
            public int Participants { get; set; }
            public int MaxParticipants { get; set; }
            public bool IsJoinedByCurrentUser { get; set; }
            public bool IsPendingByCurrentUser { get; set; }
            public bool IsApprovedByHost { get; set; }
            public bool IsOwnedByCurrentUser { get; set; }
            public bool IsLockedByHost { get; set; }
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

        // Tin tuyển vãng lai cào từ Facebook (Chrome Extension) — chỉ tham khảo, dẫn traffic ra bài gốc,
        // không có luồng Join/Skip trong platform như MatchCardItem.
        public class CommunityListingCardItem
        {
            public string Title { get; set; } = string.Empty;
            public string? SportName { get; set; }
            public string ParseStatus { get; set; } = "Parsed";
            public string? StartText { get; set; }
            public string? Venue { get; set; }
            public string? SkillRequired { get; set; }
            public string? PriceDisplay { get; set; }
            public string RawTextPreview { get; set; } = string.Empty;
            public string? SourceAuthorName { get; set; }
            public string SourceUrl { get; set; } = string.Empty;
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
        }

        public class JoinedMatchItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string StartText { get; set; } = string.Empty;
            public string Venue { get; set; } = string.Empty;
            public DateTime MatchDate { get; set; }
            public string MatchStatus { get; set; } = string.Empty;
        }

        public class PendingRequestItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string StartText { get; set; } = string.Empty;
            public string HostName { get; set; } = string.Empty;
            public DateTime MatchDate { get; set; }
            public string MatchStatus { get; set; } = string.Empty;
        }

        public class MyHostMatchItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string StartText { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int PendingCount { get; set; }
            public int AcceptedCount { get; set; }
            public int MaxParticipants { get; set; }
            public DateTime MatchDate { get; set; }
        }
    }
}

