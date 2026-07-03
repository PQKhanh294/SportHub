using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IDisputeService
    {
        Task<MatchDispute> SubmitDisputeAsync(int matchId, int reporterId, string disputeType, string description, string? evidenceUrl = null);
        Task<List<MatchDispute>> GetPendingDisputesAsync();
        Task<List<MatchDispute>> GetAllDisputesAsync(int page, int pageSize);
        Task<int> GetPendingCountAsync();
        Task<MatchDispute?> GetDisputeByIdAsync(int id);
        Task ResolveDisputeAsync(int disputeId, int adminId, string adminNote, string resolution, decimal? refundAmount);
        Task DismissDisputeAsync(int disputeId, int adminId, string adminNote);

        // Xác minh nhân chứng
        Task<DisputeWitnessResponse?> GetWitnessRequestAsync(int disputeId, int userId);
        Task<List<DisputeWitnessResponse>> GetWitnessResponsesAsync(int disputeId);
        Task<bool> SubmitWitnessResponseAsync(int disputeId, int userId, string stance, string? comment, string? evidenceUrl);

        // AI hỗ trợ admin
        Task RunAiAnalysisAsync(int disputeId);
        Task ProcessPendingAiAnalysisAsync(CancellationToken cancellationToken = default);
    }
}
