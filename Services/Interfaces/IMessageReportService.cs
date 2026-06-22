using SportHub.Models.Entities;

namespace SportHub.Services.Interfaces
{
    public interface IMessageReportService
    {
        Task<MessageReport> CreateReportAsync(int messageId, int reporterId, string? reason,
            string aiAnalysis, int aiScore, string aiRecommendation);
        Task<List<MessageReport>> GetPendingReportsAsync(int page = 1, int pageSize = 20);
        Task<int> GetPendingReportCountAsync();
        Task<List<MessageReport>> GetProcessedReportsAsync(int page = 1, int pageSize = 20);
        Task<int> GetProcessedReportCountAsync();
        Task<MessageReport?> ReviewReportAsync(int reportId, int adminId, string status, string? adminNote);
    }
}
