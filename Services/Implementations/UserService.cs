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
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, string fullName, string? phoneNumber, string? avatarUrl, string? skillLevel, string? favoriteSport = null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return false;

            user.FullName = fullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            user.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
            user.SkillLevel = string.IsNullOrWhiteSpace(skillLevel) ? user.SkillLevel : skillLevel;
            user.FavoriteSport = string.IsNullOrWhiteSpace(favoriteSport) ? user.FavoriteSport : favoriteSport;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null || !user.IsActive) return false;

            var inputHash = PasswordHasher.Hash(password);
            return user.PasswordHash == inputHash;
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

        public async Task<List<User>> SearchUsersAsync(int currentUserId, string? keyword, string? skillLevel, string? sport, string? sortBy)
        {
            var query = _context.Users.Where(u => u.UserID != currentUserId && u.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(u => u.FullName.Contains(keyword) || (u.Email != null && u.Email.Contains(keyword)));
            }
            if (!string.IsNullOrWhiteSpace(skillLevel))
            {
                query = query.Where(u => u.SkillLevel == skillLevel);
            }
            if (!string.IsNullOrWhiteSpace(sport))
            {
                query = query.Where(u => u.FavoriteSport != null && u.FavoriteSport.Contains(sport));
            }

            var users = await query.ToListAsync();

            if (sortBy == "distance")
            {
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser != null && currentUser.DefaultLatitude.HasValue && currentUser.DefaultLongitude.HasValue)
                {
                    double currentLat = (double)currentUser.DefaultLatitude.Value;
                    double currentLon = (double)currentUser.DefaultLongitude.Value;

                    users = users.OrderBy(u =>
                    {
                        if (!u.DefaultLatitude.HasValue || !u.DefaultLongitude.HasValue)
                            return double.MaxValue;

                        double uLat = (double)u.DefaultLatitude.Value;
                        double uLon = (double)u.DefaultLongitude.Value;

                        return CalculateHaversineDistance(currentLat, currentLon, uLat, uLon);
                    }).ToList();
                }
            }
            else // Default to newest
            {
                users = users.OrderByDescending(u => u.CreatedAt).ToList();
            }

            return users;
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371d; // Earth radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            return R * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
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

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> SetUserActiveAsync(int userId, bool isActive)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return false;
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
