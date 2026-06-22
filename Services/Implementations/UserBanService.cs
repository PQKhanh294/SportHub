using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class UserBanService : IUserBanService
    {
        private readonly ApplicationDbContext _context;

        public UserBanService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserBan> BanUserAsync(int userId, int adminId, string banType, string reason, int? reportId)
        {
            // Deactivate any existing active bans
            var existing = await _context.UserBans
                .Where(b => b.UserID == userId && b.IsActive)
                .ToListAsync();
            foreach (var b in existing) b.IsActive = false;

            DateTime? endAt = banType switch
            {
                "1day"    => DateTime.UtcNow.AddDays(1),
                "7days"   => DateTime.UtcNow.AddDays(7),
                "30days"  => DateTime.UtcNow.AddDays(30),
                _         => null  // permanent
            };

            var ban = new UserBan
            {
                UserID = userId,
                BannedByAdminID = adminId,
                ReportID = reportId,
                BanType = banType,
                Reason = reason,
                StartAt = DateTime.UtcNow,
                EndAt = endAt,
                IsActive = true
            };
            _context.UserBans.Add(ban);

            // Update User flags
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsBanned = true;
                user.BanEndAt = endAt;
            }

            await _context.SaveChangesAsync();
            return ban;
        }

        public async Task UnbanUserAsync(int banId, int adminId)
        {
            var ban = await _context.UserBans.FindAsync(banId);
            if (ban == null) return;
            ban.IsActive = false;

            var user = await _context.Users.FindAsync(ban.UserID);
            if (user != null) { user.IsBanned = false; user.BanEndAt = null; }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsUserBannedAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsBanned) return false;
            if (user.BanEndAt.HasValue && user.BanEndAt.Value <= DateTime.UtcNow)
            {
                // Auto-lift expired ban
                user.IsBanned = false;
                user.BanEndAt = null;
                await _context.SaveChangesAsync();
                return false;
            }
            return true;
        }

        public async Task<UserBan?> GetActiveBanAsync(int userId)
        {
            return await _context.UserBans
                .Include(b => b.BannedByAdmin)
                .Where(b => b.UserID == userId && b.IsActive)
                .OrderByDescending(b => b.StartAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserBan>> GetBanHistoryAsync(int userId)
        {
            return await _context.UserBans
                .Include(b => b.BannedByAdmin)
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.StartAt)
                .ToListAsync();
        }
    }
}
