using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IWalletService
    {
        Task<decimal> GetBalanceAsync(int userId);
        Task CreditAsync(int userId, decimal amount, string description, int? matchId = null, string type = "AdminCredit");
        Task<bool> DeductAsync(int userId, decimal amount, string description, int? matchId = null, string type = "Deduction");
        Task<List<WalletTransaction>> GetHistoryAsync(int userId, int limit = 20);

        // Top-up via bank transfer
        Task<WalletTopUpRequest> CreateTopUpRequestAsync(int userId, decimal amount);
        Task<bool> ConfirmTopUpAsync(string transactionRef, decimal actualAmount);
        Task<WalletTopUpRequest?> GetPendingTopUpAsync(int userId);
        Task<List<WalletTopUpRequest>> GetTopUpHistoryAsync(int userId, int limit = 20);

        // Pay match fee directly from wallet (skips admin approval)
        Task<bool> PayMatchFeeFromWalletAsync(int userId, int matchId, string paymentType);
    }
}
