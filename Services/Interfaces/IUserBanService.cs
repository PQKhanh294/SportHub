using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IUserBanService
    {
        Task<UserBan> BanUserAsync(int userId, int adminId, string banType, string reason, int? reportId);
        Task UnbanUserAsync(int banId, int adminId);
        Task<bool> IsUserBannedAsync(int userId);
        Task<UserBan?> GetActiveBanAsync(int userId);
        Task<List<UserBan>> GetBanHistoryAsync(int userId);
    }
}
