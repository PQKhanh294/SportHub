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
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
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
builder.Services.AddHostedService<PendingJoinExpiryHostedService>();

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
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<SportHub.Hubs.ChatHub>("/hubs/chat");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("SportHub started successfully. Open /health to verify server status.");
});

app.Run();


