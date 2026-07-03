using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SportHub.Data;
using SportHub.Models.Entities;
using SportHub.Services.Interfaces;

namespace SportHub.Services.Implementations
{
    public class DisputeService : IDisputeService
    {
        private static readonly TimeSpan WitnessDeadline = TimeSpan.FromHours(48);

        private readonly ApplicationDbContext _context;
        private readonly IWalletService _wallet;
        private readonly INotificationService _notification;
        private readonly IAiChatService _aiChat;
        private readonly ILogger<DisputeService> _logger;

        public DisputeService(
            ApplicationDbContext context,
            IWalletService wallet,
            INotificationService notification,
            IAiChatService aiChat,
            ILogger<DisputeService> logger)
        {
            _context = context;
            _wallet = wallet;
            _notification = notification;
            _aiChat = aiChat;
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

            await CreateWitnessRequestsAsync(dispute);
            return dispute;
        }

        // Gửi yêu cầu xác minh cho mọi participant Accepted + host của trận (trừ người khiếu nại)
        private async Task CreateWitnessRequestsAsync(MatchDispute dispute)
        {
            var match = await _context.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.MatchID == dispute.MatchId);
            if (match == null) return;

            var witnessIds = match.Participants
                .Where(p => p.JoinStatus == "Accepted")
                .Select(p => p.UserID)
                .Append(match.CreatedByUserID)
                .Where(id => id != dispute.ReporterId)
                .Distinct()
                .ToList();

            if (witnessIds.Count == 0) return;

            foreach (var witnessId in witnessIds)
            {
                _context.DisputeWitnessResponses.Add(new DisputeWitnessResponse
                {
                    DisputeId = dispute.Id,
                    WitnessUserId = witnessId,
                    Stance = "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            var matchTitle = match.Title ?? match.MatchType;
            foreach (var witnessId in witnessIds)
            {
                await _notification.CreateAsync(
                    witnessId,
                    "System",
                    "Yêu cầu xác minh khiếu nại",
                    $"Trận \"{matchTitle}\" có khiếu nại. Bạn là người trong trận — hãy xác nhận thông tin trong 48 giờ để admin xử lý công bằng.",
                    $"/Disputes/Respond?id={dispute.Id}");
            }

            _logger.LogInformation("Dispute {DisputeId}: created {Count} witness request(s).", dispute.Id, witnessIds.Count);
        }

        public Task<List<MatchDispute>> GetPendingDisputesAsync() =>
            _context.MatchDisputes
                .Include(d => d.Match)
                .Include(d => d.Reporter)
                .Include(d => d.WitnessResponses).ThenInclude(w => w.Witness)
                .Where(d => d.Status == "Pending" || d.Status == "UnderReview")
                .OrderBy(d => d.CreatedAt)
                .ToListAsync();

        public Task<List<MatchDispute>> GetAllDisputesAsync(int page, int pageSize) =>
            _context.MatchDisputes
                .Include(d => d.Match)
                .Include(d => d.Reporter)
                .Include(d => d.WitnessResponses).ThenInclude(w => w.Witness)
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
                .Include(d => d.WitnessResponses).ThenInclude(w => w.Witness)
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
                    $"Bạn được hoàn {refundAmount.Value:N0} xu từ khiếu nại #{disputeId}.",
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

        // ─── Witness ──────────────────────────────────────────────────────────

        public Task<DisputeWitnessResponse?> GetWitnessRequestAsync(int disputeId, int userId) =>
            _context.DisputeWitnessResponses
                .Include(w => w.Dispute).ThenInclude(d => d.Match)
                .FirstOrDefaultAsync(w => w.DisputeId == disputeId && w.WitnessUserId == userId);

        public Task<List<DisputeWitnessResponse>> GetWitnessResponsesAsync(int disputeId) =>
            _context.DisputeWitnessResponses
                .Include(w => w.Witness)
                .Where(w => w.DisputeId == disputeId)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();

        public async Task<bool> SubmitWitnessResponseAsync(int disputeId, int userId, string stance, string? comment, string? evidenceUrl)
        {
            if (stance is not ("Support" or "Oppose" or "Neutral")) return false;

            var witness = await _context.DisputeWitnessResponses
                .FirstOrDefaultAsync(w => w.DisputeId == disputeId && w.WitnessUserId == userId);
            if (witness == null || witness.RespondedAt != null) return false;

            witness.Stance = stance;
            witness.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
            witness.EvidenceUrl = evidenceUrl;
            witness.RespondedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Đủ 100% nhân chứng phản hồi → chạy AI ngay, không chờ deadline
            var anyPending = await _context.DisputeWitnessResponses
                .AnyAsync(w => w.DisputeId == disputeId && w.RespondedAt == null);
            if (!anyPending)
                await RunAiAnalysisAsync(disputeId);

            return true;
        }

        // ─── AI analysis ──────────────────────────────────────────────────────

        public async Task RunAiAnalysisAsync(int disputeId)
        {
            var dispute = await _context.MatchDisputes
                .Include(d => d.Match)
                .Include(d => d.WitnessResponses).ThenInclude(w => w.Witness)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute == null) return;

            var responded = dispute.WitnessResponses.Where(w => w.RespondedAt != null).ToList();
            var support = responded.Count(w => w.Stance == "Support");
            var oppose = responded.Count(w => w.Stance == "Oppose");
            var neutral = responded.Count(w => w.Stance == "Neutral");
            var total = dispute.WitnessResponses.Count;

            var witnessLines = responded.Count == 0
                ? "(chưa có nhân chứng nào phản hồi)"
                : string.Join("\n", responded.Select(w =>
                    $"- {w.Stance}{(string.IsNullOrEmpty(w.Comment) ? "" : $": {w.Comment}")}{(string.IsNullOrEmpty(w.EvidenceUrl) ? "" : " (có ảnh kèm)")}"));

            var evidenceCount = CountEvidenceUrls(dispute.EvidenceUrl);

            var systemPrompt =
                "Bạn là trợ lý phân tích khiếu nại cho nền tảng thể thao SportHub. " +
                "Phân tích khiếu nại và phản hồi nhân chứng, trả về DUY NHẤT một JSON object (không markdown, không giải thích thêm) theo schema: " +
                "{\"credibilityScore\": <0-100>, \"suggestedResolution\": \"FullRefund|PartialRefund|NoRefund|Dismiss\", \"summary\": \"<tóm tắt 2-3 câu tiếng Việt>\", \"keyPoints\": [\"<điểm chính>\"]}. " +
                "credibilityScore cao = khiếu nại đáng tin. Nhân chứng Support tăng độ tin, Oppose giảm. Ít phản hồi → điểm trung lập 40-60.";

            var userMessage =
                $"Loại khiếu nại: {dispute.DisputeType}\n" +
                $"Mô tả từ người khiếu nại: {dispute.Description}\n" +
                $"Số ảnh bằng chứng: {evidenceCount}\n" +
                $"Nhân chứng: {responded.Count}/{total} đã phản hồi (Support: {support}, Oppose: {oppose}, Neutral: {neutral})\n" +
                $"Chi tiết phản hồi:\n{witnessLines}";

            var raw = await _aiChat.ChatAsync(systemPrompt, new List<AiChatHistoryItem>(), userMessage);

            try
            {
                var jsonStart = raw.IndexOf('{');
                var jsonEnd = raw.LastIndexOf('}');
                if (jsonStart < 0 || jsonEnd <= jsonStart)
                    throw new JsonException("No JSON object in AI response");

                var json = raw[jsonStart..(jsonEnd + 1)];
                using var doc = JsonDocument.Parse(json);
                var score = doc.RootElement.GetProperty("credibilityScore").GetInt32();

                dispute.AiScore = Math.Clamp(score, 0, 100);
                dispute.AiSummary = json;
                if (dispute.Status == "Pending") dispute.Status = "UnderReview";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dispute {DisputeId}: AI analysis done, score={Score}.", disputeId, dispute.AiScore);
            }
            catch (Exception ex)
            {
                // AI là phụ trợ — lỗi phân tích không chặn xử lý tay của admin
                _logger.LogWarning(ex, "Dispute {DisputeId}: AI analysis failed. Raw: {Raw}", disputeId,
                    raw.Length > 300 ? raw[..300] : raw);
            }
        }

        // Hosted service gọi định kỳ: chạy AI cho dispute quá hạn nhân chứng 48h mà chưa phân tích
        public async Task ProcessPendingAiAnalysisAsync(CancellationToken cancellationToken = default)
        {
            var deadline = DateTime.UtcNow - WitnessDeadline;
            var dueDisputeIds = await _context.MatchDisputes
                .Where(d => (d.Status == "Pending" || d.Status == "UnderReview")
                    && d.AiScore == null
                    && d.CreatedAt <= deadline)
                .Select(d => d.Id)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var id in dueDisputeIds)
                await RunAiAnalysisAsync(id);
        }

        private static int CountEvidenceUrls(string? evidenceUrl)
        {
            if (string.IsNullOrWhiteSpace(evidenceUrl)) return 0;
            if (!evidenceUrl.TrimStart().StartsWith('[')) return 1;
            try
            {
                return JsonSerializer.Deserialize<List<string>>(evidenceUrl)?.Count ?? 0;
            }
            catch
            {
                return 1;
            }
        }
    }
}
