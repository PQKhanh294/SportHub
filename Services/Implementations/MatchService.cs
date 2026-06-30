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
        Task<bool> JoinMatchAsync(int matchId, int userId);
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

        /// <summary>
        /// Validates if user skill level meets the required skill level for a match
        /// Skill hierarchy: Beginner (1) < Intermediate (2) < Advanced (3) < Expert (4)
        /// </summary>
        private bool ValidateUserSkillLevel(string? userSkill, string? requiredSkill)
        {
            // If no skill required or "Any", any user can join
            if (string.IsNullOrWhiteSpace(requiredSkill) || string.Equals(requiredSkill, "Any", StringComparison.OrdinalIgnoreCase))
                return true;

            // If user has no skill defined, cannot join matches with skill requirements
            if (string.IsNullOrWhiteSpace(userSkill))
                return false;

            // Normalize skill levels for comparison
            var skillMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                // English levels
                { "Beginner", 1 },
                { "Intermediate", 2 },
                { "Advanced", 3 },
                { "Expert", 4 },
                { "Professional", 5 },
                
                // Vietnamese / Badminton specific levels
                { "Newbie", 1 },
                { "Yếu", 2 },
                { "Yếu+", 3 },
                { "TBY/TB-", 4 },
                { "Trung Bình", 5 },
                { "TB+/Khá", 6 }
            };

            // If required skill unknown, default to true or handle gracefully
            if (!skillMap.TryGetValue(requiredSkill, out var requiredSkillValue))
                return true;

            // If user skill unknown, they can't join specific requirements
            if (!skillMap.TryGetValue(userSkill, out var userSkillValue))
                return false;

            // User skill must be >= required skill (or just allow if it matches the spirit of the game)
            // For now, let's keep the >= logic
            return userSkillValue >= (requiredSkillValue - 1); // Give some buffer or strictness as needed
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

        public async Task<bool> JoinMatchAsync(int matchId, int userId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null || match.Status != "Open" || match.IsLockedByHost) return false;

            // Đã có yêu cầu đang chờ hoặc đã được duyệt/chấp nhận
            if (match.Participants.Any(p => p.UserID == userId &&
                (p.JoinStatus == "Pending" || p.JoinStatus == "Approved" || p.JoinStatus == "Accepted")))
                return false;

            // ✅ NEW: Validate user skill level
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return false;

            if (!ValidateUserSkillLevel(user.SkillLevel, match.SkillRequired))
                return false;

            // Đếm Accepted + Approved để kiểm tra chỗ còn trống
            var acceptedCount = match.Participants.Count(p => p.JoinStatus == "Accepted" || p.JoinStatus == "Approved");
            if (acceptedCount >= match.MaxParticipants) return false;

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
            return true;
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

            // ✅ NEW: Validate participant skill level before approving
            if (!ValidateUserSkillLevel(participant.User.SkillLevel, match.SkillRequired))
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
