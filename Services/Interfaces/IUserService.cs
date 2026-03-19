using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task<bool> UpdateUserProfileAsync(int userId, string fullName, string? phoneNumber, string? avatarUrl, string? skillLevel);
        Task<bool> ValidateCredentialsAsync(string email, string password);
        Task<List<User>> GetSuggestedPlayersAsync(int currentUserId, int limit = 4);
        Task<int> GetTotalMatchesPlayedAsync(int userId);
        Task<int> GetTotalWinsAsync(int userId);
        Task<int> GetTotalBookingsAsync(int userId);
    }
}
