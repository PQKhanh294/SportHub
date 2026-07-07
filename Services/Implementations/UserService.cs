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
            var me = currentUserId > 0
                ? await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == currentUserId)
                : null;

            var candidates = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserID != currentUserId && u.IsActive)
                .Select(u => new
                {
                    u.UserID, u.FullName, u.AvatarUrl, u.SkillLevel, u.FavoriteSport,
                    u.DefaultAddress, u.PhoneNumber, u.DefaultLatitude, u.DefaultLongitude,
                    MatchCount = _context.MatchParticipants.Count(mp => mp.UserID == u.UserID)
                })
                .ToListAsync();

            return candidates
                .Select(u => new
                {
                    Data = u,
                    Score =
                        (me?.FavoriteSport != null && me.FavoriteSport == u.FavoriteSport ? 3 : 0) +
                        (me?.SkillLevel != null && me.SkillLevel == u.SkillLevel ? 2 : 0) +
                        (!string.IsNullOrEmpty(u.DefaultAddress) ? 1 : 0) +
                        (!string.IsNullOrEmpty(u.PhoneNumber) ? 1 : 0)
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Data.MatchCount)
                .Take(limit)
                .Select(x => new User
                {
                    UserID = x.Data.UserID,
                    FullName = x.Data.FullName,
                    AvatarUrl = x.Data.AvatarUrl,
                    SkillLevel = x.Data.SkillLevel,
                    FavoriteSport = x.Data.FavoriteSport,
                    DefaultAddress = x.Data.DefaultAddress,
                    PhoneNumber = x.Data.PhoneNumber,
                    DefaultLatitude = x.Data.DefaultLatitude,
                    DefaultLongitude = x.Data.DefaultLongitude,
                    IsActive = true
                })
                .ToList();
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

        public async Task<int> GetTotalHostedMatchesAsync(int userId)
        {
            return await _context.Matches
                .CountAsync(m => m.CreatedByUserID == userId);
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

        public async Task IncrementLoginCountAsync(int userId)
        {
            await _context.Users
                .Where(u => u.UserID == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.LoginCount, u => u.LoginCount + 1)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
        }

        public async Task<bool> IsProfileCompleteAsync(int userId)
        {
            var u = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserID == userId);
            if (u == null) return true;
            return !string.IsNullOrWhiteSpace(u.PhoneNumber)
                && !string.IsNullOrWhiteSpace(u.SkillLevel)
                && !string.IsNullOrWhiteSpace(u.FavoriteSport)
                && !string.IsNullOrWhiteSpace(u.DefaultAddress);
        }

        public async Task<User> GetOrCreateGoogleUserAsync(string googleId, string email, string fullName, string? avatarUrl)
        {
            // Tìm theo GoogleId trước
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (user != null) return user;

            // Liên kết tài khoản cũ cùng email
            user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user != null)
            {
                user.GoogleId = googleId;
                if (string.IsNullOrWhiteSpace(user.AvatarUrl) && avatarUrl != null)
                    user.AvatarUrl = avatarUrl;
                user.EmailConfirmed = true; // Google đã xác thực email này
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return user;
            }

            // Tạo tài khoản mới từ Google
            var playerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Player");
            var newUser = new User
            {
                GoogleId = googleId,
                Email = email,
                FullName = fullName,
                AvatarUrl = avatarUrl,
                PasswordHash = string.Empty,
                IsActive = true,
                IsVerified = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            if (playerRole != null)
            {
                _context.Set<UserRole>().Add(new UserRole { UserID = newUser.UserID, RoleID = playerRole.RoleID });
                await _context.SaveChangesAsync();
            }

            return await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.UserID == newUser.UserID);
        }

        public Task<bool> IsAdminAsync(int userId)
        {
            return _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserID == userId && ur.Role.RoleName == "Admin");
        }

        // Sinh mã 6 số, hết hạn 10 phút. Trả về null nếu vừa gửi chưa quá 60s (chặn spam gửi lại).
        public async Task<string?> GenerateEmailVerificationCodeAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return null;

            if (user.EmailVerificationSentAt.HasValue &&
                user.EmailVerificationSentAt.Value.AddSeconds(60) > DateTime.UtcNow)
                return null;

            var code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            user.EmailVerificationCode = code;
            user.EmailVerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);
            user.EmailVerificationSentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return code;
        }

        public async Task<bool> ConfirmEmailCodeAsync(string email, string code)
        {
            var normalizedEmail = email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null) return false;
            if (user.EmailVerificationCode != code) return false;
            if (!user.EmailVerificationCodeExpiresAt.HasValue || user.EmailVerificationCodeExpiresAt.Value < DateTime.UtcNow) return false;

            user.EmailConfirmed = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationCodeExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
