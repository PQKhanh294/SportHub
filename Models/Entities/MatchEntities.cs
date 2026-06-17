namespace SportHub.Models.Entities
{
    public class Match
    {
        public int MatchID { get; set; }

        public int CreatedByUserID { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public int? CourtID { get; set; }
        public Court? Court { get; set; }

        public int? BookingID { get; set; }
        public Booking? Booking { get; set; }

        public int SportID { get; set; }
        public Sport Sport { get; set; } = null!;

        public DateTime MatchDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public string MatchType { get; set; } = string.Empty;
        public string? SkillRequired { get; set; }
        public byte MaxParticipants { get; set; } = 4;

        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? CustomCourtName { get; set; }
        public string? CustomCourtAddress { get; set; }
        public decimal? CustomPriceVnd { get; set; }
        public decimal? CustomLatitude { get; set; }
        public decimal? CustomLongitude { get; set; }

        // Status: Open, Full, InProgress, Completed, Cancelled, PendingDeposit
        public string Status { get; set; } = "PendingDeposit";
        public bool RequiresApproval { get; set; } = false;

        // Phí platform
        public string DepositStatus { get; set; } = "NotPaid";     // NotPaid, Paid
        public string RemainingFeeStatus { get; set; } = "NotDue"; // NotDue, Notified, Paid

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<MatchParticipant> Participants { get; set; } = new List<MatchParticipant>();
        public ICollection<MatchPayment> MatchPayments { get; set; } = new List<MatchPayment>();
    }

    public class MatchParticipant
    {
        public int ParticipantID { get; set; }

        public int MatchID { get; set; }
        public Match Match { get; set; } = null!;

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public string? TeamSide { get; set; }
        // Pending, Approved, Accepted, Declined, Cancelled
        // Approved = host đã duyệt, chờ player thanh toán 5K
        // Accepted = đã thanh toán xong
        public string JoinStatus { get; set; } = "Pending";

        // Phí 5K của player
        public string? PlayerFeeStatus { get; set; }    // AwaitingPayment, Paid, Expired
        public DateTime? PlayerFeeDeadline { get; set; }
        public string? PlayerFeeReceiptUrl { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class MatchInteraction
    {
        public int InteractionID { get; set; }

        public int MatchID { get; set; }
        public Match Match { get; set; } = null!;

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public string Action { get; set; } = "View"; // View, Skip, Request
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MatchPayment
    {
        public int MatchPaymentID { get; set; }

        public int MatchID { get; set; }
        public Match Match { get; set; } = null!;

        public int PayerUserID { get; set; }
        public User Payer { get; set; } = null!;

        // HostDeposit, HostRemaining, PlayerFee
        public string PaymentType { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        // Pending, Confirmed, Expired, Refunded
        public string Status { get; set; } = "Pending";

        public string? ReceiptUrl { get; set; }
        public string? TransactionRef { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }
}
