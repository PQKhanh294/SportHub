using SportHub.Services.Interfaces;

namespace SportHub.Services
{
    /// <summary>
    /// Chạy nền mỗi 5 phút để xử lý:
    /// 1. Hủy yêu cầu Pending quá 1 giờ chưa được host duyệt
    /// 2. Hủy chỗ Approved quá hạn 1 giờ chưa thanh toán phí 5K
    /// 3. Thông báo host hoàn thành 50% phí còn lại khi trận bắt đầu
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
                            $"Bạn không thanh toán phí 5,000 VND đúng hạn. Chỗ tại trận \"{item.MatchTitle}\" đã bị hủy.",
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
                            $"Trận \"{matchTitle}\" đã bắt đầu. Vui lòng hoàn thành {remaining:N0} VND phí dịch vụ còn lại.",
                            $"/Matchmaking/Payment?matchId={matchId}&type=remaining");
                    }
                    if (remainingFeeMatches.Count > 0)
                        _logger.LogInformation("Sent {Count} remaining fee notification(s).", remainingFeeMatches.Count);
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
