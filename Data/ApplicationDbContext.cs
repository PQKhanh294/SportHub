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
        public DbSet<MatchPayment> MatchPayments { get; set; } = null!;
        public DbSet<MatchReview> MatchReviews { get; set; } = null!;

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
        public DbSet<MessageReaction> MessageReactions { get; set; } = null!;
        public DbSet<MessageReport> MessageReports { get; set; } = null!;
        public DbSet<UserBan> UserBans { get; set; } = null!;

        // Nhóm 8: Wallet
        public DbSet<WalletTransaction> WalletTransactions { get; set; } = null!;
        public DbSet<WalletTopUpRequest> WalletTopUpRequests { get; set; } = null!;

        // Nhóm 9: Subscription
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<UserSubscription> UserSubscriptions { get; set; } = null!;
        public DbSet<SubscriptionOrder> SubscriptionOrders { get; set; } = null!;
        public DbSet<UserMatchCredit> UserMatchCredits { get; set; } = null!;
        public DbSet<SubscriptionUsage> SubscriptionUsages { get; set; } = null!;

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
            modelBuilder.Entity<MatchPayment>().ToTable("MatchPayments");
            modelBuilder.Entity<MatchReview>().ToTable("MatchReviews");
            modelBuilder.Entity<Review>().ToTable("Reviews");
            modelBuilder.Entity<WalletTransaction>().ToTable("WalletTransactions");
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
            modelBuilder.Entity<MatchPayment>().HasKey(mp => mp.MatchPaymentID);
            modelBuilder.Entity<MatchReview>().HasKey(mr => mr.MatchReviewID);
            modelBuilder.Entity<WalletTransaction>().HasKey(wt => wt.WalletTransactionID);

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
            modelBuilder.Entity<MatchPayment>().Property(mp => mp.Amount).HasPrecision(12, 2);
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
                // MatchType is sport-specific, no fixed constraint
                t.HasCheckConstraint("CK_Matches_Status", "Status IN ('Open','Full','InProgress','Completed','Cancelled','PendingDeposit')");
                t.HasCheckConstraint("CK_Matches_DepositStatus", "DepositStatus IN ('NotPaid','Paid')");
                t.HasCheckConstraint("CK_Matches_RemainingFeeStatus", "RemainingFeeStatus IN ('NotDue','Notified','Paid')");
            });

            modelBuilder.Entity<MatchParticipant>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_MatchParticipants_TeamSide", "TeamSide IS NULL OR TeamSide IN ('A','B')");
                t.HasCheckConstraint("CK_MatchParticipants_JoinStatus", "JoinStatus IN ('Pending','Approved','Accepted','Declined','Cancelled')");
                t.HasCheckConstraint("CK_MatchParticipants_PlayerFeeStatus", "PlayerFeeStatus IS NULL OR PlayerFeeStatus IN ('AwaitingPayment','Paid','Expired')");
            });

            modelBuilder.Entity<MatchPayment>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_MatchPayments_Type", "PaymentType IN ('HostDeposit','HostRemaining','PlayerFee')");
                t.HasCheckConstraint("CK_MatchPayments_Status", "Status IN ('Pending','Confirmed','Expired','Refunded')");
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

            modelBuilder.Entity<Match>()
                .HasOne(m => m.ParentMatch)
                .WithMany(m => m.RecurringChildren)
                .HasForeignKey(m => m.ParentMatchId)
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

            // MatchPayment relations
            modelBuilder.Entity<MatchPayment>()
                .HasOne(mp => mp.Match)
                .WithMany(m => m.MatchPayments)
                .HasForeignKey(mp => mp.MatchID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchPayment>()
                .HasOne(mp => mp.Payer)
                .WithMany()
                .HasForeignKey(mp => mp.PayerUserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MatchPayment>()
                .HasIndex(mp => new { mp.MatchID, mp.PaymentType, mp.Status });

            // MatchReview relations
            modelBuilder.Entity<MatchReview>()
                .HasOne(mr => mr.Match)
                .WithMany(m => m.Reviews)
                .HasForeignKey(mr => mr.MatchID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchReview>()
                .HasOne(mr => mr.ReviewerUser)
                .WithMany()
                .HasForeignKey(mr => mr.ReviewerUserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MatchReview>()
                .HasOne(mr => mr.ReviewedUser)
                .WithMany()
                .HasForeignKey(mr => mr.ReviewedUserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MatchReview>()
                .HasIndex(mr => new { mr.MatchID, mr.ReviewerUserID, mr.ReviewedUserID, mr.ReviewType })
                .IsUnique();

            // WalletTransaction relations
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.User)
                .WithMany()
                .HasForeignKey(wt => wt.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(wt => wt.UserID);

            modelBuilder.Entity<WalletTransaction>()
                .Property(wt => wt.Amount)
                .HasPrecision(12, 2);

            modelBuilder.Entity<User>()
                .Property(u => u.WalletBalance)
                .HasPrecision(12, 2)
                .HasDefaultValue(0m);

            modelBuilder.Entity<WalletTransaction>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_WalletTransactions_Type", "Type IN ('Refund','Deduction','AdminCredit','TopUp','MatchPayment')");
            });

            // WalletTopUpRequest
            modelBuilder.Entity<WalletTopUpRequest>().ToTable("WalletTopUpRequests");
            modelBuilder.Entity<WalletTopUpRequest>().HasKey(t => t.WalletTopUpID);
            modelBuilder.Entity<WalletTopUpRequest>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WalletTopUpRequest>()
                .HasIndex(t => t.TransactionRef)
                .IsUnique();
            modelBuilder.Entity<WalletTopUpRequest>()
                .HasIndex(t => new { t.UserID, t.Status });
            modelBuilder.Entity<WalletTopUpRequest>()
                .Property(t => t.Amount).HasPrecision(12, 2);
            modelBuilder.Entity<WalletTopUpRequest>()
                .Property(t => t.ActualAmount).HasPrecision(12, 2);
            modelBuilder.Entity<WalletTopUpRequest>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_WalletTopUpRequests_Status", "Status IN ('Pending','Confirmed','Expired')");
            });

            modelBuilder.Entity<MatchReview>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_MatchReviews_Type", "ReviewType IN ('PlayerToMatch','HostToPlayer')");
                t.HasCheckConstraint("CK_MatchReviews_ScoreOrg",   "ScoreOrganization  IS NULL OR ScoreOrganization  BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_MatchReviews_ScoreEquip", "ScoreEquipment     IS NULL OR ScoreEquipment     BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_MatchReviews_ScoreAtmos", "ScoreAtmosphere    IS NULL OR ScoreAtmosphere    BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_MatchReviews_ScoreHost",  "ScoreHost          IS NULL OR ScoreHost          BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_MatchReviews_ScoreValue", "ScoreValueForMoney IS NULL OR ScoreValueForMoney BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_MatchReviews_ScorePunct", "ScorePunctuality   IS NULL OR ScorePunctuality   BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_MatchReviews_ScoreSport", "ScoreSportsmanship IS NULL OR ScoreSportsmanship BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_MatchReviews_ScoreSkill", "ScoreSkillAccuracy IS NULL OR ScoreSkillAccuracy BETWEEN 1 AND 5");
            });

            // Notification type update: add MatchReviewReminder
            modelBuilder.Entity<Notification>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_Notifications_Type",
                    "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat','MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed','MatchCompleted','MatchReviewReminder','RemainingFeeReminder','WalletCredit')");
            });

            // ---- Chat Enhancement Phase 2 ----

            // ChatMessage self-ref for reply
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.ReplyToMessage)
                .WithMany()
                .HasForeignKey(m => m.ReplyToMessageID)
                .OnDelete(DeleteBehavior.NoAction);

            // MessageReaction
            modelBuilder.Entity<MessageReaction>().ToTable("MessageReactions");
            modelBuilder.Entity<MessageReaction>().HasKey(r => r.ReactionID);
            modelBuilder.Entity<MessageReaction>()
                .HasIndex(r => new { r.MessageID, r.UserID, r.ReactionType })
                .IsUnique();
            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(r => r.MessageID)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            // MessageReport
            modelBuilder.Entity<MessageReport>().ToTable("MessageReports");
            modelBuilder.Entity<MessageReport>().HasKey(r => r.ReportID);
            modelBuilder.Entity<MessageReport>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_MessageReports_Status", "Status IN ('Pending','Reviewed','Dismissed')");
            });
            modelBuilder.Entity<MessageReport>()
                .HasOne(r => r.Message)
                .WithMany()
                .HasForeignKey(r => r.MessageID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<MessageReport>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<MessageReport>()
                .HasOne(r => r.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByAdminID)
                .OnDelete(DeleteBehavior.NoAction);

            // UserBan
            modelBuilder.Entity<UserBan>().ToTable("UserBans");
            modelBuilder.Entity<UserBan>().HasKey(b => b.BanID);
            modelBuilder.Entity<UserBan>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<UserBan>()
                .HasOne(b => b.BannedByAdmin)
                .WithMany()
                .HasForeignKey(b => b.BannedByAdminID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<UserBan>()
                .HasOne(b => b.Report)
                .WithMany()
                .HasForeignKey(b => b.ReportID)
                .OnDelete(DeleteBehavior.NoAction);

            // ---- Subscription ----

            modelBuilder.Entity<SubscriptionPlan>().ToTable("SubscriptionPlans");
            modelBuilder.Entity<SubscriptionPlan>().HasKey(p => p.PlanID);
            modelBuilder.Entity<SubscriptionPlan>().HasIndex(p => p.PlanKey).IsUnique();
            modelBuilder.Entity<SubscriptionPlan>().Property(p => p.PriceMonthly).HasPrecision(12, 2);
            modelBuilder.Entity<SubscriptionPlan>().Property(p => p.PriceQuarterly).HasPrecision(12, 2);
            modelBuilder.Entity<SubscriptionPlan>().Property(p => p.PriceAnnual).HasPrecision(12, 2);

            modelBuilder.Entity<UserSubscription>().ToTable("UserSubscriptions");
            modelBuilder.Entity<UserSubscription>().HasKey(us => us.UserSubscriptionID);
            modelBuilder.Entity<UserSubscription>()
                .HasOne(us => us.User)
                .WithMany()
                .HasForeignKey(us => us.UserID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<UserSubscription>()
                .HasIndex(us => new { us.UserID, us.Status });
            modelBuilder.Entity<UserSubscription>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_UserSubscriptions_Status", "Status IN ('Active','Expired','Cancelled')");
                t.HasCheckConstraint("CK_UserSubscriptions_BillingCycle", "BillingCycle IN ('Monthly','Quarterly','Annual','Trial')");
            });

            modelBuilder.Entity<SubscriptionOrder>().ToTable("SubscriptionOrders");
            modelBuilder.Entity<SubscriptionOrder>().HasKey(o => o.SubscriptionOrderID);
            modelBuilder.Entity<SubscriptionOrder>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<SubscriptionOrder>()
                .HasIndex(o => o.TransactionRef)
                .IsUnique();
            modelBuilder.Entity<SubscriptionOrder>()
                .HasIndex(o => new { o.UserID, o.Status });
            modelBuilder.Entity<SubscriptionOrder>()
                .Property(o => o.Amount).HasPrecision(12, 2);
            modelBuilder.Entity<SubscriptionOrder>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_SubscriptionOrders_Status", "Status IN ('Pending','Confirmed','Expired')");
            });

            modelBuilder.Entity<UserMatchCredit>().ToTable("UserMatchCredits");
            modelBuilder.Entity<UserMatchCredit>().HasKey(c => c.UserMatchCreditID);
            modelBuilder.Entity<UserMatchCredit>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<UserMatchCredit>()
                .HasIndex(c => c.TransactionRef)
                .IsUnique();
            modelBuilder.Entity<UserMatchCredit>()
                .HasIndex(c => new { c.UserID, c.Status });
            modelBuilder.Entity<UserMatchCredit>()
                .Property(c => c.AmountPaid).HasPrecision(12, 2);
            modelBuilder.Entity<UserMatchCredit>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_UserMatchCredits_Status", "Status IN ('Pending','Confirmed')");
            });

            modelBuilder.Entity<SubscriptionUsage>().ToTable("SubscriptionUsages");
            modelBuilder.Entity<SubscriptionUsage>().HasKey(u => u.SubscriptionUsageID);
            modelBuilder.Entity<SubscriptionUsage>()
                .HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<SubscriptionUsage>()
                .HasIndex(u => new { u.UserID, u.YearMonth })
                .IsUnique();

            // Seed subscription plans
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    PlanID = 1, PlanKey = "Free", Name = "Miễn phí", SortOrder = 0,
                    Description = "Dành cho người mới bắt đầu",
                    PriceMonthly = 0, PriceQuarterly = 0, PriceAnnual = 0,
                    MonthlyJoinLimit = 3, MonthlyCreateLimit = 1,
                    CanSeePhoneNumber = false, CanFilterByDistance = false,
                    HasAiSuggestions = false, HasDetailedStats = false,
                    HasPriorityListing = false, PriorityScore = 0,
                    HasVerifiedBadge = false, HasPlayerFeeExempt = false
                },
                new SubscriptionPlan
                {
                    PlanID = 2, PlanKey = "Starter", Name = "Starter", SortOrder = 1,
                    Description = "Cho người chơi thường xuyên",
                    PriceMonthly = 39000, PriceQuarterly = 99000, PriceAnnual = 0,
                    MonthlyJoinLimit = 10, MonthlyCreateLimit = 3,
                    CanSeePhoneNumber = true, CanFilterByDistance = true,
                    HasAiSuggestions = false, HasDetailedStats = false,
                    HasPriorityListing = false, PriorityScore = 1,
                    HasVerifiedBadge = false, HasPlayerFeeExempt = false
                },
                new SubscriptionPlan
                {
                    PlanID = 3, PlanKey = "Pro", Name = "Pro", SortOrder = 2,
                    Description = "Cho người chơi nghiêm túc",
                    PriceMonthly = 99000, PriceQuarterly = 249000, PriceAnnual = 890000,
                    MonthlyJoinLimit = -1, MonthlyCreateLimit = -1,
                    CanSeePhoneNumber = true, CanFilterByDistance = true,
                    HasAiSuggestions = true, HasDetailedStats = true,
                    HasPriorityListing = true, PriorityScore = 2,
                    HasVerifiedBadge = false, HasPlayerFeeExempt = false
                },
                new SubscriptionPlan
                {
                    PlanID = 4, PlanKey = "Club", Name = "Club", SortOrder = 3,
                    Description = "Dành cho đội nhóm & tổ chức",
                    PriceMonthly = 199000, PriceQuarterly = 499000, PriceAnnual = 0,
                    MonthlyJoinLimit = -1, MonthlyCreateLimit = -1,
                    CanSeePhoneNumber = true, CanFilterByDistance = true,
                    HasAiSuggestions = true, HasDetailedStats = true,
                    HasPriorityListing = true, PriorityScore = 3,
                    HasVerifiedBadge = true, HasPlayerFeeExempt = true
                }
            );
        }
    }
}
