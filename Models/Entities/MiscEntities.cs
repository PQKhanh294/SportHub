namespace SportHub.Models.Entities
{
    public class TimeSlot
    {
        public int SlotID { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? SlotLabel { get; set; }

        public ICollection<PricingRule> PricingRules { get; set; } = new List<PricingRule>();
        public ICollection<BookingSlot> BookingSlots { get; set; } = new List<BookingSlot>();
    }

    public class PricingRule
    {
        public int PricingID { get; set; }
        
        public int CourtID { get; set; }
        public Court Court { get; set; } = null!;

        public int SlotID { get; set; }
        public TimeSlot TimeSlot { get; set; } = null!;

        public string DayType { get; set; } = string.Empty; // Weekday, Weekend, Holiday
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime ValidFrom { get; set; } = DateTime.Today;
        public DateTime? ValidTo { get; set; }
    }

    public class Review
    {
        public int ReviewID { get; set; }
        
        public int CourtID { get; set; }
        public Court Court { get; set; } = null!;

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public int BookingID { get; set; }
        public Booking Booking { get; set; } = null!;

        public byte Rating { get; set; } // 1-5
        public string? Comment { get; set; }
        public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    }

    // B3: Match dispute / report system
    public class MatchDispute
    {
        public int Id { get; set; }

        public int MatchId { get; set; }
        public Match Match { get; set; } = null!;

        public int ReporterId { get; set; }
        public User Reporter { get; set; } = null!;

        // HostNoShow | PlayerNoShow | WrongVenue | QualityIssue | PaymentDispute
        public string DisputeType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string? EvidenceUrl { get; set; }

        // Pending | UnderReview | AutoResolved | AdminResolved | Dismissed
        public string Status { get; set; } = "Pending";
        public string? AdminNote { get; set; }

        // FullRefund | PartialRefund | NoRefund | Warning
        public string? Resolution { get; set; }
        public decimal? RefundAmount { get; set; }

        // AI hỗ trợ admin: điểm tin cậy 0-100 + JSON tóm tắt {suggestedResolution, summary, keyPoints}
        public int? AiScore { get; set; }
        public string? AiSummary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public ICollection<DisputeWitnessResponse> WitnessResponses { get; set; } = new List<DisputeWitnessResponse>();
    }

    // Xác minh nhân chứng: mỗi participant Accepted (+ host) của trận bị khiếu nại nhận 1 yêu cầu phản hồi
    public class DisputeWitnessResponse
    {
        public int Id { get; set; }

        public int DisputeId { get; set; }
        public MatchDispute Dispute { get; set; } = null!;

        public int WitnessUserId { get; set; }
        public User Witness { get; set; } = null!;

        // Pending (chưa phản hồi) | Support (đồng ý với khiếu nại) | Oppose (phản đối) | Neutral
        public string Stance { get; set; } = "Pending";
        public string? Comment { get; set; }
        public string? EvidenceUrl { get; set; }

        public DateTime? RespondedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
