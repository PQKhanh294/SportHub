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
                .Include(m => m.Participants).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);
        }

        public async Task<bool> JoinMatchAsync(int matchId, int userId)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);
                
            if (match == null || match.Status != "Open") return false;
            
            if (match.Participants.Any(p => p.UserID == userId)) return false; // Đã tham gia
            
            if (match.Participants.Count >= match.MaxParticipants) return false; // Hết chỗ

            match.Participants.Add(new MatchParticipant
            {
                MatchID = matchId,
                UserID = userId,
                JoinStatus = "Accepted",
                JoinedAt = DateTime.UtcNow
            });
            
            if (match.Participants.Count == match.MaxParticipants)
            {
                match.Status = "Full"; // Tự động đổi trạng thái nếu đầy
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
    }
}
