namespace SportHub.Models.Entities
{
    /// <summary>
    /// Lưu thông tin kỹ năng chuyên môn của người chơi theo từng môn thể thao.
    /// Hiện tại hỗ trợ Cầu lông (Badminton).
    /// </summary>
    public class UserSportProfile
    {
        public int ProfileID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public int SportID { get; set; }
        public Sport Sport { get; set; } = null!;

        // Cầu lông: vị trí sân
        // "BackCourt" = Phông cao | "FrontCourt" = Nữa sân | "AllRound" = Đa năng
        public string? CourtPosition { get; set; }

        // Phong cách: "Aggressive" = Tấn công | "Defensive" = Phòng thủ | "Balanced" = Cân bằng
        public string? PlayStyle { get; set; }

        // Kỹ thuật mạnh: "Smash" | "Drop" = Drop shot | "Drive" | "AllRound" = Đa năng
        public string? StrokeStrength { get; set; }

        // Tự đánh giá trình độ: 1-5 sao
        public int? SelfRatedLevel { get; set; }

        // Số năm kinh nghiệm
        public int? ExperienceYears { get; set; }

        public string? SkillLevel { get; set; }

        public string? Notes { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Thông báo trong ứng dụng cho người dùng.
    /// </summary>
    public class Notification
    {
        public int NotificationID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        // "MatchJoin" | "MatchApprove" | "MatchReject" | "MatchJoinExpired" | "BookingConfirmed" | "BookingCancelled" | "System"
        public string Type { get; set; } = "System";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
