using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task<bool> UpdateUserProfileAsync(int userId, string fullName, string? phoneNumber, string? avatarUrl, string? skillLevel, string? favoriteSport = null);
        Task<bool> ValidateCredentialsAsync(string email, string password);
        Task<List<User>> GetSuggestedPlayersAsync(int currentUserId, int limit = 4);
        Task<List<User>> SearchUsersAsync(int currentUserId, string? keyword, string? skillLevel, string? sport, string? sortBy);
        Task<int> GetTotalMatchesPlayedAsync(int userId);
        Task<int> GetTotalWinsAsync(int userId);
        Task<int> GetTotalBookingsAsync(int userId);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> SetUserActiveAsync(int userId, bool isActive);
        Task IncrementLoginCountAsync(int userId);
        Task<bool> IsProfileCompleteAsync(int userId);
        Task<User> GetOrCreateGoogleUserAsync(string googleId, string email, string fullName, string? avatarUrl);
        Task<bool> IsAdminAsync(int userId);
    }
}
