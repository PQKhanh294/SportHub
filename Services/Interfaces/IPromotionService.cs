using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public record PromoCodeInfoDto(
        bool Found,
        string CampaignName,
        decimal Amount,
        int? RemainingUses,
        DateTime? ExpiresAt,
        bool AlreadyUsed,
        bool Eligible,
        string? BlockReason
    );

    public record RedeemResult(bool Success, string Message, decimal Amount);

    public interface IPromotionService
    {
        // Admin: campaign management
        Task<PromotionCampaign> CreateCampaignAsync(string name, string? description, string triggerType,
            decimal amount, string applicableScope, DateTime? startDate, DateTime? endDate,
            int? maxRedemptions, int adminId);
        Task<List<PromotionCampaign>> GetAllCampaignsAsync();
        Task SetCampaignActiveAsync(int campaignId, bool active);
        Task<PromotionCampaign?> GetCampaignAsync(int campaignId);

        // Admin: promo codes
        Task<PromoCode> CreatePromoCodeAsync(int campaignId, string code, int? maxUses, DateTime? expiresAt);
        Task<List<PromoCode>> GeneratePromoCodesAsync(int campaignId, int count, string prefix, int? maxUses, DateTime? expiresAt);
        Task<List<PromoCode>> GetPromoCodesAsync(int campaignId);
        Task SetPromoCodeActiveAsync(int promoCodeId, bool active);

        // Admin: manual campaign distribution
        Task<(int Distributed, int Skipped)> DistributeManualCampaignAsync(int campaignId, int adminId);
        Task<int> GetEligibleUserCountAsync(int campaignId);

        // Admin: wallet credit + personal vouchers
        Task AdminCreditAsync(int adminId, int userId, decimal amount, string note);
        Task<UserVoucher> IssueVoucherAsync(int adminId, int userId, int? campaignId, decimal amount, DateTime? expiresAt, string? note);

        // Admin: redemption history
        Task<List<PromotionRedemption>> GetAllRedemptionsAsync(int page = 1, int pageSize = 30);
        Task<List<PromotionRedemption>> GetUserRedemptionsAsync(int userId);

        // User: promo codes
        Task<PromoCodeInfoDto> LookupCodeAsync(string code, int userId);
        Task<RedeemResult> RedeemPromoCodeAsync(int userId, string code);

        // User: personal vouchers
        Task<List<UserVoucher>> GetMyVouchersAsync(int userId);
        Task<RedeemResult> UseVoucherAsync(int userId, string voucherCode);

        // Auto-triggers
        Task TriggerFirstLoginAsync(int userId);
        Task TriggerBirthdayAsync(int userId);
        Task TriggerHolidayAsync(string triggerType);  // WomensDay | MensDay

        // User: saved promo codes
        Task<(bool Success, string Message)> SaveCodeAsync(int userId, string code);
        Task<List<SavedCodeWithDetailsDto>> GetSavedCodesAsync(int userId);
        Task RemoveSavedCodeAsync(int userId, string code);

        // Admin user list for wallet management
        Task<List<WalletUserDto>> GetUsersForWalletManagementAsync(string? search, int page, int pageSize);
        Task<int> GetUsersCountAsync(string? search);
    }

    public record WalletUserDto(int UserID, string FullName, string Email, string? AvatarUrl, decimal WalletBalance, DateTime CreatedAt);

    public record SavedCodeWithDetailsDto(
        string Code,
        decimal? Amount,
        string? CampaignName,
        DateTime? ExpiresAt,
        bool IsValid,
        string? StatusMessage,
        DateTime SavedAt
    );
}
