using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;

        private static readonly Dictionary<string, (decimal Monthly, decimal Quarterly, decimal Annual)> _prices = new()
        {
            { "Free",    (0, 0, 0) },
            { "Starter", (39_000m, 99_000m, 0m) },
            { "Pro",     (99_000m, 249_000m, 890_000m) },
            { "Club",    (199_000m, 499_000m, 0m) }
        };

        private static readonly int CreditBundleCount = 5;
        private static readonly decimal CreditBundlePrice = 20_000m;

        public SubscriptionService(ApplicationDbContext context) => _context = context;

        // ─── Plan Info ───────────────────────────────────────────────

        public async Task<SubscriptionPlan> GetCurrentPlanAsync(int userId)
        {
            var sub = await GetActiveSubscriptionAsync(userId);
            var planKey = sub?.PlanKey ?? "Free";
            return await _context.SubscriptionPlans.FirstAsync(p => p.PlanKey == planKey);
        }

        public async Task<UserSubscription?> GetActiveSubscriptionAsync(int userId)
        {
            return await _context.UserSubscriptions
                .Where(s => s.UserID == userId && s.Status == "Active" && s.EndAt > DateTime.UtcNow)
                .OrderByDescending(s => s.EndAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SubscriptionPlan>> GetAllPlansAsync()
            => await _context.SubscriptionPlans.OrderBy(p => p.SortOrder).ToListAsync();

        // ─── Activation ──────────────────────────────────────────────

        public async Task<UserSubscription> ActivateSubscriptionAsync(int userId, string planKey, string billingCycle)
        {
            // Expire any existing active sub
            var existing = await GetActiveSubscriptionAsync(userId);
            if (existing != null)
            {
                existing.Status = "Cancelled";
                existing.EndAt = DateTime.UtcNow;
            }

            var duration = billingCycle switch
            {
                "Quarterly" => TimeSpan.FromDays(90),
                "Annual"    => TimeSpan.FromDays(365),
                "Trial"     => TimeSpan.FromDays(7),
                _           => TimeSpan.FromDays(30)
            };

            var sub = new UserSubscription
            {
                UserID = userId,
                PlanKey = planKey,
                BillingCycle = billingCycle,
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.Add(duration),
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.UserSubscriptions.Add(sub);
            await _context.SaveChangesAsync();
            return sub;
        }

        public async Task<bool> StartTrialAsync(int userId)
        {
            if (await HasUsedTrialAsync(userId)) return false;
            await ActivateSubscriptionAsync(userId, "Pro", "Trial");
            return true;
        }

        public async Task<bool> HasUsedTrialAsync(int userId)
            => await _context.UserSubscriptions
                .AnyAsync(s => s.UserID == userId && s.BillingCycle == "Trial");

        // ─── Orders ──────────────────────────────────────────────────

        public async Task<SubscriptionOrder> CreateOrderAsync(int userId, string planKey, string billingCycle)
        {
            // Expire previous pending orders for same user
            var stale = await _context.SubscriptionOrders
                .Where(o => o.UserID == userId && o.Status == "Pending")
                .ToListAsync();
            foreach (var o in stale) o.Status = "Expired";

            var amount = GetPrice(planKey, billingCycle);
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var planCode = planKey.ToUpper();
            var order = new SubscriptionOrder
            {
                UserID = userId,
                PlanKey = planKey,
                BillingCycle = billingCycle,
                Amount = amount,
                TransactionRef = $"SUB{planCode}-{userId}-{ts}",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _context.SubscriptionOrders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> ConfirmOrderAsync(string transactionRef, decimal actualAmount)
        {
            var order = await _context.SubscriptionOrders
                .FirstOrDefaultAsync(o => o.TransactionRef == transactionRef && o.Status == "Pending");
            if (order == null) return false;
            if (actualAmount < order.Amount * 0.95m) return false; // allow 5% tolerance

            order.Status = "Confirmed";
            order.ConfirmedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await ActivateSubscriptionAsync(order.UserID, order.PlanKey, order.BillingCycle);
            return true;
        }

        public async Task<SubscriptionOrder?> GetPendingOrderAsync(int userId)
            => await _context.SubscriptionOrders
                .Where(o => o.UserID == userId && o.Status == "Pending" && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

        // ─── Match Credits ────────────────────────────────────────────

        public async Task<UserMatchCredit> CreateCreditOrderAsync(int userId)
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var credit = new UserMatchCredit
            {
                UserID = userId,
                RemainingCredits = 0, // will be set to 5 on confirm
                TransactionRef = $"SUBCREDIT-{userId}-{ts}",
                AmountPaid = CreditBundlePrice,
                Status = "Pending",
                PurchasedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            _context.UserMatchCredits.Add(credit);
            await _context.SaveChangesAsync();
            return credit;
        }

        public async Task<bool> ConfirmCreditOrderAsync(string transactionRef, decimal actualAmount)
        {
            var credit = await _context.UserMatchCredits
                .FirstOrDefaultAsync(c => c.TransactionRef == transactionRef && c.Status == "Pending");
            if (credit == null) return false;
            if (actualAmount < credit.AmountPaid * 0.95m) return false;

            credit.Status = "Confirmed";
            credit.RemainingCredits = CreditBundleCount;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetRemainingCreditsAsync(int userId)
        {
            return await _context.UserMatchCredits
                .Where(c => c.UserID == userId && c.Status == "Confirmed"
                         && c.RemainingCredits > 0 && c.ExpiresAt > DateTime.UtcNow)
                .SumAsync(c => c.RemainingCredits);
        }

        public async Task<bool> UseMatchCreditAsync(int userId)
        {
            var credit = await _context.UserMatchCredits
                .Where(c => c.UserID == userId && c.Status == "Confirmed"
                         && c.RemainingCredits > 0 && c.ExpiresAt > DateTime.UtcNow)
                .OrderBy(c => c.ExpiresAt)
                .FirstOrDefaultAsync();
            if (credit == null) return false;
            credit.RemainingCredits--;
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── Feature Gating ──────────────────────────────────────────

        public async Task<bool> CanCreateMatchAsync(int userId)
        {
            var plan = await GetCurrentPlanAsync(userId);
            if (plan.MonthlyCreateLimit == -1) return true;
            var count = await GetMonthlyCreateCountAsync(userId);
            return count < plan.MonthlyCreateLimit;
        }

        public async Task<bool> CanJoinMatchAsync(int userId)
        {
            var plan = await GetCurrentPlanAsync(userId);
            if (plan.MonthlyJoinLimit == -1) return true;
            var count = await GetMonthlyJoinCountAsync(userId);
            if (count < plan.MonthlyJoinLimit) return true;
            // Free users can use credits
            if (plan.PlanKey == "Free" && await GetRemainingCreditsAsync(userId) > 0) return true;
            return false;
        }

        public async Task<bool> CanSeePhoneNumberAsync(int userId)
            => (await GetCurrentPlanAsync(userId)).CanSeePhoneNumber;

        public async Task<bool> CanFilterByDistanceAsync(int userId)
            => (await GetCurrentPlanAsync(userId)).CanFilterByDistance;

        public async Task<bool> HasAiSuggestionsAsync(int userId)
            => (await GetCurrentPlanAsync(userId)).HasAiSuggestions;

        public async Task<bool> HasDetailedStatsAsync(int userId)
            => (await GetCurrentPlanAsync(userId)).HasDetailedStats;

        public async Task<bool> HasVerifiedBadgeAsync(int userId)
            => (await GetCurrentPlanAsync(userId)).HasVerifiedBadge;

        public async Task<bool> HasPlayerFeeExemptAsync(int userId)
            => (await GetCurrentPlanAsync(userId)).HasPlayerFeeExempt;

        public async Task<int> GetPriorityScoreAsync(int userId)
            => (await GetCurrentPlanAsync(userId)).PriorityScore;

        // ─── Usage Tracking ───────────────────────────────────────────

        private static string CurrentYearMonth => DateTime.UtcNow.ToString("yyyy-MM");

        public async Task RecordJoinAsync(int userId)
        {
            var ym = CurrentYearMonth;
            var usage = await _context.SubscriptionUsages
                .FirstOrDefaultAsync(u => u.UserID == userId && u.YearMonth == ym);
            if (usage == null)
            {
                _context.SubscriptionUsages.Add(new SubscriptionUsage
                {
                    UserID = userId, YearMonth = ym, JoinCount = 1, CreateCount = 0
                });
            }
            else usage.JoinCount++;
            await _context.SaveChangesAsync();
        }

        public async Task RecordCreateAsync(int userId)
        {
            var ym = CurrentYearMonth;
            var usage = await _context.SubscriptionUsages
                .FirstOrDefaultAsync(u => u.UserID == userId && u.YearMonth == ym);
            if (usage == null)
            {
                _context.SubscriptionUsages.Add(new SubscriptionUsage
                {
                    UserID = userId, YearMonth = ym, JoinCount = 0, CreateCount = 1
                });
            }
            else usage.CreateCount++;
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetMonthlyJoinCountAsync(int userId)
        {
            var ym = CurrentYearMonth;
            return await _context.SubscriptionUsages
                .Where(u => u.UserID == userId && u.YearMonth == ym)
                .Select(u => u.JoinCount)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetMonthlyCreateCountAsync(int userId)
        {
            var ym = CurrentYearMonth;
            return await _context.SubscriptionUsages
                .Where(u => u.UserID == userId && u.YearMonth == ym)
                .Select(u => u.CreateCount)
                .FirstOrDefaultAsync();
        }

        // ─── Admin ───────────────────────────────────────────────────

        public async Task<List<UserSubscription>> GetAllActiveSubscriptionsAsync()
            => await _context.UserSubscriptions
                .Include(s => s.User)
                .Where(s => s.Status == "Active" && s.EndAt > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        public async Task<int> GetActiveSubscriptionCountAsync()
            => await _context.UserSubscriptions
                .CountAsync(s => s.Status == "Active" && s.EndAt > DateTime.UtcNow);

        // ─── Pricing ─────────────────────────────────────────────────

        public decimal GetPrice(string planKey, string billingCycle)
            => GetPriceStatic(planKey, billingCycle);

        public static decimal GetPriceStatic(string planKey, string billingCycle)
        {
            if (!_prices.TryGetValue(planKey, out var p)) return 0;
            return billingCycle switch
            {
                "Quarterly" => p.Quarterly,
                "Annual"    => p.Annual,
                _           => p.Monthly
            };
        }
    }
}
