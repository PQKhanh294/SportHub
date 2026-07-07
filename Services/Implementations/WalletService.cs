using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Hubs;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMatchPaymentService _matchPaymentService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEmailService _emailService;

        public WalletService(ApplicationDbContext context, IMatchPaymentService matchPaymentService, IHubContext<NotificationHub> hubContext, IEmailService emailService)
        {
            _context = context;
            _matchPaymentService = matchPaymentService;
            _hubContext = hubContext;
            _emailService = emailService;
        }

        public async Task<decimal> GetBalanceAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.WalletBalance ?? 0m;
        }

        public async Task CreditAsync(int userId, decimal amount, string description, int? matchId = null, string type = "AdminCredit", bool sendEmail = true)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException($"User {userId} not found");

            user.WalletBalance += amount;
            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserID = userId,
                Amount = amount,
                Type = type,
                Description = description,
                RelatedMatchID = matchId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Push realtime balance update to user
            await _hubContext.Clients.Group($"user:{userId}").SendAsync("WalletCredited", new
            {
                amount,
                description,
                newBalance = user.WalletBalance
            });

            if (sendEmail && user.NotifyByEmail && !string.IsNullOrWhiteSpace(user.Email))
                await _emailService.SendWalletCreditedAsync(user.Email, user.FullName, amount, description);
        }

        public async Task<bool> DeductAsync(int userId, decimal amount, string description, int? matchId = null, string type = "Deduction")
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.WalletBalance < amount) return false;

            user.WalletBalance -= amount;
            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserID = userId,
                Amount = -amount,
                Type = type,
                Description = description,
                RelatedMatchID = matchId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<WalletTransaction>> GetHistoryAsync(int userId, int limit = 20)
        {
            return await _context.WalletTransactions
                .Where(wt => wt.UserID == userId)
                .OrderByDescending(wt => wt.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // ---- Top-up ----

        public async Task<WalletTopUpRequest> CreateTopUpRequestAsync(int userId, decimal amount)
        {
            // Expire any old pending request for this user first
            var old = await _context.WalletTopUpRequests
                .Where(t => t.UserID == userId && t.Status == "Pending")
                .ToListAsync();
            foreach (var o in old) o.Status = "Expired";

            var ts = DateTime.UtcNow;
            var req = new WalletTopUpRequest
            {
                UserID = userId,
                Amount = amount,
                TransactionRef = $"TOPUP-{userId}-{ts:yyyyMMddHHmmss}",
                Status = "Pending",
                CreatedAt = ts,
                ExpiresAt = ts.AddHours(24)
            };
            _context.WalletTopUpRequests.Add(req);
            await _context.SaveChangesAsync();
            return req;
        }

        public async Task<bool> ConfirmTopUpAsync(string transactionRef, decimal actualAmount)
        {
            var req = await _context.WalletTopUpRequests
                .FirstOrDefaultAsync(t => t.TransactionRef == transactionRef && t.Status == "Pending");
            if (req == null) return false;

            req.Status = "Confirmed";
            req.ConfirmedAt = DateTime.UtcNow;
            req.ActualAmount = actualAmount;
            await _context.SaveChangesAsync();

            // CreditAsync fires WalletCredited SignalR event + creates transaction
            await CreditAsync(req.UserID, actualAmount, $"Nạp ví qua chuyển khoản ({transactionRef})", type: "TopUp");
            return true;
        }

        public async Task<WalletTopUpRequest?> GetPendingTopUpAsync(int userId)
        {
            return await _context.WalletTopUpRequests
                .Where(t => t.UserID == userId && t.Status == "Pending" && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<WalletTopUpRequest>> GetTopUpHistoryAsync(int userId, int limit = 20)
        {
            return await _context.WalletTopUpRequests
                .Where(t => t.UserID == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task CancelTopUpRequestAsync(int userId)
        {
            var pending = await _context.WalletTopUpRequests
                .Where(t => t.UserID == userId && t.Status == "Pending")
                .ToListAsync();
            foreach (var t in pending) t.Status = "Expired";
            if (pending.Count > 0) await _context.SaveChangesAsync();
        }

        // ---- Pay match fee from wallet ----

        public async Task<bool> PayMatchFeeFromWalletAsync(int userId, int matchId, string paymentType)
        {
            var payment = await _matchPaymentService.GetActivePaymentAsync(matchId, userId, paymentType);
            if (payment == null) return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.WalletBalance < payment.Amount) return false;

            // Deduct wallet
            user.WalletBalance -= payment.Amount;
            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserID = userId,
                Amount = -payment.Amount,
                Type = "MatchPayment",
                Description = $"Thanh toán {paymentType} cho trận #{matchId}",
                RelatedMatchID = matchId,
                CreatedAt = DateTime.UtcNow
            });

            // Mark payment confirmed (bypasses admin)
            payment.Status = "Confirmed";
            payment.ConfirmedAt = DateTime.UtcNow;

            // Apply same side-effects as MatchPaymentService.ConfirmPaymentAsync
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);
            if (match != null)
            {
                switch (paymentType)
                {
                    case "HostDeposit":
                        match.DepositStatus = "Paid";
                        match.Status = "Open";
                        break;
                    case "PlayerFee":
                        var participant = match.Participants.FirstOrDefault(p =>
                            p.UserID == userId && p.JoinStatus == "Approved");
                        if (participant != null)
                        {
                            participant.JoinStatus = "Accepted";
                            participant.PlayerFeeStatus = "Paid";
                            var acceptedCount = match.Participants.Count(p =>
                                p.JoinStatus == "Accepted" || p.JoinStatus == "Approved");
                            if (acceptedCount >= match.MaxParticipants)
                                match.Status = "Full";
                        }
                        break;
                    case "HostRemaining":
                        match.RemainingFeeStatus = "Paid";
                        break;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
