using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface ISubscriptionService
    {
        // Current plan info
        Task<SubscriptionPlan> GetCurrentPlanAsync(int userId);
        Task<UserSubscription?> GetActiveSubscriptionAsync(int userId);
        Task<List<SubscriptionPlan>> GetAllPlansAsync();

        // Activation
        Task<UserSubscription> ActivateSubscriptionAsync(int userId, string planKey, string billingCycle);
        Task<bool> StartTrialAsync(int userId);
        Task<bool> HasUsedTrialAsync(int userId);

        // Orders
        Task<SubscriptionOrder> CreateOrderAsync(int userId, string planKey, string billingCycle);
        Task<bool> ConfirmOrderAsync(string transactionRef, decimal actualAmount);
        Task<SubscriptionOrder?> GetPendingOrderAsync(int userId);

        // Match credits
        Task<UserMatchCredit> CreateCreditOrderAsync(int userId);
        Task<bool> ConfirmCreditOrderAsync(string transactionRef, decimal actualAmount);
        Task<int> GetRemainingCreditsAsync(int userId);
        Task<bool> UseMatchCreditAsync(int userId);

        // Feature gating
        Task<bool> CanCreateMatchAsync(int userId);
        Task<bool> CanJoinMatchAsync(int userId);
        Task<bool> CanSeePhoneNumberAsync(int userId);
        Task<bool> CanFilterByDistanceAsync(int userId);
        Task<bool> HasAiSuggestionsAsync(int userId);
        Task<bool> HasDetailedStatsAsync(int userId);
        Task<bool> HasVerifiedBadgeAsync(int userId);
        Task<bool> HasPlayerFeeExemptAsync(int userId);
        Task<int> GetPriorityScoreAsync(int userId);

        // Usage tracking
        Task RecordJoinAsync(int userId);
        Task RecordCreateAsync(int userId);
        Task<int> GetMonthlyJoinCountAsync(int userId);
        Task<int> GetMonthlyCreateCountAsync(int userId);

        // Admin
        Task<List<UserSubscription>> GetAllActiveSubscriptionsAsync();
        Task<int> GetActiveSubscriptionCountAsync();

        // Pricing
        decimal GetPrice(string planKey, string billingCycle);
    }
}
