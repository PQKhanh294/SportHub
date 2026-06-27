using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services
{
    /// <summary>
    /// Chạy nền mỗi 5 phút để xử lý:
    /// 1. Hủy yêu cầu Pending quá 1 giờ chưa được host duyệt
    /// 2. Hủy chỗ Approved quá hạn 1 giờ chưa thanh toán phí 5K
    /// 3. Thông báo host hoàn thành 50% phí còn lại khi trận bắt đầu
    /// 4. Tự động chuyển trận sang Completed khi đã qua giờ kết thúc + gửi nhắc đánh giá
    /// </summary>
    public class PendingJoinExpiryHostedService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan MaxPendingAge = TimeSpan.FromHours(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PendingJoinExpiryHostedService> _logger;

        public PendingJoinExpiryHostedService(
            IServiceProvider serviceProvider,
            ILogger<PendingJoinExpiryHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private async Task RemindRemainingFeesAsync(IServiceScope scope, INotificationService notificationService, CancellationToken ct)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var pendingRemaining = await db.MatchPayments
                .Include(p => p.Match)
                .Where(p => p.PaymentType == "HostRemaining"
                    && p.Status == "Pending"
                    && p.ReceiptUrl == null
                    && p.ReminderSentAt == null)
                .ToListAsync(ct);

            var toRemind = new List<MatchPayment>();
            var urgentRemind = new List<MatchPayment>();

            foreach (var p in pendingRemaining)
            {
                var age = now - p.CreatedAt;
                var remaining = p.ExpiresAt.HasValue ? (p.ExpiresAt.Value - now) : TimeSpan.MaxValue;

                if (remaining <= TimeSpan.FromHours(4))
                    urgentRemind.Add(p);
                else if (age >= TimeSpan.FromHours(24))
                    toRemind.Add(p);
            }

            foreach (var p in toRemind)
            {
                var title = string.IsNullOrWhiteSpace(p.Match.Title) ? p.Match.MatchType : p.Match.Title;
                await notificationService.CreateAsync(
                    p.PayerUserID,
                    "RemainingFeeReminder",
                    "Nhắc nhở: còn 24h để nộp phí còn lại",
                    $"Trận \"{title}\" — còn 24 giờ để nộp phí dịch vụ còn lại {p.Amount:N0} xu.",
                    $"/Matchmaking/Payment?matchId={p.MatchID}&type=remaining");
                p.ReminderSentAt = now;
            }

            foreach (var p in urgentRemind)
            {
                var title = string.IsNullOrWhiteSpace(p.Match.Title) ? p.Match.MatchType : p.Match.Title;
                await notificationService.CreateAsync(
                    p.PayerUserID,
                    "RemainingFeeReminder",
                    "Khẩn: còn ít hơn 4h nộp phí còn lại!",
                    $"Trận \"{title}\" — còn ít hơn 4 giờ để nộp phí dịch vụ còn lại {p.Amount:N0} xu. Hãy nộp ngay!",
                    $"/Matchmaking/Payment?matchId={p.MatchID}&type=remaining");
                p.ReminderSentAt = now;
            }

            if (toRemind.Count + urgentRemind.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Sent {Count} HostRemaining reminder(s).", toRemind.Count + urgentRemind.Count);
            }
        }

        private async Task AutoCompleteMatchesAsync(IServiceScope scope, INotificationService notificationService, CancellationToken ct)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var walletService = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var nowUtc = DateTime.UtcNow;
            // Vietnam is UTC+7; match end time stored as local time, so we compare with nowUtc + 7h
            var nowLocal = nowUtc.AddHours(7);
            var todayDate = DateOnly.FromDateTime(nowLocal);

            var endedMatches = await db.Matches
                .Include(m => m.Participants)
                .Where(m => (m.Status == "Open" || m.Status == "Full" || m.Status == "InProgress")
                    && (m.MatchDate < todayDate.ToDateTime(TimeOnly.MinValue)
                        || (m.MatchDate == todayDate.ToDateTime(TimeOnly.MinValue) && m.EndTime <= nowLocal.TimeOfDay)))
                .ToListAsync(ct);

            foreach (var match in endedMatches)
            {
                match.Status = "Completed";

                var acceptedPlayers = match.Participants.Where(p => p.JoinStatus == "Accepted").ToList();
                var matchTitle = match.Title ?? match.MatchType;

                // Auto-refund host deposit when no players joined
                if (acceptedPlayers.Count == 0 && match.DepositStatus == "Paid")
                {
                    var deposit = await db.MatchPayments
                        .FirstOrDefaultAsync(p => p.MatchID == match.MatchID
                            && p.PaymentType == "HostDeposit"
                            && p.Status == "Confirmed", ct);

                    if (deposit != null)
                    {
                        // Dedup: check if refund already issued for this match
                        var alreadyRefunded = await db.WalletTransactions
                            .AnyAsync(t => t.UserID == match.CreatedByUserID
                                && t.RelatedMatchID == match.MatchID
                                && t.Type == "Refund", ct);

                        if (!alreadyRefunded)
                        {
                            await walletService.CreditAsync(
                                match.CreatedByUserID,
                                deposit.Amount,
                                $"Hoàn đặt cọc — trận \"{matchTitle}\" không có người tham gia",
                                match.MatchID,
                                "Refund");

                            await notificationService.CreateAsync(
                                match.CreatedByUserID,
                                "WalletCredit",
                                "Hoàn tiền đặt cọc",
                                $"Trận \"{matchTitle}\" kết thúc mà không có người tham gia. {deposit.Amount:N0} xu đã được hoàn vào ví.",
                                "/Wallet");

                            _logger.LogInformation("Refunded {Amount} to host {HostId} for empty match {MatchId}.",
                                deposit.Amount, match.CreatedByUserID, match.MatchID);
                        }
                    }
                }

                // Notify host to rate players
                if (acceptedPlayers.Count > 0)
                {
                    await notificationService.CreateAsync(
                        match.CreatedByUserID,
                        "MatchReviewReminder",
                        "Trận đã kết thúc — Đánh giá người chơi",
                        $"Trận \"{matchTitle}\" đã kết thúc. Hãy đánh giá {acceptedPlayers.Count} người chơi trong vòng 7 ngày.",
                        $"/Matchmaking/Review?matchId={match.MatchID}&mode=host");
                }

                // Notify accepted players to rate the match
                foreach (var participant in acceptedPlayers)
                {
                    await notificationService.CreateAsync(
                        participant.UserID,
                        "MatchReviewReminder",
                        "Trận đã kết thúc — Chia sẻ đánh giá",
                        $"Trận \"{matchTitle}\" đã kết thúc. Hãy chia sẻ đánh giá trong vòng 7 ngày!",
                        $"/Matchmaking/Review?matchId={match.MatchID}&mode=player");
                }
            }

            if (endedMatches.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Auto-completed {Count} match(es) and sent review reminders.", endedMatches.Count);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
                    var matchPaymentService = scope.ServiceProvider.GetRequiredService<IMatchPaymentService>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    // 1. Hủy Pending join quá 1 giờ
                    var expiredJoins = await matchService.ExpireStalePendingJoinsAsync(MaxPendingAge, stoppingToken);
                    foreach (var item in expiredJoins)
                    {
                        await notificationService.CreateAsync(
                            item.UserId,
                            "MatchJoinExpired",
                            "Yêu cầu tham gia đã hết hạn",
                            $"Host chưa duyệt trong 1 giờ. Yêu cầu vào trận \"{item.MatchTitle}\" đã được hủy. Bạn có thể gửi lại nếu trận còn chỗ.",
                            $"/Matchmaking/Details?id={item.MatchId}");
                    }
                    if (expiredJoins.Count > 0)
                        _logger.LogInformation("Expired {Count} stale pending join(s).", expiredJoins.Count);

                    // 2. Hủy Approved player quá hạn thanh toán 5K
                    var expiredFees = await matchPaymentService.ExpirePlayerFeesAsync(stoppingToken);
                    foreach (var item in expiredFees)
                    {
                        // Notify player
                        await notificationService.CreateAsync(
                            item.UserId,
                            "MatchJoinExpired",
                            "Hết hạn thanh toán phí tham gia",
                            $"Bạn không thanh toán phí 5,000 xu đúng hạn. Chỗ tại trận \"{item.MatchTitle}\" đã bị hủy.",
                            $"/Matchmaking/Details?id={item.MatchId}");
                    }
                    if (expiredFees.Count > 0)
                        _logger.LogInformation("Expired {Count} player fee deadline(s).", expiredFees.Count);

                    // 3. Thông báo host hoàn thành phí còn lại khi trận bắt đầu
                    var remainingFeeMatches = await matchPaymentService.NotifyRemainingFeeAsync(stoppingToken);
                    foreach (var (matchId, hostUserId, matchTitle, remaining) in remainingFeeMatches)
                    {
                        await notificationService.CreateAsync(
                            hostUserId,
                            "MatchRemainingFeeRequired",
                            "Trận đã bắt đầu — hoàn thành phí còn lại",
                            $"Trận \"{matchTitle}\" đã bắt đầu. Vui lòng hoàn thành {remaining:N0} xu phí dịch vụ còn lại.",
                            $"/Matchmaking/Payment?matchId={matchId}&type=remaining");
                    }
                    if (remainingFeeMatches.Count > 0)
                        _logger.LogInformation("Sent {Count} remaining fee notification(s).", remainingFeeMatches.Count);

                    // 4. Remind host to pay HostRemaining fee (24h warning + 4h urgent)
                    await RemindRemainingFeesAsync(scope, notificationService, stoppingToken);

                    // 5. Auto-complete matches that have ended + send review reminders
                    await AutoCompleteMatchesAsync(scope, notificationService, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in match maintenance background task.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }
    }
}
