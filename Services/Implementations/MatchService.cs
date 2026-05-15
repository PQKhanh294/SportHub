using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IMatchService
    {
        Task<List<Match>> GetRecommendedMatchesAsync(int limit = 5);
        Task<Match?> GetMatchDetailsAsync(int matchId);
        Task<bool> JoinMatchAsync(int matchId, int userId);
        Task<int> CreateMatchAsync(Match match, int createdByUserId);
        Task<bool> UpdateMatchAsync(int matchId, int userId, Match updatedMatch);
        Task<bool> ApproveParticipantAsync(int matchId, int participantId, int hostUserId);
        Task<bool> RejectParticipantAsync(int matchId, int participantId, int hostUserId);
        Task<bool> LeaveMatchAsync(int matchId, int userId);
    }
}

namespace SportHub.Services.Implementations
{
    public class MatchService : Interfaces.IMatchService
    {
        private readonly ApplicationDbContext _context;

        public MatchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Match>> GetRecommendedMatchesAsync(int limit = 5)
        {
            return await _context.Matches
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

            if (match == null || match.Status != "Open") return false;

            // Đã tham gia (bất kỳ status nào)
            if (match.Participants.Any(p => p.UserID == userId)) return false;

            // Đếm chỉ Accepted để kiểm tra chỗ còn trống
            var acceptedCount = match.Participants.Count(p => p.JoinStatus == "Accepted");
            if (acceptedCount >= match.MaxParticipants) return false;

            // Nếu RequiresApproval → Pending, ngược lại → Accepted
            var joinStatus = match.RequiresApproval ? "Pending" : "Accepted";

            match.Participants.Add(new MatchParticipant
            {
                MatchID = matchId,
                UserID = userId,
                JoinStatus = joinStatus,
                JoinedAt = DateTime.UtcNow
            });

            // Tự động đổi Full nếu đủ người (chỉ tính Accepted)
            if (!match.RequiresApproval && acceptedCount + 1 >= match.MaxParticipants)
            {
                match.Status = "Full";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CreateMatchAsync(Match match, int createdByUserId)
        {
            match.CreatedByUserID = createdByUserId;
            match.Status = "Open";
            match.CreatedAt = DateTime.UtcNow;

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            // Host tự động là Accepted
            _context.MatchParticipants.Add(new MatchParticipant
            {
                MatchID = match.MatchID,
                UserID = createdByUserId,
                JoinStatus = "Accepted",
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
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
            match.RequiresApproval = updatedMatch.RequiresApproval;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveParticipantAsync(int matchId, int participantId, int hostUserId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId && m.CreatedByUserID == hostUserId);

            if (match == null) return false;

            var participant = match.Participants.FirstOrDefault(p => p.ParticipantID == participantId && p.JoinStatus == "Pending");
            if (participant == null) return false;

            var acceptedCount = match.Participants.Count(p => p.JoinStatus == "Accepted");
            if (acceptedCount >= match.MaxParticipants) return false; // Hết chỗ

            participant.JoinStatus = "Accepted";

            // Tự động Full nếu đủ
            if (acceptedCount + 1 >= match.MaxParticipants)
                match.Status = "Full";

            await _context.SaveChangesAsync();
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
    }
}
