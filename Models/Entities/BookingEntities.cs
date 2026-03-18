namespace SportHub.Models.Entities
{
    public class Booking
    {
        public int BookingID { get; set; }
        
        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public int CourtID { get; set; }
        public Court Court { get; set; } = null!;

        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal FinalAmount { get; set; } = 0;
        
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed, NoShow
        public string? Note { get; set; }
        public string? CancelReason { get; set; }
        
        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BookingSlot> BookingSlots { get; set; } = new List<BookingSlot>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class BookingSlot
    {
        public int BookingSlotID { get; set; }
        
        public int BookingID { get; set; }
        public Booking Booking { get; set; } = null!;

        public int SlotID { get; set; }
        public TimeSlot TimeSlot { get; set; } = null!;

        public decimal UnitPrice { get; set; }
    }

    public class Payment
    {
        public int PaymentID { get; set; }
        
        public int BookingID { get; set; }
        public Booking Booking { get; set; } = null!;

        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // VNPay, MoMo, BankTransfer, Cash
        public string? TransactionRef { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed, Refunded
        public string PaymentType { get; set; } = "Payment"; // Payment, Refund
        
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
