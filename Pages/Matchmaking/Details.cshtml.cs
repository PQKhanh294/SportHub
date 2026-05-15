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
        private readonly INotificationService _notificationService;

        public DetailsModel(IMatchService matchService, INotificationService notificationService)
        {
            _matchService = matchService;
            _notificationService = notificationService;
        }

        public MatchDetailItem? Item { get; set; }

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
            var isOwner    = currentUserId > 0 && match.CreatedByUserID == currentUserId;

            var acceptedCount = match.Participants.Count(p => p.JoinStatus == "Accepted");
            var canJoin = currentUserId > 0
                          && myParticipant == null
                          && match.Status == "Open"
                          && acceptedCount < match.MaxParticipants;

            var customCourtName    = ExtractCustomCourtName(match.Description);
            var customCourtAddress = ExtractCustomCourtAddress(match.Description);
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

            Item = new MatchDetailItem
            {
                MatchId            = match.MatchID,
                Title              = string.IsNullOrWhiteSpace(match.Title) ? $"{match.MatchType} Match" : match.Title,
                MatchType          = string.IsNullOrWhiteSpace(match.MatchType) ? "Open Match" : match.MatchType,
                SkillRequired      = string.IsNullOrWhiteSpace(match.SkillRequired) ? "Any" : match.SkillRequired,
                DateText           = $"{match.MatchDate:dddd, dd MMM yyyy}",
                TimeText           = $"{match.StartTime:hh\\:mm} - {match.EndTime:hh\\:mm}",
                Venue              = fallbackLocation ?? match.Court?.Venue?.VenueName ?? "TBD Venue",
                CourtName          = customCourtName ?? match.Court?.CourtName ?? "Not specified",
                CourtAddress       = customCourtAddress ?? match.Court?.Venue?.Address ?? "Not specified",
                PriceDisplay       = BuildPriceDisplay(match),
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
                SpotsLeft          = Math.Max(0, match.MaxParticipants - acceptedCount),
                IsJoined           = isAccepted,
                IsPending          = isPending,
                CanJoin            = canJoin,
                IsOwner            = isOwner,
                RequiresApproval   = match.RequiresApproval,
                HostName           = match.CreatedByUser?.FullName ?? "Host",
                HostAvatar         = string.IsNullOrWhiteSpace(match.CreatedByUser?.AvatarUrl)
                    ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(match.CreatedByUser?.FullName ?? "H")}&background=E2E8F0&color=1E293B&size=64"
                    : match.CreatedByUser!.AvatarUrl
            };

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
                if (match.RequiresApproval)
                {
                    TempData["SuccessMessage"] = "Yêu cầu đã được gửi. Vui lòng chờ host duyệt.";
                    // Notify host
                    var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "Someone";
                    await _notificationService.CreateAsync(
                        match.CreatedByUserID,
                        "MatchJoin",
                        "Yêu cầu tham gia trận đấu",
                        $"{currentUser} muốn tham gia trận \"{match.Title ?? match.MatchType}\".",
                        $"/Matchmaking/Details?id={id}");
                }
                else
                {
                    TempData["SuccessMessage"] = "Bạn đã tham gia trận đấu thành công!";
                }
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
                TempData["SuccessMessage"] = "Đã duyệt người chơi.";
                // Notify the approved user
                var participant = (await _matchService.GetMatchDetailsAsync(id))
                    ?.Participants.FirstOrDefault(p => p.ParticipantID == participantId);
                if (participant != null)
                {
                    var match = await _matchService.GetMatchDetailsAsync(id);
                    await _notificationService.CreateAsync(
                        participant.UserID,
                        "MatchApprove",
                        "Yêu cầu tham gia được chấp thuận",
                        $"Host đã duyệt bạn vào trận \"{match?.Title ?? match?.MatchType}\".",
                        $"/Matchmaking/Details?id={id}");
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

        private static string BuildPriceDisplay(Models.Entities.Match match)
        {
            var customPrice = ExtractCustomPrice(match.Description);
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
            public bool CanJoin { get; set; }
            public bool IsOwner { get; set; }
            public bool RequiresApproval { get; set; }
            public string HostName { get; set; } = string.Empty;
            public string HostAvatar { get; set; } = string.Empty;
        }

        public class ParticipantItem
        {
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
