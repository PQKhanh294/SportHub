using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SportHub.Data;
using SportHub.Hubs;
using SportHub.Services.Interfaces;
using SportHub.Services.Implementations;
using SportHub.Services;
using SportHub.Middleware;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ Razor Pages và Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddSignalR();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
    })
    .AddCookie("ExternalCookie", options =>
    {
        options.Cookie.Name = "SportHub.ExternalAuth";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddGoogle(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    })
    .AddFacebook(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";
    });

// Cấu hình Entity Framework Core với chuỗi kết nối (Giai đoạn 1)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IGeocodingService, GeocodingService>();

// Đăng ký Business Logic Services (Giai đoạn 5)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IBadgeService, BadgeService>();
builder.Services.AddScoped<SportHub.Services.Interfaces.INotificationService, SportHub.Services.Implementations.NotificationService>();
builder.Services.AddScoped<SportHub.Services.Interfaces.IMatchPaymentService, SportHub.Services.Implementations.MatchPaymentService>();
builder.Services.AddScoped<SportHub.Services.Interfaces.IMatchReviewService, SportHub.Services.Implementations.MatchReviewService>();
builder.Services.AddScoped<SportHub.Services.Interfaces.IWalletService, SportHub.Services.Implementations.WalletService>();
builder.Services.AddScoped<SportHub.Services.Interfaces.IAiChatService, SportHub.Services.Implementations.AiChatService>();
builder.Services.AddScoped<SportHub.Services.Interfaces.IMessageReportService, SportHub.Services.Implementations.MessageReportService>();
builder.Services.AddScoped<SportHub.Services.Interfaces.IUserBanService, SportHub.Services.Implementations.UserBanService>();
builder.Services.AddScoped<SportHub.Services.Implementations.ChatModerationService>();
builder.Services.AddScoped<SportHub.Services.Interfaces.ISubscriptionService, SportHub.Services.Implementations.SubscriptionService>();
builder.Services.AddScoped<SportHub.Services.Interfaces.IPromotionService, SportHub.Services.Implementations.PromotionService>();
builder.Services.AddHostedService<PendingJoinExpiryHostedService>();
builder.Services.AddHostedService<SportHub.Services.PromotionSchedulerService>();
builder.Services.AddControllers();

var app = builder.Build();

if (!builder.Configuration.GetValue<bool>("SkipDatabaseMigration"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Cấu hình Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Mã HSTS cho HTTPS bảo mật
    app.UseHsts();
}

// Cấu hình Localization
var supportedCultures = new[] { "vi-VN", "en-US" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép load file tĩnh từ wwwroot (CSS, JS)
app.UseMiddleware<UiLocalizationMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages
app.MapRazorPages();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<SportHub.Hubs.ChatHub>("/hubs/chat");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/geo/search", async (string q, int limit, IGeocodingService geo) =>
{
    var results = await geo.SearchAsync(q, Math.Clamp(limit, 1, 10));
    return Results.Json(results.Select(r => new { lat = r.Lat, lon = r.Lon, display_name = r.DisplayName, source = r.Source }));
});

app.MapGet("/geo/reverse", async (double lat, double lon, IHttpClientFactory clientFactory) =>
{
    try
    {
        var http = clientFactory.CreateClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "SportHub/1.0");
        var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var url = $"https://nominatim.openstreetmap.org/reverse?lat={latStr}&lon={lonStr}&format=json&accept-language=vi";
        var res = await http.GetStringAsync(url);
        var doc = System.Text.Json.JsonDocument.Parse(res);
        var displayName = doc.RootElement.TryGetProperty("display_name", out var dn) ? dn.GetString() : null;
        return Results.Json(new { address = displayName });
    }
    catch
    {
        return Results.Json(new { address = (string?)null });
    }
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("SportHub started successfully. Open /health to verify server status.");
});

app.Run();


