using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;
using SportHub.Services.Security;

namespace SportHub.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserID == userId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, string fullName, string? phoneNumber, string? avatarUrl, string? skillLevel)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return false;

            user.FullName = fullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            user.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
            user.SkillLevel = string.IsNullOrWhiteSpace(skillLevel) ? user.SkillLevel : skillLevel;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null || !user.IsActive) return false;

            var inputHash = PasswordHasher.Hash(password);
            return user.PasswordHash == inputHash || user.PasswordHash == password;
        }

        public async Task<List<User>> GetSuggestedPlayersAsync(int currentUserId, int limit = 4)
        {
            // Dummy logic: lấy user có rating cao
            return await _context.Users
                .Where(u => u.UserID != currentUserId && u.IsActive)
                .OrderByDescending(u => u.SkillLevel)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetTotalMatchesPlayedAsync(int userId)
        {
            return await _context.MatchParticipants
                .CountAsync(mp => mp.UserID == userId && mp.JoinStatus == "Accepted");
        }

        public async Task<int> GetTotalWinsAsync(int userId)
        {
            return await _context.MatchParticipants
                .CountAsync(mp => mp.UserID == userId && mp.JoinStatus == "Accepted" && mp.Match.Status == "Completed");
        }

        public async Task<int> GetTotalBookingsAsync(int userId)
        {
            return await _context.Bookings
                .CountAsync(b => b.UserID == userId);
        }
    }
}
