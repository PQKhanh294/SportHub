using Microsoft.EntityFrameworkCore;
using SportHub.Models.Entities;

namespace SportHub.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Nhóm 1: User & Roles
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;

        // Nhóm 2: Court & Venues
        public DbSet<Sport> Sports { get; set; } = null!;
        public DbSet<CourtOwner> CourtOwners { get; set; } = null!;
        public DbSet<CourtVenue> CourtVenues { get; set; } = null!;
        public DbSet<Court> Courts { get; set; } = null!;
        public DbSet<CourtImage> CourtImages { get; set; } = null!;

        // Nhóm 3: Booking & Payment
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<BookingSlot> BookingSlots { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        // Nhóm 4: Match & MatchParticipants
        public DbSet<Match> Matches { get; set; } = null!;
        public DbSet<MatchParticipant> MatchParticipants { get; set; } = null!;
        public DbSet<MatchInteraction> MatchInteractions { get; set; } = null!;

        // Nhóm 5: Misc
        public DbSet<TimeSlot> TimeSlots { get; set; } = null!;
        public DbSet<PricingRule> PricingRules { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;

        // Nhóm 6: New Features
        public DbSet<UserSportProfile> UserSportProfiles { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<UserBadge> UserBadges { get; set; } = null!;
        
        // Nhóm 7: Social (Friends & Chat)
        public DbSet<Friendship> Friendships { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<ChatBookingProposal> ChatBookingProposals { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table mapping (đồng bộ tên bảng với script SQL)
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles");
            modelBuilder.Entity<Sport>().ToTable("Sports");
            modelBuilder.Entity<CourtOwner>().ToTable("CourtOwners");
            modelBuilder.Entity<CourtVenue>().ToTable("CourtVenues");
            modelBuilder.Entity<Court>().ToTable("Courts");
            modelBuilder.Entity<CourtImage>().ToTable("CourtImages");
            modelBuilder.Entity<TimeSlot>().ToTable("TimeSlots");
            modelBuilder.Entity<PricingRule>().ToTable("PricingRules");
            modelBuilder.Entity<Booking>().ToTable("Bookings");
            modelBuilder.Entity<BookingSlot>().ToTable("BookingSlots");
            modelBuilder.Entity<Payment>().ToTable("Payments");
            modelBuilder.Entity<Match>().ToTable("Matches");
            modelBuilder.Entity<MatchParticipant>().ToTable("MatchParticipants");
            modelBuilder.Entity<MatchInteraction>().ToTable("MatchInteractions");
            modelBuilder.Entity<Review>().ToTable("Reviews");
            modelBuilder.Entity<UserSportProfile>().ToTable("UserSportProfiles");
            modelBuilder.Entity<Notification>().ToTable("Notifications");
            modelBuilder.Entity<UserBadge>().ToTable("UserBadges");
            modelBuilder.Entity<ChatBookingProposal>().ToTable("ChatBookingProposals");

            // Primary keys (khai báo tường minh để tránh lỗi nhận diện key theo convention)
            modelBuilder.Entity<User>().HasKey(u => u.UserID);
            modelBuilder.Entity<Role>().HasKey(r => r.RoleID);
            modelBuilder.Entity<Sport>().HasKey(s => s.SportID);
            modelBuilder.Entity<CourtOwner>().HasKey(co => co.OwnerID);
            modelBuilder.Entity<CourtVenue>().HasKey(cv => cv.VenueID);
            modelBuilder.Entity<Court>().HasKey(c => c.CourtID);
            modelBuilder.Entity<CourtImage>().HasKey(ci => ci.ImageID);
            modelBuilder.Entity<Booking>().HasKey(b => b.BookingID);
            modelBuilder.Entity<BookingSlot>().HasKey(bs => bs.BookingSlotID);
            modelBuilder.Entity<Payment>().HasKey(p => p.PaymentID);
            modelBuilder.Entity<Match>().HasKey(m => m.MatchID);
            modelBuilder.Entity<MatchParticipant>().HasKey(mp => mp.ParticipantID);
            modelBuilder.Entity<MatchInteraction>().HasKey(mi => mi.InteractionID);
            modelBuilder.Entity<TimeSlot>().HasKey(ts => ts.SlotID);
            modelBuilder.Entity<PricingRule>().HasKey(pr => pr.PricingID);
            modelBuilder.Entity<Review>().HasKey(r => r.ReviewID);
            modelBuilder.Entity<UserSportProfile>().HasKey(usp => usp.ProfileID);
            modelBuilder.Entity<Notification>().HasKey(n => n.NotificationID);
            modelBuilder.Entity<UserBadge>().HasKey(b => b.BadgeID);
            modelBuilder.Entity<ChatBookingProposal>().HasKey(p => p.ProposalID);

            // Composite Key (N:N) - UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserID, ur.RoleID });

            // Cấu hình Unique constraints cho 3NF
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<TimeSlot>()
                .HasIndex(ts => new { ts.StartTime, ts.EndTime })
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<Sport>()
                .HasIndex(s => s.SportName)
                .IsUnique();
                
            modelBuilder.Entity<CourtOwner>()
                .HasIndex(co => co.UserID)
                .IsUnique();

            modelBuilder.Entity<PricingRule>()
                .HasIndex(p => new { p.CourtID, p.SlotID, p.DayType, p.ValidFrom })
                .IsUnique();

            modelBuilder.Entity<BookingSlot>()
                .HasIndex(bs => new { bs.BookingID, bs.SlotID })
                .IsUnique();

            modelBuilder.Entity<MatchParticipant>()
                .HasIndex(mp => new { mp.MatchID, mp.UserID })
                .IsUnique();

            modelBuilder.Entity<MatchInteraction>()
                .HasIndex(mi => new { mi.MatchID, mi.UserID, mi.Action });

            modelBuilder.Entity<UserBadge>()
                .HasIndex(b => new { b.UserID, b.BadgeKey })
                .IsUnique();

            modelBuilder.Entity<ChatBookingProposal>()
                .HasIndex(p => new { p.MatchID, p.SenderID, p.ReceiverID, p.Status });

            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.BookingID, r.UserID })
                .IsUnique();

            // Index hỗ trợ truy vấn như script SQL
            modelBuilder.Entity<CourtVenue>()
                .HasIndex(v => new { v.City, v.District });

            modelBuilder.Entity<Court>()
                .HasIndex(c => new { c.SportID, c.VenueID });

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.UserID, b.BookingDate });

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.CourtID, b.BookingDate, b.Status });

            modelBuilder.Entity<Match>()
                .HasIndex(m => new { m.Status, m.MatchDate, m.SportID });

            // Kiểu dữ liệu SQL
            modelBuilder.Entity<User>().Property(u => u.DateOfBirth).HasColumnType("date");
            modelBuilder.Entity<CourtVenue>().Property(v => v.OpenTime).HasColumnType("time");
            modelBuilder.Entity<CourtVenue>().Property(v => v.CloseTime).HasColumnType("time");
            modelBuilder.Entity<TimeSlot>().Property(ts => ts.StartTime).HasColumnType("time");
            modelBuilder.Entity<TimeSlot>().Property(ts => ts.EndTime).HasColumnType("time");
            modelBuilder.Entity<Booking>().Property(b => b.BookingDate).HasColumnType("date");
            modelBuilder.Entity<Match>().Property(m => m.MatchDate).HasColumnType("date");
            modelBuilder.Entity<Match>().Property(m => m.StartTime).HasColumnType("time");
            modelBuilder.Entity<Match>().Property(m => m.EndTime).HasColumnType("time");
            modelBuilder.Entity<ChatBookingProposal>().Property(p => p.BookingDate).HasColumnType("date");
            modelBuilder.Entity<ChatBookingProposal>().Property(p => p.StartTime).HasColumnType("time");
            modelBuilder.Entity<ChatBookingProposal>().Property(p => p.EndTime).HasColumnType("time");

            modelBuilder.Entity<CourtVenue>().Property(v => v.Latitude).HasPrecision(10, 8);
            modelBuilder.Entity<CourtVenue>().Property(v => v.Longitude).HasPrecision(11, 8);
            modelBuilder.Entity<User>().Property(u => u.DefaultLatitude).HasPrecision(10, 8);
            modelBuilder.Entity<User>().Property(u => u.DefaultLongitude).HasPrecision(11, 8);
            modelBuilder.Entity<Match>().Property(m => m.CustomLatitude).HasPrecision(10, 8);
            modelBuilder.Entity<Match>().Property(m => m.CustomLongitude).HasPrecision(11, 8);
            modelBuilder.Entity<Match>().Property(m => m.CustomPriceVnd).HasPrecision(12, 2);
            modelBuilder.Entity<PricingRule>().Property(p => p.UnitPrice).HasPrecision(12, 2);
            modelBuilder.Entity<Booking>().Property(b => b.TotalAmount).HasPrecision(12, 2);
            modelBuilder.Entity<Booking>().Property(b => b.DiscountAmount).HasPrecision(12, 2);
            modelBuilder.Entity<Booking>().Property(b => b.FinalAmount).HasPrecision(12, 2);
            modelBuilder.Entity<BookingSlot>().Property(bs => bs.UnitPrice).HasPrecision(12, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(12, 2);
            modelBuilder.Entity<ChatBookingProposal>().Property(p => p.EstimatedCost).HasPrecision(12, 2);

            // Check constraints quan trọng theo script SQL
            modelBuilder.Entity<User>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_Users_Gender", "Gender IN ('M','F','O')");
                // Relaxed skill level constraint to support new levels
            });

            modelBuilder.Entity<PricingRule>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_PricingRules_DayType", "DayType IN ('Weekday','Weekend','Holiday')");
            });

            modelBuilder.Entity<Booking>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_Bookings_Status", "Status IN ('Pending','Confirmed','Cancelled','Completed','NoShow')");
            });

            modelBuilder.Entity<Payment>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_Payments_PaymentMethod", "PaymentMethod IN ('VNPay','MoMo','ZaloPay','BankTransfer','Cash')");
                t.HasCheckConstraint("CK_Payments_Status", "Status IN ('Pending','Success','Failed','Refunded')");
                t.HasCheckConstraint("CK_Payments_PaymentType", "PaymentType IN ('Payment','Refund')");
            });

            modelBuilder.Entity<Match>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_Matches_MatchType", "MatchType IN ('Singles','Doubles','Mixed')");
                t.HasCheckConstraint("CK_Matches_Status", "Status IN ('Open','Full','InProgress','Completed','Cancelled')");
            });

            modelBuilder.Entity<MatchParticipant>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_MatchParticipants_TeamSide", "TeamSide IS NULL OR TeamSide IN ('A','B')");
                t.HasCheckConstraint("CK_MatchParticipants_JoinStatus", "JoinStatus IN ('Pending','Accepted','Declined','Cancelled')");
            });

            modelBuilder.Entity<MatchInteraction>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_MatchInteractions_Action", "Action IN ('View','Skip','Request')");
            });

            modelBuilder.Entity<ChatBookingProposal>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_ChatBookingProposals_Status", "Status IN ('Waiting','Accepted','Rejected','Booked')");
                t.HasCheckConstraint("CK_ChatBookingProposals_SplitMode", "SplitMode IN ('Equal','HostPays')");
            });

            modelBuilder.Entity<UserBadge>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_UserBadges_Level", "Level IN ('Bronze','Silver','Gold')");
            });

            modelBuilder.Entity<Review>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_Reviews_Rating", "Rating BETWEEN 1 AND 5");
            });

            // Cấu hình quan hệ để tránh cycle cascade trigger
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourtOwner>()
                .HasOne(co => co.User)
                .WithOne(u => u.CourtOwnerProfile)
                .HasForeignKey<CourtOwner>(co => co.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CourtVenue>()
                .HasOne(v => v.Owner)
                .WithMany(o => o.Venues)
                .HasForeignKey(v => v.OwnerID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Court>()
                .HasOne(c => c.Venue)
                .WithMany(v => v.Courts)
                .HasForeignKey(c => c.VenueID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Court>()
                .HasOne(c => c.Sport)
                .WithMany(s => s.Courts)
                .HasForeignKey(c => c.SportID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CourtImage>()
                .HasOne(ci => ci.Court)
                .WithMany(c => c.Images)
                .HasForeignKey(ci => ci.CourtID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Sender)
                .WithMany()
                .HasForeignKey(f => f.SenderID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Receiver)
                .WithMany()
                .HasForeignKey(f => f.ReceiverID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Court)
                .WithMany()
                .HasForeignKey(b => b.CourtID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BookingSlot>()
                .HasOne(bs => bs.Booking)
                .WithMany(b => b.BookingSlots)
                .HasForeignKey(bs => bs.BookingID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingSlot>()
                .HasOne(bs => bs.TimeSlot)
                .WithMany(ts => ts.BookingSlots)
                .HasForeignKey(bs => bs.SlotID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Court)
                .WithMany()
                .HasForeignKey(m => m.CourtID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Booking)
                .WithMany()
                .HasForeignKey(m => m.BookingID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Sport)
                .WithMany()
                .HasForeignKey(m => m.SportID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PricingRule>()
                .HasOne(pr => pr.TimeSlot)
                .WithMany(ts => ts.PricingRules)
                .HasForeignKey(pr => pr.SlotID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PricingRule>()
                .HasOne(pr => pr.Court)
                .WithMany(c => c.PricingRules)
                .HasForeignKey(pr => pr.CourtID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MatchParticipant>()
                .HasOne(mp => mp.Match)
                .WithMany(m => m.Participants)
                .HasForeignKey(mp => mp.MatchID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchParticipant>()
                .HasOne(mp => mp.User)
                .WithMany()
                .HasForeignKey(mp => mp.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MatchInteraction>()
                .HasOne(mi => mi.Match)
                .WithMany()
                .HasForeignKey(mi => mi.MatchID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchInteraction>()
                .HasOne(mi => mi.User)
                .WithMany()
                .HasForeignKey(mi => mi.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Court)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CourtID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Booking)
                .WithMany()
                .HasForeignKey(r => r.BookingID)
                .OnDelete(DeleteBehavior.NoAction);
                
            modelBuilder.Entity<Match>()
                .HasOne(m => m.CreatedByUser)
                .WithMany()
                .HasForeignKey(m => m.CreatedByUserID)
                .OnDelete(DeleteBehavior.NoAction);

            // UserSportProfile relations
            modelBuilder.Entity<UserSportProfile>()
                .HasIndex(usp => new { usp.UserID, usp.SportID })
                .IsUnique();

            modelBuilder.Entity<UserSportProfile>()
                .HasOne(usp => usp.User)
                .WithMany()
                .HasForeignKey(usp => usp.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserSportProfile>()
                .HasOne(usp => usp.Sport)
                .WithMany()
                .HasForeignKey(usp => usp.SportID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserSportProfile>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_USP_CourtPosition",  "CourtPosition  IS NULL OR CourtPosition  IN ('BackCourt','FrontCourt','AllRound')");
                t.HasCheckConstraint("CK_USP_PlayStyle",      "PlayStyle      IS NULL OR PlayStyle      IN ('Aggressive','Defensive','Balanced')");
                t.HasCheckConstraint("CK_USP_StrokeStrength", "StrokeStrength IS NULL OR StrokeStrength IN ('Smash','Drop','Drive','AllRound')");
                t.HasCheckConstraint("CK_USP_SelfRated",      "SelfRatedLevel IS NULL OR SelfRatedLevel BETWEEN 1 AND 5");
            });

            // Notification relations
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_Notifications_Type",
                    "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat')");
            });

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserID, n.IsRead });

            modelBuilder.Entity<UserBadge>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatBookingProposal>()
                .HasOne(p => p.Match)
                .WithMany()
                .HasForeignKey(p => p.MatchID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatBookingProposal>()
                .HasOne(p => p.Sender)
                .WithMany()
                .HasForeignKey(p => p.SenderID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatBookingProposal>()
                .HasOne(p => p.Receiver)
                .WithMany()
                .HasForeignKey(p => p.ReceiverID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatBookingProposal>()
                .HasOne(p => p.Court)
                .WithMany()
                .HasForeignKey(p => p.CourtID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
