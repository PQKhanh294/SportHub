namespace SportHub.Models.ViewModels
{
    /// <summary>
    /// ViewModel hiển thị thẻ sân ngắn gọn (Nearby Courts, Search Results).
    /// Ánh xạ từ: Courts JOIN CourtVenues JOIN Sports JOIN PricingRules
    /// </summary>
    public class CourtCardViewModel
    {
        public int CourtID { get; set; }
        public int VenueID { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string SportName { get; set; } = string.Empty;
        public string? MainImageUrl { get; set; }

        // Địa chỉ — lấy từ CourtVenues (không lưu trực tiếp trong Courts → đảm bảo 3NF)
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double? DistanceKm { get; set; }           // Tính theo tọa độ người dùng

        // Giá — lấy từ PricingRules theo giờ cao điểm/thường
        public decimal MinPricePerHour { get; set; }
        public string CourtType { get; set; } = string.Empty;  // Indoor / Outdoor
        public int TotalCourts { get; set; }                   // Số sân tại Venue

        // Trạng thái khả dụng (tính theo BookingSlots còn trống)
        public AvailabilityStatus Availability { get; set; } = AvailabilityStatus.Available;

        public enum AvailabilityStatus
        {
            Available,   // Còn nhiều slot trống
            Limited,     // Còn ít (< 2 slot)
            Full         // Hết slot trong ngày
        }
    }

    /// <summary>
    /// ViewModel chi tiết sân — dùng cho trang Court Details.
    /// </summary>
    public class CourtDetailViewModel : CourtCardViewModel
    {
        public string? Description { get; set; }
        public string SurfaceType { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public string? PhoneContact { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }

        // Tiện ích Venue
        public bool HasParking { get; set; }
        public bool HasShower { get; set; }
        public bool HasLocker { get; set; }
        public bool HasFood { get; set; }

        public List<string> ImageUrls { get; set; } = new();
        public List<TimeSlotViewModel> AvailableSlots { get; set; } = new();
        public List<PricingRuleViewModel> PricingRules { get; set; } = new();
        public List<ReviewViewModel> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}
