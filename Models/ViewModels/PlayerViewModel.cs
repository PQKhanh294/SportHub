namespace SportHub.Models.ViewModels
{
    /// <summary>
    /// ViewModel người chơi gợi ý — dùng cho Suggested Players trên Home Page.
    /// Ánh xạ từ: Users JOIN UserRoles
    /// </summary>
    public class PlayerCardViewModel
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? SkillLevel { get; set; }     // Beginner/Intermediate/Advanced/Professional
        public string FavoriteSport { get; set; } = string.Empty; // Lấy từ môn chơi nhiều nhất
        public double AverageRating { get; set; }
        public int TotalMatches { get; set; }
        public bool AlreadyConnected { get; set; }
    }

    /// <summary>
    /// ViewModel hồ sơ đầy đủ — dùng cho trang Profile.
    /// </summary>
    public class UserProfileViewModel
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? SkillLevel { get; set; }
        public bool IsVerified { get; set; }

        // Thống kê
        public int TotalBookings { get; set; }
        public int TotalMatches { get; set; }
        public double AverageRating { get; set; }

        public List<string> Roles { get; set; } = new();
    }
}
