using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class ResendEmailService : IEmailService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _from;
        private readonly ILogger<ResendEmailService> _logger;

        public ResendEmailService(IHttpClientFactory factory, IConfiguration config, ILogger<ResendEmailService> logger)
        {
            _http   = factory.CreateClient();
            _apiKey = config["Resend:ApiKey"] ?? "";
            _from   = config["Resend:FromAddress"] ?? "SportHub <no-reply@sporthub.vn>";
            _logger = logger;
        }

        private async Task SendAsync(string to, string subject, string html)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Resend API key not configured — email skipped (to: {To})", to);
                return;
            }

            var payload = JsonSerializer.Serialize(new { from = _from, to, subject, html });
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            try
            {
                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Resend API returned {Status} for email to {To}: {Body}", resp.StatusCode, to, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via Resend to {To}", to);
            }
        }

        // Gửi thử + trả nguyên văn phản hồi Resend — dùng cho admin chẩn đoán (domain chưa verify, key sai...)
        public async Task<(bool Success, string Detail)> SendTestAsync(string to)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return (false, "Resend:ApiKey chưa được cấu hình trong appsettings.json");

            var payload = JsonSerializer.Serialize(new
            {
                from = _from,
                to,
                subject = "[SportHub] Test email",
                html = $"<p>Email test từ SportHub lúc {DateTime.UtcNow.AddHours(7):HH:mm:ss dd/MM/yyyy} (giờ VN).</p>"
            });
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            try
            {
                var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                return (resp.IsSuccessStatusCode, $"{(int)resp.StatusCode} {resp.StatusCode}: {body}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public Task SendMatchApprovedAsync(string toEmail, string fullName, string matchTitle, string matchDate, string matchUrl) =>
            SendAsync(toEmail, $"[SportHub] Bạn đã được duyệt tham gia trận: {matchTitle}",
                $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                   <p>Bạn đã được <strong>chấp thuận</strong> tham gia trận <strong>{matchTitle}</strong> vào ngày <strong>{matchDate}</strong>.</p>
                   <p><a href='{matchUrl}' style='color:#6366f1;font-weight:bold;'>Xem chi tiết trận →</a></p>
                   <hr/><p style='color:#888;font-size:12px;'>SportHub — Nền tảng ghép trận thể thao</p>");

        public Task SendMatchCancelledAsync(string toEmail, string fullName, string matchTitle, string cancelReason, decimal refundAmount) =>
            SendAsync(toEmail, $"[SportHub] Trận đấu đã bị hủy: {matchTitle}",
                $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                   <p>Trận <strong>{matchTitle}</strong> đã bị <strong>hủy bởi chủ trận</strong>.</p>
                   <p><strong>Lý do:</strong> {cancelReason}</p>
                   {(refundAmount > 0 ? $"<p>Bạn sẽ được <strong>hoàn {refundAmount:N0}đ</strong> về ví SportHub.</p>" : "")}
                   <hr/><p style='color:#888;font-size:12px;'>SportHub</p>");

        public Task SendWalletCreditedAsync(string toEmail, string fullName, decimal amount, string description) =>
            SendAsync(toEmail, $"[SportHub] Ví được cộng +{amount:N0}đ",
                $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                   <p>Ví SportHub của bạn vừa nhận <strong>+{amount:N0}đ</strong>.</p>
                   <p><em>{description}</em></p>
                   <hr/><p style='color:#888;font-size:12px;'>SportHub</p>");

        public Task SendPromoCodeAsync(string toEmail, string fullName, string code, decimal amount, string campaignName, DateTime? expiresAt) =>
            SendAsync(toEmail, $"[SportHub] Mã khuyến mãi của bạn: {code}",
                $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                   <p>Bạn nhận được mã khuyến mãi <strong>{code}</strong> từ chiến dịch <em>{campaignName}</em>.</p>
                   <p>Giá trị: <strong>+{amount:N0}đ</strong>{(expiresAt.HasValue ? $" · HSD {expiresAt.Value.ToLocalTime():dd/MM/yyyy}" : "")}</p>
                   <hr/><p style='color:#888;font-size:12px;'>SportHub</p>");

        public Task SendMatchReminderAsync(string toEmail, string fullName, string matchTitle, string matchDate, string venue) =>
            SendAsync(toEmail, $"[SportHub] Nhắc nhở: Trận {matchTitle} sắp diễn ra",
                $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                   <p>Trận <strong>{matchTitle}</strong> của bạn sẽ diễn ra vào <strong>{matchDate}</strong>.</p>
                   <p><strong>Địa điểm:</strong> {venue}</p>
                   <hr/><p style='color:#888;font-size:12px;'>SportHub</p>");

        public Task SendPasswordResetAsync(string toEmail, string fullName, string resetUrl) =>
            SendAsync(toEmail, "[SportHub] Đặt lại mật khẩu",
                $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                   <p>Bạn (hoặc ai đó) vừa yêu cầu đặt lại mật khẩu cho tài khoản SportHub này.</p>
                   <p><a href='{resetUrl}' style='color:#6366f1;font-weight:bold;'>Đặt lại mật khẩu →</a></p>
                   <p style='color:#888;font-size:12px;'>Liên kết có hiệu lực trong 1 giờ. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
                   <hr/><p style='color:#888;font-size:12px;'>SportHub — Nền tảng ghép trận thể thao</p>");
    }
}
