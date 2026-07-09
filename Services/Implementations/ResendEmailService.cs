using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using SportHub.Common;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class ResendEmailService : IEmailService
    {
        private readonly HttpClient _http;
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;
        private readonly string _from;
        private readonly string _baseUrl;
        private readonly ILogger<ResendEmailService> _logger;

        private const string BrandColor = "#50A5B1";

        public ResendEmailService(IHttpClientFactory factory, ApplicationDbContext context, IConfiguration config, ILogger<ResendEmailService> logger)
        {
            _http    = factory.CreateClient();
            _context = context;
            _apiKey  = config["Resend:ApiKey"] ?? "";
            _from    = config["Resend:FromAddress"] ?? "SportHub <no-reply@sporthub.vn>";
            _baseUrl = (config["App:BaseUrl"] ?? "https://sporthub-dn.id.vn/").TrimEnd('/');
            _logger  = logger;
        }

        // ---- Layout & building blocks dùng chung cho mọi email ----

        private string Layout(string headline, string bodyHtml, string? ctaLabel = null, string? ctaUrl = null)
        {
            var logoUrl = $"{_baseUrl}/images/logo-email.png";
            var ctaHtml = !string.IsNullOrWhiteSpace(ctaLabel) && !string.IsNullOrWhiteSpace(ctaUrl)
                ? Button(ctaLabel!, ctaUrl!) : "";

            return $@"
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background-color:#f1f5f9;padding:32px 16px;font-family:Segoe UI,Helvetica,Arial,sans-serif;'>
  <tr><td align='center'>
    <table role='presentation' width='480' cellpadding='0' cellspacing='0' style='max-width:480px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 10px rgba(15,23,42,0.08);'>
      <tr>
        <td style='background-color:{BrandColor};padding:28px 32px;text-align:center;'>
          <img src='{logoUrl}' alt='SportHub' width='44' height='44' style='display:block;margin:0 auto 10px;border-radius:10px;' />
          <span style='color:#ffffff;font-size:19px;font-weight:700;letter-spacing:0.3px;'>SportHub</span>
        </td>
      </tr>
      <tr>
        <td style='padding:32px;'>
          <h1 style='margin:0 0 16px;font-size:19px;line-height:1.4;color:#0f172a;'>{headline}</h1>
          <div style='font-size:14px;line-height:1.7;color:#334155;'>{bodyHtml}</div>
          {ctaHtml}
        </td>
      </tr>
      <tr>
        <td style='padding:18px 32px;background-color:#f8fafc;border-top:1px solid #e2e8f0;text-align:center;'>
          <p style='margin:0;font-size:12px;color:#94a3b8;'>SportHub — Nền tảng ghép trận thể thao</p>
          <p style='margin:4px 0 0;font-size:11px;color:#cbd5e1;'>© {DateTime.UtcNow.Year} SportHub. Email tự động, vui lòng không trả lời trực tiếp.</p>
        </td>
      </tr>
    </table>
  </td></tr>
</table>";
        }

        private static string Callout(string type, string html)
        {
            var (bg, border, text) = type switch
            {
                "danger" => ("#fef2f2", "#ef4444", "#b91c1c"),
                "warning" => ("#fffbeb", "#f59e0b", "#b45309"),
                "success" => ("#f0fdf4", "#22c55e", "#15803d"),
                _ => ("#eff6ff", "#3b82f6", "#1d4ed8"), // info
            };
            return $"<div style='background-color:{bg};border-left:4px solid {border};padding:12px 16px;border-radius:6px;margin:16px 0;color:{text};font-size:14px;line-height:1.6;'>{html}</div>";
        }

        private static string Button(string label, string url) =>
            $"<div style='text-align:center;margin:24px 0 4px;'><a href='{url}' style='display:inline-block;background-color:{BrandColor};color:#ffffff;font-weight:700;font-size:14px;padding:12px 28px;border-radius:8px;text-decoration:none;'>{label}</a></div>";

        // ---- Gửi & ghi log ----

        private async Task SendAsync(string to, string subject, string html, [CallerMemberName] string templateType = "")
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

            bool success;
            string? errorDetail = null;
            try
            {
                var resp = await _http.SendAsync(req);
                success = resp.IsSuccessStatusCode;
                if (!success)
                {
                    errorDetail = await resp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Resend API returned {Status} for email to {To}: {Body}", resp.StatusCode, to, errorDetail);
                }
            }
            catch (Exception ex)
            {
                success = false;
                errorDetail = ex.Message;
                _logger.LogError(ex, "Failed to send email via Resend to {To}", to);
            }

            _context.EmailLogs.Add(new EmailLog
            {
                ToEmail = to,
                Subject = subject,
                TemplateType = templateType,
                Success = success,
                ErrorDetail = errorDetail,
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        // Gửi thử + trả nguyên văn phản hồi Resend — dùng cho admin chẩn đoán (domain chưa verify, key sai...)
        public async Task<(bool Success, string Detail)> SendTestAsync(string to)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return (false, "Resend:ApiKey chưa được cấu hình trong appsettings.json");

            var html = Layout("Email test từ SportHub 📨",
                $"<p>Đây là email test được gửi lúc <strong>{VietnamTime.Now:HH:mm:ss dd/MM/yyyy}</strong> (giờ VN).</p>" +
                Callout("info", "Nếu bạn nhận được email này với đầy đủ logo và định dạng, cấu hình Resend + domain đã hoạt động tốt."));

            var payload = JsonSerializer.Serialize(new { from = _from, to, subject = "[SportHub] Test email", html });
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            bool success;
            string detail;
            try
            {
                var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                success = resp.IsSuccessStatusCode;
                detail = $"{(int)resp.StatusCode} {resp.StatusCode}: {body}";
            }
            catch (Exception ex)
            {
                success = false;
                detail = ex.Message;
            }

            _context.EmailLogs.Add(new EmailLog
            {
                ToEmail = to,
                Subject = "[SportHub] Test email",
                TemplateType = nameof(SendTestAsync),
                Success = success,
                ErrorDetail = success ? null : detail,
                SentAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return (success, detail);
        }

        public Task SendMatchApprovedAsync(string toEmail, string fullName, string matchTitle, string matchDate, string matchUrl) =>
            SendAsync(toEmail, $"[SportHub] Bạn đã được duyệt tham gia trận: {matchTitle}",
                Layout("Yêu cầu tham gia đã được duyệt! 🎉",
                    $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                       <p>Bạn đã được <strong>chấp thuận</strong> tham gia trận đấu:</p>
                       {Callout("success", $"<strong>{matchTitle}</strong><br/>Thời gian: {matchDate}")}
                       <p>Hãy chuẩn bị sẵn sàng và đến đúng giờ nhé!</p>",
                    "Xem chi tiết trận →", matchUrl));

        public Task SendMatchCancelledAsync(string toEmail, string fullName, string matchTitle, string cancelReason, decimal refundAmount) =>
            SendAsync(toEmail, $"[SportHub] Trận đấu đã bị hủy: {matchTitle}",
                Layout("Trận đấu đã bị hủy",
                    $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                       <p>Trận <strong>{matchTitle}</strong> đã bị <strong>hủy bởi chủ trận</strong>.</p>
                       {Callout("danger", $"<strong>Lý do:</strong> {cancelReason}" +
                            (refundAmount > 0 ? $"<br/><strong>Hoàn tiền:</strong> {refundAmount:N0}đ đã về ví SportHub" : ""))}"));

        public Task SendWalletCreditedAsync(string toEmail, string fullName, decimal amount, string description) =>
            SendAsync(toEmail, $"[SportHub] Ví được cộng +{amount:N0}đ",
                Layout("Ví của bạn vừa được cộng tiền 💰",
                    $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                       <p>Ví SportHub của bạn vừa nhận:</p>
                       {Callout("success", $"<strong style='font-size:18px;'>+{amount:N0}đ</strong><br/>{description}")}",
                    "Xem ví của tôi →", $"{_baseUrl}/Wallet"));

        public Task SendPromoCodeAsync(string toEmail, string fullName, string code, decimal amount, string campaignName, DateTime? expiresAt) =>
            SendAsync(toEmail, $"[SportHub] Mã khuyến mãi của bạn: {code}",
                Layout("Bạn vừa nhận được mã khuyến mãi 🎁",
                    $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                       <p>Bạn nhận được mã khuyến mãi từ chiến dịch <em>{campaignName}</em>:</p>
                       {Callout("info", $"<strong style='font-size:20px;letter-spacing:2px;'>{code}</strong><br/>" +
                            $"Giá trị: <strong>+{amount:N0}đ</strong>{(expiresAt.HasValue ? $" · HSD {expiresAt.Value.ToVietnamTime():dd/MM/yyyy}" : "")}")}",
                    "Dùng mã ngay →", $"{_baseUrl}/Wallet"));

        public Task SendMatchReminderAsync(string toEmail, string fullName, string matchTitle, string matchDate, string venue) =>
            SendAsync(toEmail, $"[SportHub] Nhắc nhở: Trận {matchTitle} sắp diễn ra",
                Layout("Trận đấu của bạn sắp diễn ra ⏰",
                    $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                       <p>Trận <strong>{matchTitle}</strong> của bạn sẽ diễn ra vào:</p>
                       {Callout("info", $"<strong>{matchDate}</strong><br/>Địa điểm: {venue}")}"));

        public Task SendPasswordResetAsync(string toEmail, string fullName, string resetUrl) =>
            SendAsync(toEmail, "[SportHub] Đặt lại mật khẩu",
                Layout("Yêu cầu đặt lại mật khẩu",
                    $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                       <p>Bạn (hoặc ai đó) vừa yêu cầu đặt lại mật khẩu cho tài khoản SportHub này.</p>
                       {Callout("warning", "Liên kết có hiệu lực trong <strong>1 giờ</strong>. Nếu bạn không yêu cầu, hãy bỏ qua email này.")}",
                    "Đặt lại mật khẩu →", resetUrl));

        public Task SendVerificationCodeAsync(string toEmail, string fullName, string code) =>
            SendAsync(toEmail, "[SportHub] Mã xác thực email của bạn",
                Layout("Mã xác thực của bạn",
                    $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                       <p>Mã xác thực email của bạn là:</p>
                       <p style='text-align:center;font-size:30px;font-weight:800;letter-spacing:8px;color:{BrandColor};margin:20px 0;'>{code}</p>
                       {Callout("warning", "Mã có hiệu lực trong <strong>10 phút</strong>. Nếu bạn không yêu cầu, hãy bỏ qua email này.")}"));

        // ---- Email mới ----

        public Task SendDailyDigestAsync(string toEmail, string fullName, List<(string SenderName, int UnreadCount)> unreadChats,
            List<(string Title, string Message)> unreadNotifications, string? aiSummary, string dashboardUrl)
        {
            var sb = new StringBuilder();
            sb.Append($"<p>Xin chào <strong>{fullName}</strong>,</p>");
            sb.Append("<p>Đây là tổng hợp những gì bạn có thể đã bỏ lỡ hôm qua trên SportHub:</p>");

            if (!string.IsNullOrWhiteSpace(aiSummary))
                sb.Append(Callout("info", $"💡 {aiSummary}"));

            if (unreadChats.Count > 0)
            {
                var rows = string.Join("", unreadChats.Select(c => $"<li><strong>{c.SenderName}</strong> — {c.UnreadCount} tin nhắn chưa đọc</li>"));
                sb.Append($"<p style='margin-bottom:6px;font-weight:700;color:#0f172a;'>💬 Tin nhắn chưa đọc</p><ul style='margin:0 0 16px;padding-left:20px;'>{rows}</ul>");
            }

            if (unreadNotifications.Count > 0)
            {
                var rows = string.Join("", unreadNotifications.Select(n => $"<li><strong>{n.Title}</strong> — {n.Message}</li>"));
                sb.Append($"<p style='margin-bottom:6px;font-weight:700;color:#0f172a;'>🔔 Thông báo chưa xem</p><ul style='margin:0;padding-left:20px;'>{rows}</ul>");
            }

            return SendAsync(toEmail, "[SportHub] Tổng hợp hoạt động hôm qua của bạn",
                Layout("Bạn có hoạt động chưa xem 📋", sb.ToString(), "Xem ngay →", dashboardUrl));
        }

        public Task SendPaymentReminderAsync(string toEmail, string fullName, string matchTitle, string paymentType,
            decimal amount, DateTime expiresAt, string payUrl, bool urgent = false)
        {
            var paymentLabel = paymentType switch
            {
                "HostRemaining" => "phí dịch vụ còn lại",
                "PlayerFee" => "phí tham gia trận",
                "HostDeposit" => "tiền đặt cọc",
                _ => "khoản thanh toán"
            };
            var calloutType = urgent ? "danger" : "warning";
            var headline = urgent ? "Khẩn: sắp hết hạn thanh toán! ⚠️" : "Nhắc nhở thanh toán";

            return SendAsync(toEmail, $"[SportHub] {(urgent ? "Khẩn cấp — " : "")}Nhắc nhở thanh toán: {matchTitle}",
                Layout(headline,
                    $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                       <p>Trận <strong>{matchTitle}</strong> của bạn còn khoản {paymentLabel} chưa hoàn tất:</p>
                       {Callout(calloutType, $"<strong style='font-size:18px;'>{amount:N0}đ</strong><br/>Hạn chót: <strong>{expiresAt.ToVietnamTime():HH:mm dd/MM/yyyy}</strong>")}",
                    "Thanh toán ngay →", payUrl));
        }

        public Task SendMatchJoinRequestAsync(string toEmail, string fullName, string matchTitle, string playerName, string matchUrl) =>
            SendAsync(toEmail, $"[SportHub] {playerName} muốn tham gia trận của bạn",
                Layout("Có người muốn tham gia trận đấu 🙋",
                    $@"<p>Xin chào <strong>{fullName}</strong>,</p>
                       <p><strong>{playerName}</strong> vừa gửi yêu cầu tham gia trận:</p>
                       {Callout("info", $"<strong>{matchTitle}</strong>")}
                       <p>Vui lòng duyệt sớm để người chơi không phải chờ lâu.</p>",
                    "Duyệt yêu cầu →", matchUrl));
    }
}
