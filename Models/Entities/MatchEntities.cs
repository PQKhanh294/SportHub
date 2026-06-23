namespace SportHub.Models.Entities
{
    public class MatchReview
    {
        public int MatchReviewID { get; set; }

        public int MatchID { get; set; }
        public Match Match { get; set; } = null!;

        public int ReviewerUserID { get; set; }
        public User ReviewerUser { get; set; } = null!;

        // For PlayerToMatch: reviewed = host. For HostToPlayer: reviewed = specific player
        public int ReviewedUserID { get; set; }
        public User ReviewedUser { get; set; } = null!;

        // "PlayerToMatch" or "HostToPlayer"
        public string ReviewType { get; set; } = string.Empty;

        // Player → Match criteria (1-5, used when ReviewType = "PlayerToMatch")
        public byte? ScoreOrganization { get; set; }
        public byte? ScoreEquipment { get; set; }
        public byte? ScoreAtmosphere { get; set; }
        public byte? ScoreHost { get; set; }
        public byte? ScoreValueForMoney { get; set; }

        // Host → Player criteria (1-5, used when ReviewType = "HostToPlayer")
        public byte? ScorePunctuality { get; set; }
        public byte? ScoreSportsmanship { get; set; }
        public byte? ScoreSkillAccuracy { get; set; }

        public string? Comment { get; set; }
        public bool IsVisible { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal AverageScore
        {
            get
            {
                IEnumerable<byte?> raw = ReviewType == "PlayerToMatch"
                    ? new[] { ScoreOrganization, ScoreEquipment, ScoreAtmosphere, ScoreHost, ScoreValueForMoney }
                    : new[] { ScorePunctuality, ScoreSportsmanship, ScoreSkillAccuracy };
                var scores = raw.Where(s => s.HasValue).Select(s => (decimal)s!.Value).ToList();
                return scores.Count == 0 ? 0m : scores.Average();
            }
        }
    }


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

        public bool IsSplitFee { get; set; } = false;

        public bool IsRecurring { get; set; } = false;
        public string? RecurringDays { get; set; }   // "1,3,5" (0=CN,1=T2,...,6=T7)
        public DateTime? RecurringUntil { get; set; }
        public int? ParentMatchId { get; set; }
        public Match? ParentMatch { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<MatchParticipant> Participants { get; set; } = new List<MatchParticipant>();
        public ICollection<MatchPayment> MatchPayments { get; set; } = new List<MatchPayment>();
        public ICollection<MatchReview> Reviews { get; set; } = new List<MatchReview>();
        public ICollection<Match> RecurringChildren { get; set; } = new List<Match>();
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
        public DateTime? ReminderSentAt { get; set; }
    }

    public class WalletTransaction
    {
        public int WalletTransactionID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        public decimal Amount { get; set; } // positive = credit, negative = debit
        public string Type { get; set; } = string.Empty; // Refund, Deduction, AdminCredit, TopUp, MatchPayment
        public string Description { get; set; } = string.Empty;
        public int? RelatedMatchID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WalletTopUpRequest
    {
        public int WalletTopUpID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        public decimal Amount { get; set; }
        public string TransactionRef { get; set; } = string.Empty; // TOPUP-{userId}-{yyyyMMddHHmmss}
        public string Status { get; set; } = "Pending";            // Pending | Confirmed | Expired
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public decimal? ActualAmount { get; set; }                 // số tiền thực nhận từ SePay
    }
}
