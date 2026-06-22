using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class MatchPaymentService : IMatchPaymentService
    {
        private readonly ApplicationDbContext _context;
        public const decimal DepositPerSlotFactor = 0.5m; // (max/2) slots
        public const decimal DepositPerSlotAmount = 5_000m;
        public const decimal PlayerFeeAmount = 5_000m;
        public static readonly TimeSpan PlayerFeeTimeout = TimeSpan.FromHours(1);

        public MatchPaymentService(ApplicationDbContext context) => _context = context;

        public decimal CalculateHostDeposit(int maxParticipants)
            => (int)(maxParticipants * DepositPerSlotFactor) * DepositPerSlotAmount;

        public async Task<MatchPayment> CreateHostDepositAsync(int matchId, int hostUserId)
        {
            var match = await _context.Matches.FindAsync(matchId)
                ?? throw new InvalidOperationException("Match not found");

            var amount = CalculateHostDeposit(match.MaxParticipants);
            var payment = new MatchPayment
            {
                MatchID = matchId,
                PayerUserID = hostUserId,
                PaymentType = "HostDeposit",
                Amount = amount,
                Status = "Pending",
                TransactionRef = $"DEPOSIT-{matchId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CreatedAt = DateTime.UtcNow
            };
            _context.MatchPayments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<MatchPayment> CreatePlayerFeeAsync(int matchId, int playerUserId)
        {
            var deadline = DateTime.UtcNow.Add(PlayerFeeTimeout);
            var payment = new MatchPayment
            {
                MatchID = matchId,
                PayerUserID = playerUserId,
                PaymentType = "PlayerFee",
                Amount = PlayerFeeAmount,
                Status = "Pending",
                ExpiresAt = deadline,
                TransactionRef = $"FEE-{matchId}-{playerUserId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CreatedAt = DateTime.UtcNow
            };
            _context.MatchPayments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<MatchPayment> CreateHostRemainingAsync(int matchId, int hostUserId)
        {
            var match = await _context.Matches.FindAsync(matchId)
                ?? throw new InvalidOperationException("Match not found");

            var amount = CalculateHostDeposit(match.MaxParticipants); // same as deposit
            var payment = new MatchPayment
            {
                MatchID = matchId,
                PayerUserID = hostUserId,
                PaymentType = "HostRemaining",
                Amount = amount,
                Status = "Pending",
                TransactionRef = $"REMAINING-{matchId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CreatedAt = DateTime.UtcNow
            };
            _context.MatchPayments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<MatchPayment?> GetActivePaymentAsync(int matchId, int userId, string paymentType)
        {
            return await _context.MatchPayments
                .Where(p => p.MatchID == matchId
                    && p.PayerUserID == userId
                    && p.PaymentType == paymentType
                    && p.Status == "Pending")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        // Tìm payment có thể nộp lại biên lai: Pending hoặc Refunded (bị reject bởi admin)
        private async Task<MatchPayment?> GetResubmittablePaymentAsync(int matchId, int userId, string paymentType)
        {
            return await _context.MatchPayments
                .Where(p => p.MatchID == matchId
                    && p.PayerUserID == userId
                    && p.PaymentType == paymentType
                    && (p.Status == "Pending" || p.Status == "Refunded"))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> SubmitHostDepositReceiptAsync(int matchId, int userId, string receiptUrl)
        {
            var payment = await GetResubmittablePaymentAsync(matchId, userId, "HostDeposit");
            if (payment == null) return false;
            payment.Status = "Pending";
            payment.ReceiptUrl = receiptUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SubmitPlayerFeeReceiptAsync(int matchId, int userId, string receiptUrl)
        {
            var payment = await GetResubmittablePaymentAsync(matchId, userId, "PlayerFee");
            if (payment == null) return false;
            // Bỏ qua deadline nếu đây là lần nộp lại sau khi bị reject
            if (payment.Status == "Pending" && payment.ExpiresAt.HasValue && payment.ExpiresAt < DateTime.UtcNow)
                return false;
            payment.Status = "Pending";
            payment.ReceiptUrl = receiptUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SubmitHostRemainingReceiptAsync(int matchId, int userId, string receiptUrl)
        {
            var payment = await GetResubmittablePaymentAsync(matchId, userId, "HostRemaining");
            if (payment == null) return false;
            payment.Status = "Pending";
            payment.ReceiptUrl = receiptUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmPaymentAsync(int paymentId)
        {
            var payment = await _context.MatchPayments
                .Include(p => p.Match)
                .FirstOrDefaultAsync(p => p.MatchPaymentID == paymentId && p.Status == "Pending");
            if (payment == null) return false;

            payment.Status = "Confirmed";
            payment.ConfirmedAt = DateTime.UtcNow;

            switch (payment.PaymentType)
            {
                case "HostDeposit":
                    payment.Match.DepositStatus = "Paid";
                    payment.Match.Status = "Open";
                    break;

                case "PlayerFee":
                    var participant = await _context.MatchParticipants
                        .FirstOrDefaultAsync(p => p.MatchID == payment.MatchID
                            && p.UserID == payment.PayerUserID
                            && p.JoinStatus == "Approved");
                    if (participant != null)
                    {
                        participant.JoinStatus = "Accepted";
                        participant.PlayerFeeStatus = "Paid";

                        var accepted = await _context.MatchParticipants
                            .CountAsync(p => p.MatchID == payment.MatchID
                                && (p.JoinStatus == "Accepted" || p.JoinStatus == "Approved"));
                        if (accepted >= payment.Match.MaxParticipants)
                            payment.Match.Status = "Full";
                    }
                    break;

                case "HostRemaining":
                    payment.Match.RemainingFeeStatus = "Paid";
                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectPaymentAsync(int paymentId)
        {
            var payment = await _context.MatchPayments
                .FirstOrDefaultAsync(p => p.MatchPaymentID == paymentId && p.Status == "Pending");
            if (payment == null) return false;
            payment.Status = "Refunded";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AdminRevenueStats> GetRevenueStatsAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var confirmed = await _context.MatchPayments
                .Where(p => p.Status == "Confirmed")
                .ToListAsync();

            var pendingCount = await _context.MatchPayments
                .CountAsync(p => p.Status == "Pending" && p.ReceiptUrl != null);

            return new AdminRevenueStats
            {
                TotalRevenue           = confirmed.Sum(p => p.Amount),
                TodayRevenue           = confirmed.Where(p => p.ConfirmedAt.HasValue && p.ConfirmedAt.Value >= todayStart).Sum(p => p.Amount),
                ThisMonthRevenue       = confirmed.Where(p => p.ConfirmedAt.HasValue && p.ConfirmedAt.Value >= monthStart).Sum(p => p.Amount),
                TotalConfirmedPayments = confirmed.Count,
                PendingPaymentsCount   = pendingCount
            };
        }

        public async Task<List<TransactionHistoryItem>> GetTransactionHistoryAsync()
        {
            return await _context.MatchPayments
                .Include(p => p.Match)
                .Include(p => p.Payer)
                .Where(p => p.Status == "Confirmed" && p.ConfirmedAt.HasValue)
                .OrderByDescending(p => p.ConfirmedAt)
                .Select(p => new TransactionHistoryItem
                {
                    PaymentId   = p.MatchPaymentID,
                    MatchId     = p.MatchID,
                    MatchTitle  = p.Match.Title ?? p.Match.MatchType,
                    PayerUserId = p.PayerUserID,
                    PayerName   = p.Payer.FullName,
                    PaymentType = p.PaymentType,
                    Amount      = p.Amount,
                    ReceiptUrl  = p.ReceiptUrl,
                    ConfirmedAt = p.ConfirmedAt!.Value
                })
                .ToListAsync();
        }

        public async Task<List<MatchPaymentAdminItem>> GetPendingPaymentsAsync()
        {
            return await _context.MatchPayments
                .Include(p => p.Match)
                .Include(p => p.Payer)
                .Where(p => p.Status == "Pending" && p.ReceiptUrl != null)
                .OrderBy(p => p.CreatedAt)
                .Select(p => new MatchPaymentAdminItem
                {
                    PaymentId = p.MatchPaymentID,
                    MatchId = p.MatchID,
                    MatchTitle = p.Match.Title ?? p.Match.MatchType,
                    PayerUserId = p.PayerUserID,
                    PayerName = p.Payer.FullName,
                    PaymentType = p.PaymentType,
                    Amount = p.Amount,
                    ReceiptUrl = p.ReceiptUrl,
                    CreatedAt = p.CreatedAt,
                    ExpiresAt = p.ExpiresAt
                })
                .ToListAsync();
        }

        public async Task<List<ExpiredPlayerFee>> ExpirePlayerFeesAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var expiredParticipants = await _context.MatchParticipants
                .Include(p => p.Match)
                .Where(p => p.JoinStatus == "Approved"
                    && p.PlayerFeeStatus == "AwaitingPayment"
                    && p.PlayerFeeDeadline.HasValue
                    && p.PlayerFeeDeadline < now)
                .ToListAsync(ct);

            if (expiredParticipants.Count == 0) return new();

            var result = new List<ExpiredPlayerFee>();
            foreach (var p in expiredParticipants)
            {
                p.JoinStatus = "Cancelled";
                p.PlayerFeeStatus = "Expired";

                var pendingPayment = await _context.MatchPayments
                    .FirstOrDefaultAsync(mp => mp.MatchID == p.MatchID
                        && mp.PayerUserID == p.UserID
                        && mp.PaymentType == "PlayerFee"
                        && mp.Status == "Pending", ct);
                if (pendingPayment != null) pendingPayment.Status = "Expired";

                if (p.Match.Status == "Full") p.Match.Status = "Open";

                var title = string.IsNullOrWhiteSpace(p.Match.Title) ? p.Match.MatchType : p.Match.Title;
                result.Add(new ExpiredPlayerFee(p.UserID, p.MatchID, title ?? "Trận đấu", p.ParticipantID));
            }

            await _context.SaveChangesAsync(ct);
            return result;
        }

        public async Task<List<(int MatchId, int HostUserId, string MatchTitle, decimal RemainingAmount)>> NotifyRemainingFeeAsync(CancellationToken ct = default)
        {
            var now = DateTime.Now;

            var candidates = await _context.Matches
                .Where(m => m.RemainingFeeStatus == "NotDue" && m.DepositStatus == "Paid")
                .ToListAsync(ct);

            var result = new List<(int, int, string, decimal)>();
            foreach (var m in candidates)
            {
                var matchStart = m.MatchDate.Date + m.StartTime;
                if (matchStart <= now)
                {
                    m.RemainingFeeStatus = "Notified";

                    // Create HostRemaining payment record with 48h deadline
                    var remaining = CalculateHostDeposit(m.MaxParticipants);
                    _context.MatchPayments.Add(new MatchPayment
                    {
                        MatchID = m.MatchID,
                        PayerUserID = m.CreatedByUserID,
                        PaymentType = "HostRemaining",
                        Amount = remaining,
                        Status = "Pending",
                        TransactionRef = $"REMAINING-{m.MatchID}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddHours(48)
                    });

                    var title = string.IsNullOrWhiteSpace(m.Title) ? m.MatchType : m.Title;
                    result.Add((m.MatchID, m.CreatedByUserID, title ?? "Trận đấu", remaining));
                }
            }

            if (result.Count > 0)
                await _context.SaveChangesAsync(ct);

            return result;
        }

        public async Task<List<DailyRevenue>> GetDailyRevenueAsync(int days = 7)
        {
            var since = DateTime.UtcNow.Date.AddDays(-(days - 1));
            var payments = await _context.MatchPayments
                .Where(p => p.Status == "Confirmed" && p.ConfirmedAt >= since)
                .Select(p => new { p.PaymentType, p.Amount, Date = p.ConfirmedAt!.Value.Date })
                .ToListAsync();

            return Enumerable.Range(0, days)
                .Select(i => since.AddDays(i))
                .Select(date => new DailyRevenue
                {
                    Date = date,
                    HostDepositTotal  = payments.Where(p => p.Date == date && p.PaymentType == "HostDeposit").Sum(p => p.Amount),
                    PlayerFeeTotal    = payments.Where(p => p.Date == date && p.PaymentType == "PlayerFee").Sum(p => p.Amount),
                    HostRemainingTotal = payments.Where(p => p.Date == date && p.PaymentType == "HostRemaining").Sum(p => p.Amount)
                })
                .ToList();
        }

        public async Task<List<UnpaidRemainingFee>> GetUnpaidRemainingFeesAsync()
        {
            var unpaid = await _context.Matches
                .Include(m => m.CreatedByUser)
                .Where(m => m.RemainingFeeStatus == "Notified")
                .ToListAsync();

            var result = new List<UnpaidRemainingFee>();
            foreach (var m in unpaid)
            {
                var payment = await _context.MatchPayments
                    .Where(p => p.MatchID == m.MatchID && p.PaymentType == "HostRemaining")
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                // Skip if already confirmed or receipt submitted
                if (payment?.Status == "Confirmed") continue;
                if (payment?.ReceiptUrl != null) continue;

                var title = string.IsNullOrWhiteSpace(m.Title) ? m.MatchType : m.Title;
                result.Add(new UnpaidRemainingFee
                {
                    MatchId = m.MatchID,
                    MatchTitle = title ?? "Trận đấu",
                    HostName = m.CreatedByUser.FullName,
                    Amount = CalculateHostDeposit(m.MaxParticipants),
                    MatchDate = m.MatchDate,
                    PaymentCreatedAt = payment?.CreatedAt
                });
            }
            return result;
        }
    }
}
