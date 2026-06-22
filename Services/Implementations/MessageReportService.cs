using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class MessageReportService : IMessageReportService
    {
        private readonly ApplicationDbContext _context;

        public MessageReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MessageReport> CreateReportAsync(int messageId, int reporterId, string? reason,
            string aiAnalysis, int aiScore, string aiRecommendation)
        {
            var report = new MessageReport
            {
                MessageID = messageId,
                ReporterID = reporterId,
                Reason = reason,
                AiAnalysis = aiAnalysis,
                AiViolationScore = aiScore,
                AiRecommendation = aiRecommendation,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.MessageReports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<List<MessageReport>> GetPendingReportsAsync(int page = 1, int pageSize = 20)
        {
            return await _context.MessageReports
                .Include(r => r.Reporter)
                .Include(r => r.Message).ThenInclude(m => m.Sender)
                .Include(r => r.Message).ThenInclude(m => m.Receiver)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.AiViolationScore)
                .ThenByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetPendingReportCountAsync()
        {
            return await _context.MessageReports.CountAsync(r => r.Status == "Pending");
        }

        public async Task<List<MessageReport>> GetProcessedReportsAsync(int page = 1, int pageSize = 20)
        {
            return await _context.MessageReports
                .Include(r => r.Reporter)
                .Include(r => r.Message).ThenInclude(m => m!.Sender)
                .Include(r => r.Message).ThenInclude(m => m!.Receiver)
                .Include(r => r.ReviewedByAdmin)
                .Where(r => r.Status != "Pending")
                .OrderByDescending(r => r.ReviewedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetProcessedReportCountAsync()
        {
            return await _context.MessageReports.CountAsync(r => r.Status != "Pending");
        }

        public async Task<MessageReport?> ReviewReportAsync(int reportId, int adminId, string status, string? adminNote)
        {
            var report = await _context.MessageReports.FindAsync(reportId);
            if (report == null) return null;
            report.Status = status;
            report.AdminNote = adminNote;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedByAdminID = adminId;
            await _context.SaveChangesAsync();
            return report;
        }
    }
}
