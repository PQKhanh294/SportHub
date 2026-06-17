using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public record ExpiredPlayerFee(int UserId, int MatchId, string MatchTitle, int ParticipantId);

    public class MatchPaymentAdminItem
    {
        public int PaymentId { get; set; }
        public int MatchId { get; set; }
        public string MatchTitle { get; set; } = string.Empty;
        public int PayerUserId { get; set; }
        public string PayerName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? ReceiptUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool HasReceipt => !string.IsNullOrWhiteSpace(ReceiptUrl);
    }

    public class AdminRevenueStats
    {
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public int TotalConfirmedPayments { get; set; }
        public int PendingPaymentsCount { get; set; }
    }

    public class TransactionHistoryItem
    {
        public int PaymentId { get; set; }
        public int MatchId { get; set; }
        public string MatchTitle { get; set; } = string.Empty;
        public int PayerUserId { get; set; }
        public string PayerName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? ReceiptUrl { get; set; }
        public DateTime ConfirmedAt { get; set; }
        public string TypeLabel => PaymentType switch
        {
            "HostDeposit"   => "Đặt cọc host",
            "PlayerFee"     => "Phí player",
            "HostRemaining" => "Phí còn lại",
            _ => PaymentType
        };
    }

    public interface IMatchPaymentService
    {
        decimal CalculateHostDeposit(int maxParticipants);
        Task<MatchPayment> CreateHostDepositAsync(int matchId, int hostUserId);
        Task<MatchPayment> CreatePlayerFeeAsync(int matchId, int playerUserId);
        Task<MatchPayment> CreateHostRemainingAsync(int matchId, int hostUserId);
        Task<MatchPayment?> GetActivePaymentAsync(int matchId, int userId, string paymentType);
        Task<bool> SubmitHostDepositReceiptAsync(int matchId, int userId, string receiptUrl);
        Task<bool> SubmitPlayerFeeReceiptAsync(int matchId, int userId, string receiptUrl);
        Task<bool> SubmitHostRemainingReceiptAsync(int matchId, int userId, string receiptUrl);
        Task<bool> ConfirmPaymentAsync(int paymentId);
        Task<bool> RejectPaymentAsync(int paymentId);
        Task<List<MatchPaymentAdminItem>> GetPendingPaymentsAsync();
        Task<AdminRevenueStats> GetRevenueStatsAsync();
        Task<List<TransactionHistoryItem>> GetTransactionHistoryAsync();
        Task<List<ExpiredPlayerFee>> ExpirePlayerFeesAsync(CancellationToken ct = default);
        Task<List<(int MatchId, int HostUserId, string MatchTitle, decimal RemainingAmount)>> NotifyRemainingFeeAsync(CancellationToken ct = default);
    }
}
