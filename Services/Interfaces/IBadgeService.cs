using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IBadgeService
    {
        Task<List<BadgeProgressItem>> GetBadgeProgressAsync(int userId);
        Task SyncEarnedBadgesAsync(int userId);
    }

    public class BadgeProgressItem
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "workspace_premium";
        public string Level { get; set; } = "None";
        public int ProgressValue { get; set; }
        public int TargetValue { get; set; }
        public int ProgressPercent => TargetValue <= 0 ? 0 : Math.Clamp((int)Math.Round(ProgressValue * 100m / TargetValue), 0, 100);
        public bool IsEarned => Level != "None";
        public UserBadge? EarnedBadge { get; set; }
    }
}
