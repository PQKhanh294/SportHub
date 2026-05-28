using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SportHub.Data;
using SportHub.Hubs;
using SportHub.Services.Interfaces;
using SportHub.Services.Implementations;
using SportHub.Services;
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
builder.Services.AddScoped<SportHub.Services.Interfaces.INotificationService, SportHub.Services.Implementations.NotificationService>();
builder.Services.AddHostedService<PendingJoinExpiryHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasMigrations = dbContext.Database.GetMigrations().Any();

    if (hasMigrations)
    {
        dbContext.Database.Migrate();
    }
    else
    {
        dbContext.Database.EnsureCreated();

        // Trường hợp DB đã tồn tại nhưng chưa có schema ứng dụng (ví dụ chỉ có bảng hệ thống)
        if (!TableExists(dbContext, "Users"))
        {
            var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
            databaseCreator.CreateTables();
        }
    }

    EnsureUserLocationColumns(dbContext);
    EnsureMatchRequiresApproval(dbContext);
}

static void EnsureMatchRequiresApproval(ApplicationDbContext dbContext)
{
    if (!TableExists(dbContext, "Matches"))
        return;

    dbContext.Database.ExecuteSqlRaw(
        "UPDATE dbo.Matches SET RequiresApproval = 1 WHERE RequiresApproval = 0");
}

static void EnsureUserLocationColumns(ApplicationDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH('dbo.Users', 'DefaultAddress') IS NULL
                ALTER TABLE dbo.Users ADD DefaultAddress NVARCHAR(300) NULL;
            IF COL_LENGTH('dbo.Users', 'DefaultLatitude') IS NULL
                ALTER TABLE dbo.Users ADD DefaultLatitude DECIMAL(10,8) NULL;
            IF COL_LENGTH('dbo.Users', 'DefaultLongitude') IS NULL
                ALTER TABLE dbo.Users ADD DefaultLongitude DECIMAL(11,8) NULL;
        END
        """);
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

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages
app.MapRazorPages();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("SportHub started successfully. Open /health to verify server status.");
});

app.Run();

static bool TableExists(ApplicationDbContext dbContext, string tableName)
{
    if (string.IsNullOrWhiteSpace(tableName) || !System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[A-Za-z_][A-Za-z0-9_]*$"))
        return false;

    dbContext.Database.OpenConnection();
    try
    {
        using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT CASE WHEN OBJECT_ID(@tableName, N'U') IS NULL THEN 0 ELSE 1 END";
        var param = command.CreateParameter();
        param.ParameterName = "@tableName";
        param.Value = $"dbo.{tableName}";
        command.Parameters.Add(param);
        var result = command.ExecuteScalar();
        return Convert.ToInt32(result) == 1;
    }
    finally
    {
        dbContext.Database.CloseConnection();
    }
}

