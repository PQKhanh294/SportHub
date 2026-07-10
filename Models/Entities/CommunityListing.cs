using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SportHub.Models.Entities
{
    // Tin tuyển vãng lai cào từ hội nhóm Facebook (qua Chrome Extension) — chỉ mang tính tham khảo,
    // dẫn traffic sang bài gốc, KHÔNG dùng luồng cọc/thanh toán/participant như Match.
    [Index(nameof(SourceUrl), IsUnique = true)]
    public class CommunityListing
    {
        public int CommunityListingID { get; set; }

        public int? SportID { get; set; }
        public Sport? Sport { get; set; }

        [Required]
        public string SourceUrl { get; set; } = string.Empty;
        public string? SourceAuthorName { get; set; }
        public string RawText { get; set; } = string.Empty;

        public int SubmittedByUserID { get; set; }
        public User SubmittedByUser { get; set; } = null!;

        // Trường AI bóc tách — nullable vì AI có thể không lấy được hết
        public string? Title { get; set; }
        public string? VenueName { get; set; }
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateOnly? MatchDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? SkillRequired { get; set; }
        public decimal? CostMaleVnd { get; set; }
        public decimal? CostFemaleVnd { get; set; }
        public int? SlotsNeeded { get; set; }

        // Pending, Parsed, Failed
        public string ParseStatus { get; set; } = "Pending";
        public string? ParsedJsonRaw { get; set; }

        // Active, Expired, Hidden
        public string Status { get; set; } = "Active";
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
