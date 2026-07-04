namespace SportHub.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendMatchApprovedAsync(string toEmail, string fullName, string matchTitle, string matchDate, string matchUrl);
        Task SendMatchCancelledAsync(string toEmail, string fullName, string matchTitle, string cancelReason, decimal refundAmount);
        Task SendWalletCreditedAsync(string toEmail, string fullName, decimal amount, string description);
        Task SendPromoCodeAsync(string toEmail, string fullName, string code, decimal amount, string campaignName, DateTime? expiresAt);
        Task SendMatchReminderAsync(string toEmail, string fullName, string matchTitle, string matchDate, string venue);
        Task SendPasswordResetAsync(string toEmail, string fullName, string resetUrl);
        Task SendVerificationCodeAsync(string toEmail, string fullName, string code);
        Task<(bool Success, string Detail)> SendTestAsync(string toEmail);
    }
}
