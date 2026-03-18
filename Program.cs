using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SportHub.Data;
using SportHub.Services.Interfaces;
using SportHub.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ Razor Pages
builder.Services.AddRazorPages();

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

// Đăng ký Business Logic Services (Giai đoạn 5)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IBookingService, BookingService>();

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
}

// Cấu hình Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Mã HSTS cho HTTPS bảo mật
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép load file tĩnh từ wwwroot (CSS, JS)

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages
app.MapRazorPages();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("SportHub started successfully. Open /health to verify server status.");
});

app.Run();

static bool TableExists(ApplicationDbContext dbContext, string tableName)
{
    using var connection = dbContext.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        connection.Open();
    }

    using var command = connection.CreateCommand();
    command.CommandText = $"SELECT CASE WHEN OBJECT_ID('dbo.{tableName}', 'U') IS NULL THEN 0 ELSE 1 END";
    var result = command.ExecuteScalar();
    return Convert.ToInt32(result) == 1;
}
