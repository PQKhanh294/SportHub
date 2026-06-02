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

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
