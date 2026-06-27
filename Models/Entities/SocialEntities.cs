using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportHub.Models.Entities
{
    /// <summary>
    /// Bảng lưu thông tin kết bạn giữa 2 người dùng.
    /// </summary>
    public class Friendship
    {
        [Key]
        public int FriendshipID { get; set; }

        public int SenderID { get; set; }
        public User Sender { get; set; } = null!;

        public int ReceiverID { get; set; }
        public User Receiver { get; set; } = null!;

        // "Pending", "Accepted", "Declined"
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bảng lưu tin nhắn chat 1-1 giữa 2 người dùng.
    /// </summary>
    public class ChatMessage
    {
        [Key]
        public int MessageID { get; set; }

        public int SenderID { get; set; }
        public User Sender { get; set; } = null!;

        public int ReceiverID { get; set; }
        public User Receiver { get; set; } = null!;

        public string Content { get; set; } = string.Empty;

        // "Text" | "Image" | "MatchCard"
        public string MessageType { get; set; } = "Text";

        public string? ImageUrl { get; set; }

        public string? MatchCardJson { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        // Reply / Pin / Delete extensions
        public int? ReplyToMessageID { get; set; }
        public ChatMessage? ReplyToMessage { get; set; }
        public bool IsPinned { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MessageReaction
    {
        public int ReactionID { get; set; }
        public int MessageID { get; set; }
        public ChatMessage Message { get; set; } = null!;
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        // Heart | Laugh | Wow | Sad | Angry | Like
        public string ReactionType { get; set; } = "Heart";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class MessageReport
    {
        public int ReportID { get; set; }
        public int MessageID { get; set; }
        public ChatMessage Message { get; set; } = null!;
        public int ReporterID { get; set; }
        public User Reporter { get; set; } = null!;
        public string? Reason { get; set; }
        public string AiAnalysis { get; set; } = "";
        public int AiViolationScore { get; set; } = 0;
        // dismiss | warn | ban_1day | ban_7days | ban_30days | ban_permanent
        public string AiRecommendation { get; set; } = "";
        // Pending | Reviewed | Dismissed
        public string Status { get; set; } = "Pending";
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByAdminID { get; set; }
        public User? ReviewedByAdmin { get; set; }
    }

    public class UserBan
    {
        public int BanID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;
        public int BannedByAdminID { get; set; }
        public User BannedByAdmin { get; set; } = null!;
        public int? ReportID { get; set; }
        public MessageReport? Report { get; set; }
        // 1day | 7days | 30days | permanent
        public string BanType { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime StartAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ChatBookingProposal
    {
        [Key]
        public int ProposalID { get; set; }

        public int MatchID { get; set; }
        public Match Match { get; set; } = null!;

        public int SenderID { get; set; }
        public User Sender { get; set; } = null!;

        public int ReceiverID { get; set; }
        public User Receiver { get; set; } = null!;

        public int? CourtID { get; set; }
        public Court? Court { get; set; }

        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string SplitMode { get; set; } = "Equal"; // Equal, HostPays
        public string Status { get; set; } = "Waiting"; // Waiting, Accepted, Rejected, Booked
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
