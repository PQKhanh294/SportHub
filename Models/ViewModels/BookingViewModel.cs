namespace SportHub.Models.ViewModels
{
    /// <summary>
    /// ViewModel tóm tắt đặt sân — dùng cho Booking & Payment Summary.
    /// Ánh xạ từ: Bookings JOIN BookingSlots JOIN Courts JOIN CourtVenues JOIN TimeSlots
    /// </summary>
    public class BookingViewModel
    {
        public int BookingID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime BookedAt { get; set; }

        // Thông tin sân — chỉ dùng ID để JOIN, không lưu tên vào Bookings
        public int CourtID { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string? CourtImageUrl { get; set; }
        public string SportName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Khung giờ — ánh xạ từ BookingSlots
        public List<string> SlotLabels { get; set; } = new();
        public string TimeDisplay => SlotLabels.Count > 0
            ? $"{SlotLabels.First().Split('-')[0].Trim()} - {SlotLabels.Last().Split('-')[1].Trim()}"
            : string.Empty;

        // Giá
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }

        // Thanh toán — ánh xạ từ Payments
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public string? TransactionRef { get; set; }

        // Badge màu theo Status
        public string StatusBadgeCss => Status switch
        {
            "Confirmed"  => "bg-emerald-100 text-emerald-700",
            "Pending"    => "bg-amber-100 text-amber-700",
            "Cancelled"  => "bg-red-100 text-red-700",
            "Completed"  => "bg-blue-100 text-blue-700",
            _            => "bg-slate-100 text-slate-600"
        };
    }

    /// <summary>
    /// Input model cho form đặt sân.
    /// </summary>
    public class CreateBookingInputModel
    {
        public int CourtID { get; set; }
        public DateTime BookingDate { get; set; }
        public List<int> SelectedSlotIDs { get; set; } = new();
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "VNPay";
    }
}
