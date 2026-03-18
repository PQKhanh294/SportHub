namespace SportHub.Models.ViewModels
{
    /// <summary>
    /// ViewModel thẻ trận đấu — dùng cho Recommended Matches trên Home Page.
    /// Ánh xạ từ: Matches JOIN Courts JOIN CourtVenues JOIN MatchParticipants JOIN Users
    /// </summary>
    public class MatchCardViewModel
    {
        public int MatchID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SportName { get; set; } = string.Empty;
        public string? CourtImageUrl { get; set; }

        // Thời gian — ánh xạ từ Matches.MatchDate + StartTime + EndTime
        public DateTime MatchDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string DateTimeDisplay => $"{MatchDate:ddd, dd/MM} lúc {StartTime:hh\\:mm}";

        // Địa điểm — lấy từ Courts → CourtVenues (không lưu trực tiếp trong Matches)
        public string? VenueName { get; set; }
        public string? District { get; set; }

        // Loại trận
        public string MatchType { get; set; } = string.Empty;     // Singles/Doubles/Mixed
        public string MatchCategory { get; set; } = string.Empty;  // Competitive/Friendly/Casual
        public string? SkillRequired { get; set; }

        // Người tham gia — ánh xạ từ MatchParticipants
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public int SlotsLeft => MaxParticipants - CurrentParticipants;
        public List<string> ParticipantAvatars { get; set; } = new();

        // Trạng thái
        public string Status { get; set; } = "Open";
        public bool IsOpen => Status == "Open" && SlotsLeft > 0;

        // Badge màu theo category
        public string BadgeCssClass => MatchCategory switch
        {
            "Competitive" => "bg-blue-600",
            "Friendly"    => "bg-emerald-500",
            "Casual"      => "bg-amber-500",
            _             => "bg-slate-400"
        };
    }

    /// <summary>
    /// ViewModel chi tiết trận đấu — dùng cho trang Match Details.
    /// </summary>
    public class MatchDetailViewModel : MatchCardViewModel
    {
        public string? Description { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string? CreatedByAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ParticipantViewModel> Participants { get; set; } = new();
        public bool CurrentUserJoined { get; set; }
        public bool CurrentUserIsOwner { get; set; }
    }

    public class ParticipantViewModel
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? TeamSide { get; set; }  // A hoặc B
        public string JoinStatus { get; set; } = string.Empty;
        public string SportName { get; set; } = string.Empty;
        public string? SkillLevel { get; set; }
    }
}
