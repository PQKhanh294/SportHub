using Microsoft.EntityFrameworkCore;
using SportHub.Common;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Implementations;
using SportHub.Services.Interfaces;

namespace SportHub.Services
{
    /// <summary>
    /// Chạy nền mỗi 5 phút để xử lý:
    /// 1. Hủy yêu cầu Pending quá 1 giờ chưa được host duyệt
    /// 2. Hủy chỗ Approved quá hạn 1 giờ chưa thanh toán phí 5K
    /// 3. Nhắc host thanh toán phí còn lại (HostRemaining) đang Pending — 24h + 4h khẩn
    /// 4. Tự động chuyển trận sang Completed khi đã qua giờ kết thúc; quyết toán phí host theo số người chơi
    ///    THỰC TẾ (5.000 xu/người) so với cọc đã đóng — thừa thì hoàn ví, thiếu thì tự trừ ví/thông báo thu thêm;
    ///    gửi nhắc đánh giá
    /// 5. Gửi email nhắc nhở người chơi ~2h trước giờ trận bắt đầu
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

        private async Task RemindRemainingFeesAsync(IServiceScope scope, INotificationService notificationService, IEmailService emailService, CancellationToken ct)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = (config["App:BaseUrl"] ?? "https://sporthub-dn.id.vn/").TrimEnd('/');
            var now = DateTime.UtcNow;

            var pendingRemaining = await db.MatchPayments
                .Include(p => p.Match)
                .Include(p => p.Payer)
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
                if (p.Payer?.NotifyByEmail == true && p.Payer.NotifyPaymentReminder && !string.IsNullOrWhiteSpace(p.Payer.Email))
                    await emailService.SendPaymentReminderAsync(p.Payer.Email, p.Payer.FullName, title, p.PaymentType,
                        p.Amount, p.ExpiresAt ?? now.AddHours(24), $"{baseUrl}/Matchmaking/Payment?matchId={p.MatchID}&type=remaining", urgent: false);
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
                if (p.Payer?.NotifyByEmail == true && p.Payer.NotifyPaymentReminder && !string.IsNullOrWhiteSpace(p.Payer.Email))
                    await emailService.SendPaymentReminderAsync(p.Payer.Email, p.Payer.FullName, title, p.PaymentType,
                        p.Amount, p.ExpiresAt ?? now.AddHours(4), $"{baseUrl}/Matchmaking/Payment?matchId={p.MatchID}&type=remaining", urgent: true);
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
            // Vietnam is UTC+7; match end time stored as local time, so we compare with giờ Việt Nam
            var nowLocal = VietnamTime.Now;
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

                // Quyết toán phí host theo số người chơi THỰC TẾ đã tham gia (5.000 xu/người), so với tiền cọc
                // đã đóng trước đó (nửa giá theo sức chứa lúc tạo trận): thừa thì hoàn, thiếu thì thu thêm.
                // Trước đây "phí còn lại" được tính cố định theo MaxParticipants ngay khi trận BẮT ĐẦU, sai
                // lệch với số người chơi thực tế cuối cùng — nay dời hẳn về lúc KẾT THÚC trận cho chính xác.
                if (match.DepositStatus == "Paid")
                {
                    var deposit = await db.MatchPayments
                        .FirstOrDefaultAsync(p => p.MatchID == match.MatchID
                            && p.PaymentType == "HostDeposit"
                            && p.Status == "Confirmed", ct);

                    // Dedup: nếu đã hoàn tiền hoặc đã tạo payment HostRemaining cho trận này rồi thì bỏ qua
                    // (phòng trường hợp job bị restart giữa chừng sau khi CreditAsync/tạo payment đã lưu xong
                    // nhưng match.Status = "Completed" chưa kịp lưu ở batch cuối).
                    var alreadySettled = deposit != null && (
                        await db.WalletTransactions.AnyAsync(t => t.RelatedMatchID == match.MatchID && t.Type == "Refund", ct) ||
                        await db.MatchPayments.AnyAsync(p => p.MatchID == match.MatchID && p.PaymentType == "HostRemaining", ct));

                    if (deposit != null && !alreadySettled)
                    {
                        var actualFee = acceptedPlayers.Count * MatchPaymentService.PlayerFeeAmount;
                        var diff = actualFee - deposit.Amount;

                        if (diff < 0)
                        {
                            var refundAmount = -diff;
                            await walletService.CreditAsync(
                                match.CreatedByUserID,
                                refundAmount,
                                acceptedPlayers.Count == 0
                                    ? $"Hoàn đặt cọc — trận \"{matchTitle}\" không có người tham gia"
                                    : $"Hoàn chênh lệch cọc — trận \"{matchTitle}\" chỉ có {acceptedPlayers.Count} người tham gia",
                                match.MatchID,
                                "Refund");

                            await notificationService.CreateAsync(
                                match.CreatedByUserID,
                                "WalletCredit",
                                "Hoàn tiền cọc",
                                $"Trận \"{matchTitle}\" kết thúc với {acceptedPlayers.Count} người tham gia. Đã hoàn {refundAmount:N0} xu vào ví.",
                                "/Wallet");

                            match.RemainingFeeStatus = "Paid";
                            _logger.LogInformation("Refunded {Amount} to host {HostId} for match {MatchId} ({Count} players).",
                                refundAmount, match.CreatedByUserID, match.MatchID, acceptedPlayers.Count);
                        }
                        else if (diff > 0)
                        {
                            db.MatchPayments.Add(new MatchPayment
                            {
                                MatchID = match.MatchID,
                                PayerUserID = match.CreatedByUserID,
                                PaymentType = "HostRemaining",
                                Amount = diff,
                                Status = "Pending",
                                TransactionRef = $"REMAINING-{match.MatchID}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                                CreatedAt = DateTime.UtcNow,
                                ExpiresAt = DateTime.UtcNow.AddHours(48)
                            });
                            match.RemainingFeeStatus = "Notified";
                            // Lưu trước để GetActivePaymentAsync (dùng trong PayMatchFeeFromWalletAsync) tìm thấy payment vừa tạo
                            await db.SaveChangesAsync(ct);

                            var autoPaid = await walletService.PayMatchFeeFromWalletAsync(match.CreatedByUserID, match.MatchID, "HostRemaining");
                            if (autoPaid)
                            {
                                await notificationService.CreateAsync(
                                    match.CreatedByUserID,
                                    "MatchPaymentConfirmed",
                                    "Đã tự động thanh toán phí còn lại",
                                    $"Trận \"{matchTitle}\" có {acceptedPlayers.Count} người tham gia — đã tự động trừ {diff:N0} xu phí còn lại từ ví.",
                                    $"/Matchmaking/Details?id={match.MatchID}");
                            }
                            else
                            {
                                await notificationService.CreateAsync(
                                    match.CreatedByUserID,
                                    "MatchRemainingFeeRequired",
                                    "Trận đã kết thúc — hoàn thành phí còn lại",
                                    $"Trận \"{matchTitle}\" có {acceptedPlayers.Count} người tham gia — vui lòng thanh toán thêm {diff:N0} xu phí dịch vụ.",
                                    $"/Matchmaking/Payment?matchId={match.MatchID}&type=remaining");
                            }

                            _logger.LogInformation("Charged extra {Amount} to host {HostId} for match {MatchId} ({Count} players, autoPaid={AutoPaid}).",
                                diff, match.CreatedByUserID, match.MatchID, acceptedPlayers.Count, autoPaid);
                        }
                        else
                        {
                            match.RemainingFeeStatus = "Paid";
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

        private async Task ExpireCommunityListingsAsync(IServiceScope scope, CancellationToken ct)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var count = await db.CommunityListings
                .Where(c => c.Status == "Active" && c.ExpiresAt <= DateTime.UtcNow)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, "Expired"), ct);
            if (count > 0)
                _logger.LogInformation("Expired {Count} community listing(s).", count);
        }

        private async Task SendMatchRemindersAsync(IServiceScope scope, CancellationToken ct)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var nowLocal = VietnamTime.Now;
            var windowStart = nowLocal.AddHours(2);
            var windowEnd = windowStart.Add(CheckInterval);

            var candidateMatches = await db.Matches
                .Include(m => m.Participants).ThenInclude(p => p.User)
                .Include(m => m.Court).ThenInclude(c => c!.Venue)
                .Where(m => (m.Status == "Open" || m.Status == "Full")
                    && m.MatchDate >= nowLocal.Date && m.MatchDate <= windowEnd.Date)
                .ToListAsync(ct);

            var upcomingMatches = candidateMatches
                .Where(m => m.MatchDate.Date + m.StartTime >= windowStart && m.MatchDate.Date + m.StartTime < windowEnd)
                .ToList();

            if (upcomingMatches.Count == 0) return;

            var matchIds = upcomingMatches.Select(m => m.MatchID).ToList();
            var alreadyReminded = await db.MatchInteractions
                .Where(i => matchIds.Contains(i.MatchID) && i.Action == "MatchReminder")
                .Select(i => new { i.MatchID, i.UserID })
                .ToListAsync(ct);
            var remindedSet = alreadyReminded.Select(x => (x.MatchID, x.UserID)).ToHashSet();

            var sentCount = 0;
            foreach (var match in upcomingMatches)
            {
                var matchTitle = match.Title ?? match.MatchType;
                var matchDateStr = $"{match.MatchDate:dd/MM/yyyy} {match.StartTime:hh\\:mm}";
                var venue = match.CustomCourtName ?? match.Court?.Venue?.VenueName ?? "Chưa xác định";

                foreach (var p in match.Participants.Where(p => p.JoinStatus == "Accepted"))
                {
                    if (remindedSet.Contains((match.MatchID, p.UserID))) continue;

                    if (p.User?.NotifyByEmail == true && p.User.NotifyMatchReminder && !string.IsNullOrWhiteSpace(p.User.Email))
                    {
                        await emailService.SendMatchReminderAsync(p.User.Email, p.User.FullName, matchTitle, matchDateStr, venue);
                        sentCount++;
                    }
                    db.MatchInteractions.Add(new MatchInteraction { MatchID = match.MatchID, UserID = p.UserID, Action = "MatchReminder", CreatedAt = DateTime.UtcNow });
                }
            }

            await db.SaveChangesAsync(ct);
            if (sentCount > 0)
                _logger.LogInformation("Sent {Count} match reminder email(s).", sentCount);
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
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

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

                    // 3. Remind host to pay HostRemaining fee (24h warning + 4h urgent)
                    await RemindRemainingFeesAsync(scope, notificationService, emailService, stoppingToken);

                    // 4. Auto-complete matches that have ended: quyết toán phí host theo số người chơi
                    // THỰC TẾ (không phải theo sức chứa) + gửi nhắc đánh giá
                    await AutoCompleteMatchesAsync(scope, notificationService, stoppingToken);

                    // 5. Send match reminder emails ~2h before start
                    await SendMatchRemindersAsync(scope, stoppingToken);

                    // 6. AI analysis cho khiếu nại quá hạn nhân chứng 48h
                    var disputeService = scope.ServiceProvider.GetRequiredService<IDisputeService>();
                    await disputeService.ProcessPendingAiAnalysisAsync(stoppingToken);

                    // 7. Đánh dấu Expired cho tin cộng đồng quá hạn (feed đã tự lọc theo ExpiresAt,
                    // bước này chỉ để cập nhật cờ Status phục vụ trang Admin)
                    await ExpireCommunityListingsAsync(scope, stoppingToken);
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
