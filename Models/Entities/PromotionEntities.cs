namespace SportHub.Models.Entities
{
    public class PromotionCampaign
    {
        public int CampaignID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        // Manual | FirstLogin | Birthday | WomensDay | MensDay | PromoCode | PersonalVoucher
        public string TriggerType { get; set; } = "Manual";
        public decimal Amount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        // All | HostOnly | Gender:F | Gender:M | NewUser
        public string ApplicableScope { get; set; } = "All";
        public int? MaxRedemptions { get; set; }
        public int RedemptionCount { get; set; } = 0;
        public int CreatedByAdminID { get; set; }
        public User CreatedByAdmin { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PromoCode> PromoCodes { get; set; } = new List<PromoCode>();
        public ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
        public ICollection<PromotionRedemption> Redemptions { get; set; } = new List<PromotionRedemption>();
    }

    public class PromoCode
    {
        public int PromoCodeID { get; set; }
        public int CampaignID { get; set; }
        public PromotionCampaign Campaign { get; set; } = null!;
        public string Code { get; set; } = string.Empty;
        public int? MaxUses { get; set; }
        public int UseCount { get; set; } = 0;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserVoucher
    {
        public int VoucherID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        public int? CampaignID { get; set; }
        public PromotionCampaign? Campaign { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
        public int? IssuedByAdminID { get; set; }
        public User? IssuedByAdmin { get; set; }
    }

    public class PromotionRedemption
    {
        public int RedemptionID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        public int? CampaignID { get; set; }
        public PromotionCampaign? Campaign { get; set; }
        public int? PromoCodeID { get; set; }
        public PromoCode? PromoCode { get; set; }
        public int? VoucherID { get; set; }
        public UserVoucher? Voucher { get; set; }
        public decimal AmountCredited { get; set; }
        public int? WalletTransactionID { get; set; }
        public WalletTransaction? WalletTransaction { get; set; }
        public string Note { get; set; } = string.Empty;
        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
    }
}
