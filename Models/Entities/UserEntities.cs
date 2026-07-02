namespace SportHub.Models.Entities
{
    public class User
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ZaloContact { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; } // 'M', 'F', 'O'
        public string? SkillLevel { get; set; }
        public string? FavoriteSport { get; set; }
        public string? DefaultAddress { get; set; }
        public decimal? DefaultLatitude { get; set; }
        public decimal? DefaultLongitude { get; set; }
        public string? GoogleId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public bool IsBanned { get; set; } = false;
        public DateTime? BanEndAt { get; set; }
        public decimal WalletBalance { get; set; } = 0m;
        public int LoginCount { get; set; } = 0;
        public bool NotifyByEmail { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public CourtOwner? CourtOwnerProfile { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<UserVoucher> Vouchers { get; set; } = new List<UserVoucher>();
    }

    public class Role
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public int RoleID { get; set; }
        public Role Role { get; set; } = null!;
    }
}
