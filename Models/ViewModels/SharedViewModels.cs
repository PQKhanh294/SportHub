namespace SportHub.Models.ViewModels
{
    public class TimeSlotViewModel
    {
        public int SlotID { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string SlotLabel { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public decimal Price { get; set; }
    }

    public class PricingRuleViewModel
    {
        public string DayType { get; set; } = string.Empty;  // Weekday / Weekend / Holiday
        public string SlotLabel { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string FormattedPrice => UnitPrice.ToString("N0") + "đ/giờ";
    }

    public class ReviewViewModel
    {
        public int ReviewID { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string? ReviewerAvatarUrl { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewedAt { get; set; }
    }
}
