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
        
        public string MatchType { get; set; } = string.Empty; // Singles, Doubles, Mixed
        public string? SkillRequired { get; set; }
        public byte MaxParticipants { get; set; } = 4;
        
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "Open"; // Open, Full, InProgress, Completed, Cancelled
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<MatchParticipant> Participants { get; set; } = new List<MatchParticipant>();
    }

    public class MatchParticipant
    {
        public int ParticipantID { get; set; }
        
        public int MatchID { get; set; }
        public Match Match { get; set; } = null!;

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public string? TeamSide { get; set; } // 'A', 'B'
        public string JoinStatus { get; set; } = "Pending"; // Pending, Accepted, Declined, Cancelled
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
