using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Globalization;
using System.Text.RegularExpressions;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    public class DetailsModel : PageModel
    {
        private readonly IMatchService _matchService;
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly INotificationService _notificationService;
        private readonly IMatchReviewService _reviewService;

        public DetailsModel(
            IMatchService matchService,
            IMatchPaymentService matchPaymentService,
            INotificationService notificationService,
            IMatchReviewService reviewService)
        {
            _matchService = matchService;
            _matchPaymentService = matchPaymentService;
            _notificationService = notificationService;
            _reviewService = reviewService;
        }

        public MatchDetailItem? Item { get; set; }
        public bool CanReviewAsPlayer { get; set; }
        public bool CanReviewAsHost { get; set; }
        public UserRatingSummary? HostRating { get; set; }
        public List<MatchReviewHistoryItem> PublicReviews { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            ViewData["ActivePage"] = "Matchmaking";
            if (!id.HasValue)
                return RedirectToPage("/Matchmaking/Index");

            var match = await _matchService.GetMatchDetailsAsync(id.Value);
            if (match == null)
                return RedirectToPage("/Matchmaking/Index");

            var currentUserId = GetCurrentUserId();
            var myParticipant = currentUserId > 0
                ? match.Participants.FirstOrDefault(p => p.UserID == currentUserId)
                : null;

            var isAccepted = myParticipant?.JoinStatus == "Accepted";
            var isPending  = myParticipant?.JoinStatus == "Pending";
            var isApproved = myParticipant?.JoinStatus == "Approved";
            var isOwner    = currentUserId > 0 && match.CreatedByUserID == currentUserId;

            var acceptedCount = match.Participants.Count(p => p.JoinStatus == "Accepted");
            var filledCount = match.Participants.Count(p => p.JoinStatus == "Accepted" || p.JoinStatus == "Approved");
            var canJoin = currentUserId > 0
                          && myParticipant == null
                          && match.Status == "Open"
                          && filledCount < match.MaxParticipants;

            var customCourtName    = match.CustomCourtName ?? ExtractCustomCourtName(match.Description);
            var customCourtAddress = match.CustomCourtAddress ?? ExtractCustomCourtAddress(match.Description);
            var fallbackLocation   = BuildCustomLocation(customCourtName, customCourtAddress);

            // Danh sách chờ duyệt (chỉ host xem)
            var pendingParticipants = isOwner
                ? match.Participants
                    .Where(p => p.JoinStatus == "Pending")
                    .Select(p => new PendingParticipantItem
                    {
                        ParticipantId = p.ParticipantID,
                        UserId        = p.UserID,
                        FullName      = p.User.FullName,
                        SkillLevel    = p.User.SkillLevel ?? "Unknown",
                        AvatarUrl     = string.IsNullOrWhiteSpace(p.User.AvatarUrl)
                            ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(p.User.FullName)}&background=E2E8F0&color=1E293B&size=64"
                            : p.User.AvatarUrl,
                        JoinedAt = p.JoinedAt
                    }).ToList()
                : new();

            var depositAmount = _matchPaymentService.CalculateHostDeposit(match.MaxParticipants);

            Item = new MatchDetailItem
            {
                MatchId            = match.MatchID,
                Title              = string.IsNullOrWhiteSpace(match.Title) ? $"{match.MatchType} Match" : match.Title,
                MatchType          = string.IsNullOrWhiteSpace(match.MatchType) ? "Open Match" : match.MatchType,
                SportName          = match.Sport?.SportName ?? "Sport",
                SkillRequired      = GetSkillDisplay(match.SkillRequired),
                DateText           = $"{match.MatchDate:dddd, dd MMM yyyy}",
                TimeText           = $"{match.StartTime:hh\\:mm} - {match.EndTime:hh\\:mm}",
                Venue              = fallbackLocation ?? match.Court?.Venue?.VenueName ?? "TBD Venue",
                CourtName          = customCourtName ?? match.Court?.CourtName ?? "Not specified",
                CourtAddress       = customCourtAddress ?? match.Court?.Venue?.Address ?? "Not specified",
                PriceDisplay       = BuildPriceDisplay(match),
                CourtImageUrl      = match.Sport?.SportName != null && (match.Sport.SportName.Contains("Cầu lông", StringComparison.OrdinalIgnoreCase) || match.Sport.SportName.Contains("Badminton", StringComparison.OrdinalIgnoreCase))
                                    ? "/images/badminton_bg.png"
                                    : (match.Court?.Images.OrderBy(i => i.SortOrder).FirstOrDefault(i => i.IsMain)?.ImageUrl
                                    ?? match.Court?.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.ImageUrl
                                    ?? "https://lh3.googleusercontent.com/aida-public/AB6AXuBuyetq-CjYniznRR2LQD8js1JDRvgAYveuoCNhfGsauk1CP-z2TywH1K-Xw5PoaJMxXqGfqFgGgDkMEgzi9rE2IjqCKCiiBCpyawFJCUII3zFhqq7ebqAlP1YZux0CAcfosVIK1Ru7RwxZqtC-tnt4OKa5N3qc919AjX0s1MzA5BQdsghli12q44PqbEusnjtcTT0Z1PbUvJZ-pHdJoDiRLPBOMKoQZYZLlGQ2lrBdViOvLJ4XVCkjH6QzdDesUC1Qk_Z_pnK3_GE"),
                Latitude           = match.CustomLatitude ?? ExtractCustomLatitude(match.Description) ?? match.Court?.Venue?.Latitude,
                Longitude          = match.CustomLongitude ?? ExtractCustomLongitude(match.Description) ?? match.Court?.Venue?.Longitude,
                AcceptedParticipants = match.Participants
                    .Where(p => p.JoinStatus == "Accepted")
                    .Select(p => new ParticipantItem
                    {
                        FullName  = p.User.FullName,
                        AvatarUrl = string.IsNullOrWhiteSpace(p.User.AvatarUrl)
                            ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(p.User.FullName)}&background=E2E8F0&color=1E293B&size=64"
                            : p.User.AvatarUrl,
                        SkillLevel = p.User.SkillLevel ?? "Unknown"
                    }).ToList(),
                PendingParticipants = pendingParticipants,
                MaxParticipants    = match.MaxParticipants,
                SpotsLeft          = Math.Max(0, match.MaxParticipants - filledCount),
                IsJoined           = isAccepted,
                IsPending          = isPending,
                IsApproved         = isApproved,
                CanJoin            = canJoin,
                IsOwner            = isOwner,
                RequiresApproval   = match.RequiresApproval,
                HostId             = match.CreatedByUserID,
                HostName           = match.CreatedByUser?.FullName ?? "Host",
                HostAvatar         = string.IsNullOrWhiteSpace(match.CreatedByUser?.AvatarUrl)
                    ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(match.CreatedByUser?.FullName ?? "H")}&background=E2E8F0&color=1E293B&size=64"
                    : match.CreatedByUser!.AvatarUrl,

                // Review state
                MatchStatus            = match.Status,
                ShowHostDepositPrompt  = isOwner && match.Status == "PendingDeposit",
                HostDepositAmount      = depositAmount,
                ShowPlayerFeePrompt    = isApproved && myParticipant?.PlayerFeeStatus == "AwaitingPayment",
                PlayerFeeDeadline      = myParticipant?.PlayerFeeDeadline,
                ShowRemainingFeePrompt = isOwner && match.RemainingFeeStatus == "Notified",
                RemainingFeeAmount     = depositAmount
            };

            // Review data
            if (currentUserId > 0)
            {
                CanReviewAsPlayer = await _reviewService.CanSubmitPlayerReviewAsync(id.Value, currentUserId);
                CanReviewAsHost = isOwner && (await _reviewService.GetPlayersToRateAsync(id.Value, currentUserId)).Any(p => !p.AlreadyRated);
            }
            HostRating = await _reviewService.GetHostRatingSummaryAsync(match.CreatedByUserID);
            PublicReviews = await _reviewService.GetReceivedReviewsAsync(match.CreatedByUserID);
            // Only show reviews from this match
            PublicReviews = PublicReviews.Where(r => r.MatchId == id.Value && r.ReviewType == "PlayerToMatch").ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostJoinAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return RedirectToPage("/Auth/Login", new { returnUrl = $"/Matchmaking/Details?id={id}" });

            var match = await _matchService.GetMatchDetailsAsync(id);
            if (match == null) return RedirectToPage("/Matchmaking/Index");

            var joined = await _matchService.JoinMatchAsync(id, userId);
            if (joined)
            {
                TempData["SuccessMessage"] = "Yêu cầu đã gửi. Bạn đang chờ host duyệt (tối đa 1 giờ).";
                var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "Người chơi";
                await _notificationService.CreateAsync(
                    match.CreatedByUserID,
                    "MatchJoin",
                    "Có người muốn tham gia trận",
                    $"{currentUser} gửi yêu cầu tham gia trận \"{match.Title ?? match.MatchType}\". Vui lòng duyệt trong 1 giờ.",
                    $"/Matchmaking/Details?id={id}");
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể tham gia trận đấu. Trận có thể đã đầy hoặc đã đóng.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostLeaveAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
                return RedirectToPage("/Auth/Login");

            var left = await _matchService.LeaveMatchAsync(id, userId);
            TempData[left ? "SuccessMessage" : "ErrorMessage"] = left
                ? "Bạn đã rời khỏi trận đấu."
                : "Không thể rời trận đấu.";

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostApproveAsync(int id, int participantId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var approved = await _matchService.ApproveParticipantAsync(id, participantId, userId);
            if (approved)
            {
                TempData["SuccessMessage"] = "Đã duyệt người chơi. Họ có 1 giờ để thanh toán phí 5,000 VND.";
                // Reload to get updated participant (JoinStatus = Approved)
                var updatedMatch = await _matchService.GetMatchDetailsAsync(id);
                var participant = updatedMatch?.Participants.FirstOrDefault(p => p.ParticipantID == participantId);
                if (participant != null)
                {
                    var deadlineStr = participant.PlayerFeeDeadline.HasValue
                        ? participant.PlayerFeeDeadline.Value.ToLocalTime().ToString("HH:mm dd/MM")
                        : "1 giờ tới";
                    await _notificationService.CreateAsync(
                        participant.UserID,
                        "MatchPaymentRequired",
                        "Bạn được duyệt — Thanh toán phí tham gia",
                        $"Host đã duyệt bạn vào trận \"{updatedMatch?.Title ?? updatedMatch?.MatchType}\". Thanh toán 5,000 VND trước {deadlineStr} để giữ chỗ.",
                        $"/Matchmaking/Payment?matchId={id}&type=playerfee");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể duyệt. Trận có thể đã đầy.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRejectAsync(int id, int participantId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var rejected = await _matchService.RejectParticipantAsync(id, participantId, userId);
            if (rejected)
            {
                TempData["SuccessMessage"] = "Đã từ chối người chơi.";
                var participant = (await _matchService.GetMatchDetailsAsync(id))
                    ?.Participants.FirstOrDefault(p => p.ParticipantID == participantId);
                if (participant != null)
                {
                    var match = await _matchService.GetMatchDetailsAsync(id);
                    await _notificationService.CreateAsync(
                        participant.UserID,
                        "MatchReject",
                        "Yêu cầu tham gia không được chấp thuận",
                        $"Host đã không duyệt yêu cầu của bạn vào trận \"{match?.Title ?? match?.MatchType}\".",
                        $"/Matchmaking/Details?id={id}");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể từ chối.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");

            var match = await _matchService.GetMatchDetailsAsync(id);
            if (match == null || match.CreatedByUserID != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa trận này.";
                return RedirectToPage(new { id });
            }

            var targetUsers = match.Participants
                .Where(p => p.UserID != userId && (p.JoinStatus == "Accepted" || p.JoinStatus == "Pending"))
                .Select(p => p.UserID)
                .Distinct()
                .ToList();

            var deleted = await _matchService.DeleteMatchAsync(id, userId);
            if (!deleted)
            {
                TempData["ErrorMessage"] = "Không thể xóa trận đấu.";
                return RedirectToPage(new { id });
            }

            var matchTitle = match.Title ?? match.MatchType;
            foreach (var uid in targetUsers)
            {
                await _notificationService.CreateAsync(
                    uid,
                    "System",
                    "Trận đấu đã bị hủy",
                    $"Host đã xóa trận \"{matchTitle}\". Yêu cầu/tham gia của bạn đã được hủy.",
                    "/Matchmaking/Index");
            }

            TempData["SuccessMessage"] = "Đã xóa trận đấu thành công.";
            return RedirectToPage("/Matchmaking/Index");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private static string? ExtractCustomCourtName(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return lines.FirstOrDefault(l => l.StartsWith("Court name:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court name:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
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

        private static string? ExtractCustomCourtAddress(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return lines.FirstOrDefault(l => l.StartsWith("Court address:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court address:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        private static string? BuildCustomLocation(string? courtName, string? courtAddress)
        {
            if (!string.IsNullOrWhiteSpace(courtName) && !string.IsNullOrWhiteSpace(courtAddress))
                return $"{courtName} - {courtAddress}";
            return !string.IsNullOrWhiteSpace(courtName) ? courtName : courtAddress;
        }

        private static decimal? ExtractCustomLatitude(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var line = lines.FirstOrDefault(l => l.StartsWith("Court latitude:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court latitude:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return decimal.TryParse(line, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static decimal? ExtractCustomLongitude(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var line = lines.FirstOrDefault(l => l.StartsWith("Court longitude:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("Court longitude:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return decimal.TryParse(line, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static string BuildPriceDisplay(Models.Entities.Match match)
        {
            var customPrice = match.CustomPriceVnd ?? ExtractCustomPrice(match.Description);
            if (customPrice.HasValue) return $"{customPrice.Value:N0} VND";
            if (match.Booking?.FinalAmount > 0) return $"{match.Booking.FinalAmount:N0} VND";
            if (match.Court?.PricingRules != null && match.Court.PricingRules.Any())
                return $"Từ {match.Court.PricingRules.Min(p => p.UnitPrice):N0} VND/giờ";
            return "Chưa xác định";
        }

        private static decimal? ExtractCustomPrice(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var line = lines.FirstOrDefault(l => l.StartsWith("Match price VND:", StringComparison.OrdinalIgnoreCase));
            if (line == null) return null;
            var raw = line.Replace("Match price VND:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            var normalized = Regex.Replace(raw, "[^0-9,.-]", string.Empty);
            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) return v;
            if (decimal.TryParse(normalized, NumberStyles.Number, new System.Globalization.CultureInfo("vi-VN"), out var vi)) return vi;
            return decimal.TryParse(normalized, NumberStyles.Number, new System.Globalization.CultureInfo("en-US"), out var en) ? en : null;
        }

        // ViewModel classes
        public class MatchDetailItem
        {
            public int MatchId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string MatchType { get; set; } = string.Empty;
            public string SportName { get; set; } = string.Empty;
            public string? CourtImageUrl { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public string SkillRequired { get; set; } = "Any";
            public string DateText { get; set; } = string.Empty;
            public string TimeText { get; set; } = string.Empty;
            public string Venue { get; set; } = string.Empty;
            public string CourtName { get; set; } = string.Empty;
            public string CourtAddress { get; set; } = string.Empty;
            public string PriceDisplay { get; set; } = string.Empty;
            public List<ParticipantItem> AcceptedParticipants { get; set; } = new();
            public List<PendingParticipantItem> PendingParticipants { get; set; } = new();
            public int MaxParticipants { get; set; }
            public int SpotsLeft { get; set; }
            public bool IsJoined { get; set; }
            public bool IsPending { get; set; }
            public bool IsApproved { get; set; }
            public bool CanJoin { get; set; }
            public bool IsOwner { get; set; }
            public bool RequiresApproval { get; set; }
            public int HostId { get; set; }
            public string HostName { get; set; } = string.Empty;
            public string HostAvatar { get; set; } = string.Empty;

            // Payment state
            public string MatchStatus { get; set; } = string.Empty;
            public bool ShowHostDepositPrompt { get; set; }
            public decimal HostDepositAmount { get; set; }
            public bool ShowPlayerFeePrompt { get; set; }
            public DateTime? PlayerFeeDeadline { get; set; }
            public bool ShowRemainingFeePrompt { get; set; }
            public decimal RemainingFeeAmount { get; set; }
        }

        public class ParticipantItem
        {
            public int UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string AvatarUrl { get; set; } = string.Empty;
            public string SkillLevel { get; set; } = string.Empty;
        }

        public class PendingParticipantItem
        {
            public int ParticipantId { get; set; }
            public int UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string SkillLevel { get; set; } = string.Empty;
            public string AvatarUrl { get; set; } = string.Empty;
            public DateTime JoinedAt { get; set; }
        }
    }
}
