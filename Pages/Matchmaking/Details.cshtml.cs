using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Globalization;
using System.Text.RegularExpressions;
using SportHub.Hubs;
using SportHub.Services.Interfaces;

namespace SportHub.Pages.Matchmaking
{
    public class DetailsModel : PageModel
    {
        private readonly IMatchService _matchService;
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly INotificationService _notificationService;
        private readonly IMatchReviewService _reviewService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IDisputeService _disputeService;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public DetailsModel(
            IMatchService matchService,
            IMatchPaymentService matchPaymentService,
            INotificationService notificationService,
            IMatchReviewService reviewService,
            ISubscriptionService subscriptionService,
            IDisputeService disputeService,
            IWebHostEnvironment env,
            IHubContext<NotificationHub> hubContext,
            IEmailService emailService,
            IConfiguration config)
        {
            _matchService = matchService;
            _matchPaymentService = matchPaymentService;
            _notificationService = notificationService;
            _reviewService = reviewService;
            _subscriptionService = subscriptionService;
            _disputeService = disputeService;
            _env = env;
            _hubContext = hubContext;
            _emailService = emailService;
            _config = config;
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
                            ? "/images/avatar-default.png"
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
                CourtNumber        = match.CourtNumber,
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
                        UserId    = p.UserID,
                        FullName  = p.User.FullName,
                        AvatarUrl = string.IsNullOrWhiteSpace(p.User.AvatarUrl)
                            ? "/images/avatar-default.png"
                            : p.User.AvatarUrl,
                        SkillLevel   = p.User.SkillLevel ?? "Unknown",
                        PhoneNumber  = p.User.ShowContactToTeammates ? p.User.PhoneNumber : null,
                        ZaloContact  = p.User.ShowContactToTeammates ? p.User.ZaloContact : null
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
                    ? "/images/avatar-default.png"
                    : match.CreatedByUser!.AvatarUrl,

                IsSplitFee      = match.IsSplitFee,
                IsRecurring     = match.IsRecurring,
                RecurringDays   = match.RecurringDays,
                IsLockedByHost  = match.IsLockedByHost,

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

            // Subscription gate
            bool canJoin = await _subscriptionService.CanJoinMatchAsync(userId);
            if (!canJoin)
            {
                var plan = await _subscriptionService.GetCurrentPlanAsync(userId);
                var count = await _subscriptionService.GetMonthlyJoinCountAsync(userId);
                TempData["ErrorMessage"] = $"Bạn đã tham gia {count}/{plan.MonthlyJoinLimit} trận trong tháng. Nâng cấp gói hoặc mua thêm credit để tiếp tục!";
                return RedirectToPage("/Subscription/Index");
            }

            // If Free plan hit limit but has credits, consume a credit
            var currentPlan = await _subscriptionService.GetCurrentPlanAsync(userId);
            bool usedCredit = false;
            if (currentPlan.PlanKey == "Free")
            {
                var joinCount = await _subscriptionService.GetMonthlyJoinCountAsync(userId);
                if (joinCount >= currentPlan.MonthlyJoinLimit)
                    usedCredit = await _subscriptionService.UseMatchCreditAsync(userId);
            }

            var joinResult = await _matchService.JoinMatchAsync(id, userId);
            if (joinResult.Success)
            {
                await _subscriptionService.RecordJoinAsync(userId);
                var creditNote = usedCredit ? " (1 credit đã được dùng)" : "";
                TempData["SuccessMessage"] = $"Yêu cầu đã gửi. Bạn đang chờ host duyệt (tối đa 1 giờ).{creditNote}";
                var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? "Người chơi";
                await _notificationService.CreateAsync(
                    match.CreatedByUserID,
                    "MatchJoin",
                    "Có người muốn tham gia trận",
                    $"{currentUser} gửi yêu cầu tham gia trận \"{match.Title ?? match.MatchType}\". Vui lòng duyệt trong 1 giờ.",
                    $"/Matchmaking/Details?id={id}");

                await _hubContext.Clients.Group($"user:{match.CreatedByUserID}").SendAsync("match_join_request", new
                {
                    matchId = id,
                    matchTitle = match.Title ?? match.MatchType,
                    playerName = currentUser,
                    playerUserId = userId
                });

                if (match.CreatedByUser?.NotifyByEmail == true && !string.IsNullOrWhiteSpace(match.CreatedByUser.Email))
                {
                    var baseUrl = (_config["App:BaseUrl"] ?? "https://sporthub-dn.id.vn/").TrimEnd('/');
                    await _emailService.SendMatchJoinRequestAsync(match.CreatedByUser.Email, match.CreatedByUser.FullName,
                        match.Title ?? match.MatchType, currentUser, $"{baseUrl}/Matchmaking/Details?id={id}");
                }
            }
            else
            {
                TempData["ErrorMessage"] = joinResult.Reason.ToUserMessage();
                if (joinResult.Reason == JoinMatchReason.ProfileIncomplete)
                    TempData["OpenProfileModal"] = true;
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
                TempData["SuccessMessage"] = "Đã duyệt người chơi. Họ có 1 giờ để thanh toán phí 5,000 xu.";
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
                        $"Host đã duyệt bạn vào trận \"{updatedMatch?.Title ?? updatedMatch?.MatchType}\". Thanh toán 5,000 xu trước {deadlineStr} để giữ chỗ.",
                        $"/Matchmaking/Payment?matchId={id}&type=playerfee");
                    await _hubContext.Clients.Group($"user:{participant.UserID}").SendAsync("match_join_approved", new {
                        matchId = id,
                        matchTitle = updatedMatch?.Title ?? updatedMatch?.MatchType ?? "Trận đấu",
                        paymentUrl = $"/Matchmaking/Payment?matchId={id}&type=fee"
                    });
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
                    await _hubContext.Clients.Group($"user:{participant.UserID}").SendAsync("match_join_rejected", new {
                        matchId = id,
                        matchTitle = match?.Title ?? match?.MatchType ?? "Trận đấu"
                    });
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

            if (match.Status is "Completed" or "InProgress")
            {
                TempData["ErrorMessage"] = "Không thể xóa trận đang diễn ra hoặc đã hoàn thành.";
                return RedirectToPage(new { id });
            }

            // Soft-delete: cancel + refund players, keep record for history/disputes
            var (success, message) = await _matchService.CancelMatchByHostAsync(id, userId, "Host đã xóa trận");
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Đã hủy trận. {message}"
                : message;

            return RedirectToPage("/Matchmaking/Index");
        }

        public async Task<IActionResult> OnPostLockAsync(int id)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");
            var (success, message) = await _matchService.LockMatchByHostAsync(id, userId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostHostCancelAsync(int id, string cancelReason)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");
            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy trận.";
                return RedirectToPage(new { id });
            }
            var (success, message) = await _matchService.CancelMatchByHostAsync(id, userId, cancelReason);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return success ? RedirectToPage("/Matchmaking/Index") : RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostSubmitDisputeAsync(int matchId, string disputeType, string description, List<IFormFile>? evidenceFiles)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToPage("/Auth/Login");
            if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 50)
            {
                TempData["ErrorMessage"] = "Vui lòng mô tả chi tiết khiếu nại (tối thiểu 50 ký tự).";
                return RedirectToPage(new { id = matchId });
            }

            var urls = new List<string>();
            foreach (var file in (evidenceFiles ?? new()).Where(f => f.Length > 0).Take(3))
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext) || file.Length > 5 * 1024 * 1024) continue;

                var folder = Path.Combine(_env.WebRootPath, "uploads", "disputes");
                Directory.CreateDirectory(folder);
                var fileName = $"dispute_{matchId}_{userId}_{Guid.NewGuid():N}{ext}";
                var savePath = Path.Combine(folder, fileName);
                await using var stream = System.IO.File.Create(savePath);
                await file.CopyToAsync(stream);
                urls.Add($"/uploads/disputes/{fileName}");
            }

            // Lưu JSON array để hỗ trợ nhiều ảnh; record cũ dạng URL đơn vẫn đọc được ở admin
            string? evidenceUrl = urls.Count switch
            {
                0 => null,
                _ => System.Text.Json.JsonSerializer.Serialize(urls)
            };

            await _disputeService.SubmitDisputeAsync(matchId, userId, disputeType, description.Trim(), evidenceUrl);
            TempData["SuccessMessage"] = "Khiếu nại đã được gửi. Người chơi trong trận sẽ được mời xác minh, admin xem xét trong 24–48 giờ.";
            return RedirectToPage(new { id = matchId });
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
            if (match.IsSplitFee) return "Chia đều cuối buổi";

            var customPrice = match.CustomPriceVnd ?? ExtractCustomPrice(match.Description);
            if (customPrice.HasValue) return $"{customPrice.Value:N0} xu";
            if (match.Booking?.FinalAmount > 0) return $"{match.Booking.FinalAmount:N0} xu";
            if (match.Court?.PricingRules != null && match.Court.PricingRules.Any())
                return $"Từ {match.Court.PricingRules.Min(p => p.UnitPrice):N0} xu/giờ";
            return "Chưa xác định";
        }

        private static decimal? ExtractCustomPrice(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            var lines = description.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var line = lines.FirstOrDefault(l => l.StartsWith("Match price xu:", StringComparison.OrdinalIgnoreCase));
            if (line == null) return null;
            var raw = line.Replace("Match price xu:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
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
            public string? CourtNumber { get; set; }
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
            public bool IsSplitFee { get; set; }
            public bool IsRecurring { get; set; }
            public string? RecurringDays { get; set; }

            // Host lock & cancel (B1)
            public bool IsLockedByHost { get; set; }

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
            public string? PhoneNumber { get; set; }
            public string? ZaloContact { get; set; }
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
