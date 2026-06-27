using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class DisputeService : IDisputeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWalletService _wallet;
        private readonly INotificationService _notification;
        private readonly ILogger<DisputeService> _logger;

        public DisputeService(
            ApplicationDbContext context,
            IWalletService wallet,
            INotificationService notification,
            ILogger<DisputeService> logger)
        {
            _context = context;
            _wallet = wallet;
            _notification = notification;
            _logger = logger;
        }

        public async Task<MatchDispute> SubmitDisputeAsync(int matchId, int reporterId, string disputeType, string description, string? evidenceUrl = null)
        {
            var dispute = new MatchDispute
            {
                MatchId = matchId,
                ReporterId = reporterId,
                DisputeType = disputeType,
                Description = description,
                EvidenceUrl = evidenceUrl,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.MatchDisputes.Add(dispute);
            await _context.SaveChangesAsync();
            return dispute;
        }

        public Task<List<MatchDispute>> GetPendingDisputesAsync() =>
            _context.MatchDisputes
                .Include(d => d.Match)
                .Include(d => d.Reporter)
                .Where(d => d.Status == "Pending" || d.Status == "UnderReview")
                .OrderBy(d => d.CreatedAt)
                .ToListAsync();

        public Task<List<MatchDispute>> GetAllDisputesAsync(int page, int pageSize) =>
            _context.MatchDisputes
                .Include(d => d.Match)
                .Include(d => d.Reporter)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public Task<int> GetPendingCountAsync() =>
            _context.MatchDisputes.CountAsync(d => d.Status == "Pending" || d.Status == "UnderReview");

        public Task<MatchDispute?> GetDisputeByIdAsync(int id) =>
            _context.MatchDisputes
                .Include(d => d.Match)
                .Include(d => d.Reporter)
                .FirstOrDefaultAsync(d => d.Id == id);

        public async Task ResolveDisputeAsync(int disputeId, int adminId, string adminNote, string resolution, decimal? refundAmount)
        {
            var dispute = await _context.MatchDisputes
                .Include(d => d.Match)
                .Include(d => d.Reporter)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute == null) return;

            dispute.Status = "AdminResolved";
            dispute.AdminNote = adminNote;
            dispute.Resolution = resolution;
            dispute.RefundAmount = refundAmount;
            dispute.ResolvedAt = DateTime.UtcNow;

            if (refundAmount.HasValue && refundAmount > 0 && resolution != "NoRefund")
            {
                await _wallet.CreditAsync(
                    dispute.ReporterId,
                    refundAmount.Value,
                    $"Hoàn tiền khiếu nại #{disputeId}");

                await _notification.CreateAsync(
                    dispute.ReporterId,
                    "System",
                    "Khiếu nại được giải quyết",
                    $"Bạn được hoàn {refundAmount.Value:N0}đ từ khiếu nại #{disputeId}.",
                    $"/Matchmaking/Details?id={dispute.MatchId}");
            }
            else
            {
                await _notification.CreateAsync(
                    dispute.ReporterId,
                    "System",
                    "Kết quả khiếu nại",
                    $"Khiếu nại #{disputeId} đã được xem xét: {adminNote}",
                    $"/Matchmaking/Details?id={dispute.MatchId}");
            }

            await _context.SaveChangesAsync();
        }

        public async Task DismissDisputeAsync(int disputeId, int adminId, string adminNote)
        {
            var dispute = await _context.MatchDisputes.FindAsync(disputeId);
            if (dispute == null) return;

            dispute.Status = "Dismissed";
            dispute.AdminNote = adminNote;
            dispute.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _notification.CreateAsync(
                dispute.ReporterId,
                "System",
                "Khiếu nại bị bác bỏ",
                $"Khiếu nại #{disputeId} đã bị bác bỏ. Lý do: {adminNote}",
                $"/Matchmaking/Details?id={dispute.MatchId}");
        }
    }
}
