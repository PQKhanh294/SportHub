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
}
