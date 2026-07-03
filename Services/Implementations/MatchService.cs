using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using ExpiredPendingJoin = SportHub.Services.Interfaces.ExpiredPendingJoin;

namespace SportHub.Services.Interfaces
{
    public interface IMatchService
    {

        Task<List<Match>> GetRecommendedMatchesAsync(int limit = 5);
        Task<List<Match>> GetRecommendedMatchesForUserAsync(int userId, int limit = 5);
        Task<Match?> GetMatchDetailsAsync(int matchId);
        Task<JoinMatchResult> JoinMatchAsync(int matchId, int userId);
        Task<bool> SkipMatchAsync(int matchId, int userId);
        Task<int> CreateMatchAsync(Match match, int createdByUserId, bool hostJoins = false);
        Task<bool> UpdateMatchAsync(int matchId, int userId, Match updatedMatch);
        Task<bool> DeleteMatchAsync(int matchId, int userId);
        Task<bool> ApproveParticipantAsync(int matchId, int participantId, int hostUserId);
        Task<bool> RejectParticipantAsync(int matchId, int participantId, int hostUserId);
        Task<bool> LeaveMatchAsync(int matchId, int userId);
        Task<IReadOnlyList<ExpiredPendingJoin>> ExpireStalePendingJoinsAsync(TimeSpan maxPendingAge, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> LockMatchByHostAsync(int matchId, int hostUserId);
        Task<(bool Success, string Message)> CancelMatchByHostAsync(int matchId, int hostUserId, string reason);
    }

    public record ExpiredPendingJoin(int UserId, int MatchId, string MatchTitle);

    public enum JoinMatchReason
    {
        Success,
        NotFound,
        Closed,
        Locked,
        AlreadyRequested,
        MatchFull,
        ProfileIncomplete,
        SkillTooLow
    }

    public record JoinMatchResult(bool Success, JoinMatchReason Reason)
    {
        public static readonly JoinMatchResult Ok = new(true, JoinMatchReason.Success);
        public static JoinMatchResult Fail(JoinMatchReason reason) => new(false, reason);
    }

    public static class JoinMatchReasonExtensions
    {
        public static string ToUserMessage(this JoinMatchReason reason) => reason switch
        {
            JoinMatchReason.ProfileIncomplete => "Bạn chưa cập nhật môn thể thao & trình độ. Hãy hoàn thiện hồ sơ để tham gia trận.",
            JoinMatchReason.SkillTooLow => "Trình độ của bạn chưa phù hợp với yêu cầu của trận này.",
            JoinMatchReason.MatchFull => "Trận đã đủ người.",
            JoinMatchReason.Locked => "Trận đã bị host khóa đăng ký.",
            JoinMatchReason.Closed => "Trận đã đóng đăng ký.",
            JoinMatchReason.AlreadyRequested => "Bạn đã gửi yêu cầu tham gia trận này rồi.",
            JoinMatchReason.NotFound => "Trận đấu không tồn tại.",
            _ => "Không thể tham gia trận đấu."
        };
    }
}

namespace SportHub.Services.Implementations
{
    public class MatchService : Interfaces.IMatchService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWalletService _walletService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public MatchService(ApplicationDbContext context, IWalletService walletService,
            INotificationService notificationService, IEmailService emailService, IConfiguration config)
        {
            _context = context;
            _walletService = walletService;
            _notificationService = notificationService;
            _emailService = emailService;
            _config = config;
        }

        private enum SkillCheck { Pass, ProfileIncomplete, TooLow }

        // Thang trình độ riêng từng môn — đồng bộ với skillsBySport ở Create.cshtml và skillGuideData ở Matchmaking/Index.
        // Không gộp 1 map chung vì các mức trùng tên khác hạng giữa các môn (VD "Trung Bình" cầu lông = 5, bóng đá = 2).
        private static readonly Dictionary<string, Dictionary<string, int>> SkillRanksBySport =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Cầu lông"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Newbie"] = 1, ["Yếu"] = 2, ["Yếu+"] = 3, ["TBY/TB-"] = 4, ["Trung Bình"] = 5, ["TB+/Khá"] = 6
                },
                ["Pickleball"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["2.0"] = 1, ["2.5"] = 2, ["3.0"] = 3, ["3.5"] = 4, ["4.0"] = 5, ["4.5+"] = 6
                },
                ["Tennis"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["1.0-2.0"] = 1, ["2.5-3.0"] = 2, ["3.5"] = 3, ["4.0"] = 4, ["4.5"] = 5, ["5.0+"] = 6
                },
                ["Bóng đá"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Phong trào"] = 1, ["Trung bình"] = 2, ["Khá"] = 3, ["Tốt"] = 4, ["Chuyên nghiệp"] = 5
                },
                ["Bóng bàn"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Mới"] = 1, ["Mới bắt đầu"] = 1, ["Cơ bản"] = 2, ["Trung bình"] = 3, ["Khá"] = 4, ["Nâng cao"] = 5
                }
            };

        private static readonly Dictionary<string, int> GenericSkillRanks = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Beginner"] = 1, ["Newbie"] = 1, ["Mới"] = 1, ["Mới bắt đầu"] = 1,
            ["Intermediate"] = 2, ["Trung bình"] = 2,
            ["Khá"] = 3,
            ["Advanced"] = 4, ["Nâng cao"] = 4,
            ["Expert"] = 5, ["Professional"] = 5, ["Chuyên nghiệp"] = 5
        };

        private static int? GetSkillRank(string sportName, string level)
        {
            level = level.Trim();
            if (SkillRanksBySport.TryGetValue(sportName, out var map) && map.TryGetValue(level, out var rank))
                return rank;
            return GenericSkillRanks.TryGetValue(level, out var generic) ? generic : null;
        }

        // Trả về hạng tối thiểu trận yêu cầu; null = không có yêu cầu xác định (không chặn).
        // Hỗ trợ composite cầu lông "Nam:Yếu,Trung Bình|Nữ:Yếu" — so theo giới tính user.
        private static int? GetRequiredRank(string sportName, string required, string? gender)
        {
            required = required.Trim();
            if (!required.Contains(':'))
                return required.Equals("Any", StringComparison.OrdinalIgnoreCase) ? null : GetSkillRank(sportName, required);

            var wantedSide = gender == "M" ? "Nam" : gender == "F" ? "Nữ" : null;
            int? best = null;
            foreach (var part in required.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = part.IndexOf(':');
                if (idx <= 0) continue;
                var side = part[..idx].Trim();
                if (wantedSide != null && !side.Equals(wantedSide, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var levelRaw in part[(idx + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var level = levelRaw.Trim();
                    if (level.Equals("Any", StringComparison.OrdinalIgnoreCase)) return null;
                    var rank = GetSkillRank(sportName, level);
                    if (rank.HasValue && (best == null || rank < best)) best = rank;
                }
            }
            return best;
        }

        private async Task<SkillCheck> CheckSkillAsync(Match match, User user)
        {
            var required = match.SkillRequired;
            if (string.IsNullOrWhiteSpace(required) || required.Trim().Equals("Any", StringComparison.OrdinalIgnoreCase))
                return SkillCheck.Pass;

            var sportName = match.Sport?.SportName
                ?? await _context.Sports.Where(s => s.SportID == match.SportID).Select(s => s.SportName).FirstOrDefaultAsync()
                ?? string.Empty;

            // Ưu tiên trình độ đúng môn từ UserSportProfiles, fallback trường legacy SkillLevel
            var userSkill = await _context.UserSportProfiles
                .Where(p => p.UserID == user.UserID && p.SportID == match.SportID)
                .Select(p => p.SkillLevel)
                .FirstOrDefaultAsync() ?? user.SkillLevel;

            if (string.IsNullOrWhiteSpace(userSkill))
                return SkillCheck.ProfileIncomplete;

            var requiredRank = GetRequiredRank(sportName, required, user.Gender);
            if (requiredRank == null) return SkillCheck.Pass;

            var userRank = GetSkillRank(sportName, userSkill);
            if (userRank == null) return SkillCheck.ProfileIncomplete; // trình độ đã set thuộc thang môn khác

            // Buffer -1 giữ nguyên từ logic cũ: cho phép thấp hơn yêu cầu 1 bậc
            return userRank.Value >= requiredRank.Value - 1 ? SkillCheck.Pass : SkillCheck.TooLow;
        }

        public async Task<List<Match>> GetRecommendedMatchesAsync(int limit = 5)
        {
            return await _context.Matches
                .Include(m => m.CreatedByUser)
                .Include(m => m.Court).ThenInclude(c => c!.Venue)
                .Include(m => m.Court).ThenInclude(c => c!.Images)
                .Include(m => m.Court).ThenInclude(c => c!.PricingRules)
                .Include(m => m.Booking)
                .Include(m => m.Sport)
                .Include(m => m.Participants).ThenInclude(p => p.User)
                .Where(m => m.Status == "Open" && m.MatchDate >= DateTime.Today)
                .OrderBy(m => m.MatchDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Match>> GetRecommendedMatchesForUserAsync(int userId, int limit = 5)
        {
            var skippedMatchIds = userId > 0
                ? await _context.MatchInteractions
                    .Where(i => i.UserID == userId && i.Action == "Skip")
                    .Select(i => i.MatchID)
                    .ToListAsync()
                : new List<int>();

            return await _context.Matches
                .Include(m => m.CreatedByUser)
                .Include(m => m.Court).ThenInclude(c => c!.Venue)
                .Include(m => m.Court).ThenInclude(c => c!.Images)
                .Include(m => m.Court).ThenInclude(c => c!.PricingRules)
                .Include(m => m.Booking)
                .Include(m => m.Sport)
                .Include(m => m.Participants).ThenInclude(p => p.User)
                .Where(m => m.Status == "Open"
                    && m.MatchDate >= DateTime.Today
                    && !skippedMatchIds.Contains(m.MatchID))
                .OrderBy(m => m.MatchDate)
                .ThenBy(m => m.StartTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<Match?> GetMatchDetailsAsync(int matchId)
        {
            return await _context.Matches
                .Include(m => m.CreatedByUser)
                .Include(m => m.Court).ThenInclude(c => c!.Venue)
                .Include(m => m.Court).ThenInclude(c => c!.PricingRules)
                .Include(m => m.Booking)
                .Include(m => m.Sport)
                .Include(m => m.Participants).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);
        }

        public async Task<JoinMatchResult> JoinMatchAsync(int matchId, int userId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .Include(m => m.Sport)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null) return JoinMatchResult.Fail(JoinMatchReason.NotFound);
            if (match.IsLockedByHost) return JoinMatchResult.Fail(JoinMatchReason.Locked);
            if (match.Status != "Open") return JoinMatchResult.Fail(JoinMatchReason.Closed);

            // Đã có yêu cầu đang chờ hoặc đã được duyệt/chấp nhận
            if (match.Participants.Any(p => p.UserID == userId &&
                (p.JoinStatus == "Pending" || p.JoinStatus == "Approved" || p.JoinStatus == "Accepted")))
                return JoinMatchResult.Fail(JoinMatchReason.AlreadyRequested);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return JoinMatchResult.Fail(JoinMatchReason.NotFound);

            var skillCheck = await CheckSkillAsync(match, user);
            if (skillCheck == SkillCheck.ProfileIncomplete) return JoinMatchResult.Fail(JoinMatchReason.ProfileIncomplete);
            if (skillCheck == SkillCheck.TooLow) return JoinMatchResult.Fail(JoinMatchReason.SkillTooLow);

            // Đếm Accepted + Approved để kiểm tra chỗ còn trống
            var acceptedCount = match.Participants.Count(p => p.JoinStatus == "Accepted" || p.JoinStatus == "Approved");
            if (acceptedCount >= match.MaxParticipants) return JoinMatchResult.Fail(JoinMatchReason.MatchFull);

            // Ghép vãng lai: luôn chờ host duyệt (không vào thẳng)
            match.Participants.Add(new MatchParticipant
            {
                MatchID = matchId,
                UserID = userId,
                JoinStatus = "Pending",
                JoinedAt = DateTime.UtcNow
            });

            match.RequiresApproval = true;
            _context.MatchInteractions.Add(new MatchInteraction
            {
                MatchID = matchId,
                UserID = userId,
                Action = "Request",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return JoinMatchResult.Ok;
        }

        public async Task<bool> SkipMatchAsync(int matchId, int userId)
        {
            if (userId <= 0) return false;

            var exists = await _context.Matches.AnyAsync(m => m.MatchID == matchId);
            if (!exists) return false;

            var alreadySkipped = await _context.MatchInteractions
                .AnyAsync(i => i.MatchID == matchId && i.UserID == userId && i.Action == "Skip");
            if (alreadySkipped) return true;

            _context.MatchInteractions.Add(new MatchInteraction
            {
                MatchID = matchId,
                UserID = userId,
                Action = "Skip",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CreateMatchAsync(Match match, int createdByUserId, bool hostJoins = false)
        {
            match.CreatedByUserID = createdByUserId;
            match.Status = "PendingDeposit"; // Becomes Open after host deposit confirmed
            match.RequiresApproval = true;
            match.CreatedAt = DateTime.UtcNow;

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            if (hostJoins)
            {
                _context.MatchParticipants.Add(new MatchParticipant
                {
                    MatchID = match.MatchID,
                    UserID = createdByUserId,
                    JoinStatus = "Accepted",
                    JoinedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return match.MatchID;
        }

        public async Task<bool> UpdateMatchAsync(int matchId, int userId, Match updatedMatch)
        {
            var match = await _context.Matches
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null || match.CreatedByUserID != userId)
                return false;

            match.CourtID = updatedMatch.CourtID;
            match.SportID = updatedMatch.SportID;
            match.MatchDate = updatedMatch.MatchDate;
            match.StartTime = updatedMatch.StartTime;
            match.EndTime = updatedMatch.EndTime;
            match.MatchType = updatedMatch.MatchType;
            match.SkillRequired = updatedMatch.SkillRequired;
            match.MaxParticipants = updatedMatch.MaxParticipants;
            match.Title = updatedMatch.Title;
            match.Description = updatedMatch.Description;
            match.CustomCourtName = updatedMatch.CustomCourtName;
            match.CustomCourtAddress = updatedMatch.CustomCourtAddress;
            match.CustomPriceVnd = updatedMatch.CustomPriceVnd;
            match.CustomLatitude = updatedMatch.CustomLatitude;
            match.CustomLongitude = updatedMatch.CustomLongitude;
            match.IsSplitFee = updatedMatch.IsSplitFee;
            match.RequiresApproval = true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMatchAsync(int matchId, int userId)
        {
            var match = await _context.Matches
                .FirstOrDefaultAsync(m => m.MatchID == matchId && m.CreatedByUserID == userId);

            if (match == null) return false;

            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveParticipantAsync(int matchId, int participantId, int hostUserId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.MatchID == matchId && m.CreatedByUserID == hostUserId);

            if (match == null) return false;

            var participant = match.Participants.FirstOrDefault(p => p.ParticipantID == participantId && p.JoinStatus == "Pending");
            if (participant == null) return false;

            // Chặn duyệt khi trình độ thấp hơn yêu cầu; hồ sơ thiếu thì host tự quyết (đã thấy profile)
            if (await CheckSkillAsync(match, participant.User) == SkillCheck.TooLow)
                return false;

            var filledCount = match.Participants.Count(p => p.JoinStatus == "Accepted" || p.JoinStatus == "Approved");
            if (filledCount >= match.MaxParticipants) return false; // Hết chỗ

            // Approved = chờ player thanh toán 5K trong 1 giờ
            var deadline = DateTime.UtcNow.AddHours(1);
            participant.JoinStatus = "Approved";
            participant.PlayerFeeStatus = "AwaitingPayment";
            participant.PlayerFeeDeadline = deadline;

            // Tạo bản ghi thanh toán PlayerFee
            _context.MatchPayments.Add(new MatchPayment
            {
                MatchID = matchId,
                PayerUserID = participant.UserID,
                PaymentType = "PlayerFee",
                Amount = 5_000m,
                Status = "Pending",
                ExpiresAt = deadline,
                TransactionRef = $"FEE-{matchId}-{participant.UserID}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CreatedAt = DateTime.UtcNow
            });

            // Tự động Full nếu (Accepted + Approved) đủ
            if (filledCount + 1 >= match.MaxParticipants)
                match.Status = "Full";

            await _context.SaveChangesAsync();

            if (participant.User?.NotifyByEmail == true && !string.IsNullOrWhiteSpace(participant.User.Email))
            {
                var baseUrl = (_config["App:BaseUrl"] ?? "https://sporthub-dn.id.vn").TrimEnd('/');
                await _emailService.SendMatchApprovedAsync(participant.User.Email, participant.User.FullName,
                    match.Title ?? "Trận đấu", match.MatchDate.ToString("dd/MM/yyyy"),
                    $"{baseUrl}/Matchmaking/Details/{matchId}");
            }

            return true;
        }

        public async Task<bool> RejectParticipantAsync(int matchId, int participantId, int hostUserId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId && m.CreatedByUserID == hostUserId);

            if (match == null) return false;

            var participant = match.Participants.FirstOrDefault(p => p.ParticipantID == participantId && p.JoinStatus == "Pending");
            if (participant == null) return false;

            participant.JoinStatus = "Declined";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LeaveMatchAsync(int matchId, int userId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null) return false;
            if (match.CreatedByUserID == userId) return false; // Host không thể leave

            var participant = match.Participants.FirstOrDefault(p => p.UserID == userId);
            if (participant == null) return false;

            participant.JoinStatus = "Cancelled";

            // Nếu trận đang Full → mở lại
            if (match.Status == "Full") match.Status = "Open";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<ExpiredPendingJoin>> ExpireStalePendingJoinsAsync(
            TimeSpan maxPendingAge,
            CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow - maxPendingAge;

            var stale = await _context.MatchParticipants
                .Include(p => p.Match)
                .Where(p => p.JoinStatus == "Pending" && p.JoinedAt < cutoff)
                .ToListAsync(cancellationToken);

            if (stale.Count == 0)
                return Array.Empty<ExpiredPendingJoin>();

            var result = new List<ExpiredPendingJoin>();
            foreach (var participant in stale)
            {
                participant.JoinStatus = "Cancelled";
                var title = string.IsNullOrWhiteSpace(participant.Match.Title)
                    ? participant.Match.MatchType
                    : participant.Match.Title;
                result.Add(new ExpiredPendingJoin(participant.UserID, participant.MatchID, title ?? "Trận đấu"));
            }

            await _context.SaveChangesAsync(cancellationToken);
            return result;
        }

        public async Task<(bool Success, string Message)> LockMatchByHostAsync(int matchId, int hostUserId)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return (false, "Không tìm thấy trận.");
            if (match.CreatedByUserID != hostUserId) return (false, "Bạn không có quyền.");
            if (match.Status == "Cancelled") return (false, "Trận đã bị hủy.");

            match.IsLockedByHost = !match.IsLockedByHost;
            await _context.SaveChangesAsync();
            return (true, match.IsLockedByHost ? "Đã khóa trận — không nhận thêm người tham gia." : "Đã mở khóa trận.");
        }

        public async Task<(bool Success, string Message)> CancelMatchByHostAsync(int matchId, int hostUserId, string reason)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);
            if (match == null) return (false, "Không tìm thấy trận.");
            if (match.CreatedByUserID != hostUserId) return (false, "Bạn không có quyền hủy trận này.");
            if (match.Status is "Cancelled" or "Completed" or "InProgress")
                return (false, "Không thể hủy trận ở trạng thái hiện tại.");

            // Refund confirmed player fees
            var playerFeePayments = await _context.MatchPayments
                .Where(p => p.MatchID == matchId && p.PaymentType == "PlayerFee" && p.Status == "Confirmed")
                .ToListAsync();

            foreach (var payment in playerFeePayments)
            {
                await _walletService.CreditAsync(payment.PayerUserID, payment.Amount,
                    $"Hoàn phí trận #{matchId} — {match.Title ?? "Trận đấu"}", matchId: matchId, type: "Refund");
                payment.Status = "Refunded";

                await _notificationService.CreateAsync(
                    payment.PayerUserID, "System",
                    "Trận đấu đã bị hủy bởi chủ trận",
                    $"Hoàn {payment.Amount:N0}đ — {match.Title ?? "Trận đấu"}. Lý do: {reason}",
                    $"/Matchmaking/Details/{matchId}");

                var payer = await _context.Users.FindAsync(payment.PayerUserID);
                if (payer?.NotifyByEmail == true && !string.IsNullOrWhiteSpace(payer.Email))
                    await _emailService.SendMatchCancelledAsync(payer.Email, payer.FullName,
                        match.Title ?? "Trận đấu", reason, payment.Amount);
            }

            match.Status = "Cancelled";
            match.CancelReason = reason.Trim();
            match.CancelledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, $"Đã hủy trận và hoàn tiền cho {playerFeePayments.Count} người tham gia.");
        }
    }
}
