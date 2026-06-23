using SportHub.Services.Interfaces;

namespace SportHub.Services
{
    public class PromotionSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PromotionSchedulerService> _logger;

        // Vietnam UTC+7
        private static readonly TimeZoneInfo _vnTz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

        public PromotionSchedulerService(IServiceProvider services, ILogger<PromotionSchedulerService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTz);

                // Run daily at 08:00 VN time
                var next8am = nowVn.Date.AddHours(8);
                if (nowVn.Hour >= 8) next8am = next8am.AddDays(1);

                var delay = next8am - nowVn;
                _logger.LogInformation("PromotionScheduler: next run at {NextRun} VN time ({Delay:hh\\:mm} from now)", next8am, delay);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                await RunDailyChecksAsync(stoppingToken);
            }
        }

        private async Task RunDailyChecksAsync(CancellationToken ct)
        {
            _logger.LogInformation("PromotionScheduler: running daily promotion checks");
            try
            {
                using var scope = _services.CreateScope();
                var promotionService = scope.ServiceProvider.GetRequiredService<IPromotionService>();

                var todayVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTz);

                // 8/3 — International Women's Day
                if (todayVn.Month == 3 && todayVn.Day == 8)
                {
                    _logger.LogInformation("PromotionScheduler: triggering WomensDay promotions");
                    await promotionService.TriggerHolidayAsync("WomensDay");
                }

                // 19/11 — International Men's Day
                if (todayVn.Month == 11 && todayVn.Day == 19)
                {
                    _logger.LogInformation("PromotionScheduler: triggering MensDay promotions");
                    await promotionService.TriggerHolidayAsync("MensDay");
                }

                // Birthday: handled via login trigger for individual users (to keep scheduler lean)
                // For users who don't log in, we could add a batch here in the future
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PromotionScheduler: error during daily checks");
            }
        }
    }
}
