using SportHub.Services.Interfaces;

namespace SportHub.Services
{
    /// <summary>Tự hủy yêu cầu tham gia Pending quá 2 giờ và gửi thông báo cho người chơi.</summary>
    public class PendingJoinExpiryHostedService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan MaxPendingAge = TimeSpan.FromHours(2);

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
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var expired = await matchService.ExpireStalePendingJoinsAsync(MaxPendingAge, stoppingToken);
                    foreach (var item in expired)
                    {
                        await notificationService.CreateAsync(
                            item.UserId,
                            "MatchJoinExpired",
                            "Yêu cầu tham gia đã hết hạn",
                            $"Host chưa duyệt trong 2 giờ. Yêu cầu vào trận \"{item.MatchTitle}\" đã được hủy. Bạn có thể gửi lại nếu trận còn chỗ.",
                            $"/Matchmaking/Details?id={item.MatchId}");
                    }

                    if (expired.Count > 0)
                        _logger.LogInformation("Expired {Count} stale pending join request(s).", expired.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while expiring pending join requests.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }
    }
}
