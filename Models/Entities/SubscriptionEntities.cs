namespace SportHub.Models.Entities
{
    public class SubscriptionPlan
    {
        public int PlanID { get; set; }
        public string PlanKey { get; set; } = "";          // "Free","Starter","Pro","Club"
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal PriceMonthly { get; set; }
        public decimal PriceQuarterly { get; set; }        // 0 = not available
        public decimal PriceAnnual { get; set; }           // 0 = not available
        public int MonthlyJoinLimit { get; set; }          // -1 = unlimited
        public int MonthlyCreateLimit { get; set; }        // -1 = unlimited
        public bool CanSeePhoneNumber { get; set; }
        public bool CanFilterByDistance { get; set; }
        public bool HasAiSuggestions { get; set; }
        public bool HasDetailedStats { get; set; }
        public bool HasPriorityListing { get; set; }
        public int PriorityScore { get; set; }             // 0=Free,1=Starter,2=Pro,3=Club
        public bool HasVerifiedBadge { get; set; }
        public bool HasPlayerFeeExempt { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class UserSubscription
    {
        public int UserSubscriptionID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        public string PlanKey { get; set; } = "Free";
        public string BillingCycle { get; set; } = "Monthly"; // Monthly,Quarterly,Annual,Trial
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Status { get; set; } = "Active";        // Active,Expired,Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SubscriptionOrder
    {
        public int SubscriptionOrderID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        public string PlanKey { get; set; } = "";
        public string BillingCycle { get; set; } = "Monthly";
        public decimal Amount { get; set; }
        public string TransactionRef { get; set; } = "";      // SUB{PLAN}-{userId}-{ts}
        public string Status { get; set; } = "Pending";       // Pending,Confirmed,Expired
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }

    public class UserMatchCredit
    {
        public int UserMatchCreditID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        public int RemainingCredits { get; set; }
        public string TransactionRef { get; set; } = "";      // SUBCREDIT-{userId}-{ts}
        public decimal AmountPaid { get; set; }
        public string Status { get; set; } = "Pending";       // Pending,Confirmed
        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
    }

    // Tracks monthly create/join usage per user for Free/Starter gating
    public class SubscriptionUsage
    {
        public int SubscriptionUsageID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        public string YearMonth { get; set; } = "";           // "2026-06"
        public int JoinCount { get; set; } = 0;
        public int CreateCount { get; set; } = 0;
    }
}
