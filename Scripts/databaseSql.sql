IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Roles] (
    [RoleID] int NOT NULL IDENTITY,
    [RoleName] nvarchar(450) NOT NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleID])
);
GO

CREATE TABLE [Sports] (
    [SportID] int NOT NULL IDENTITY,
    [SportName] nvarchar(450) NOT NULL,
    [IconUrl] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_Sports] PRIMARY KEY ([SportID])
);
GO

CREATE TABLE [TimeSlots] (
    [SlotID] int NOT NULL IDENTITY,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [SlotLabel] nvarchar(max) NULL,
    CONSTRAINT [PK_TimeSlots] PRIMARY KEY ([SlotID])
);
GO

CREATE TABLE [Users] (
    [UserID] int NOT NULL IDENTITY,
    [Email] nvarchar(450) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [AvatarUrl] nvarchar(max) NULL,
    [DateOfBirth] date NULL,
    [Gender] nvarchar(max) NULL,
    [SkillLevel] nvarchar(max) NULL,
    [FavoriteSport] nvarchar(max) NULL,
    [DefaultAddress] nvarchar(max) NULL,
    [DefaultLatitude] decimal(10,8) NULL,
    [DefaultLongitude] decimal(11,8) NULL,
    [IsActive] bit NOT NULL,
    [IsVerified] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserID]),
    CONSTRAINT [CK_Users_Gender] CHECK (Gender IN ('M','F','O'))
);
GO

CREATE TABLE [ChatMessages] (
    [MessageID] int NOT NULL IDENTITY,
    [SenderID] int NOT NULL,
    [ReceiverID] int NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([MessageID]),
    CONSTRAINT [FK_ChatMessages_Users_ReceiverID] FOREIGN KEY ([ReceiverID]) REFERENCES [Users] ([UserID]),
    CONSTRAINT [FK_ChatMessages_Users_SenderID] FOREIGN KEY ([SenderID]) REFERENCES [Users] ([UserID])
);
GO

CREATE TABLE [CourtOwners] (
    [OwnerID] int NOT NULL IDENTITY,
    [UserID] int NOT NULL,
    [BusinessName] nvarchar(max) NOT NULL,
    [TaxCode] nvarchar(max) NULL,
    [BankAccount] nvarchar(max) NULL,
    [BankName] nvarchar(max) NULL,
    [IsApproved] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CourtOwners] PRIMARY KEY ([OwnerID]),
    CONSTRAINT [FK_CourtOwners_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
);
GO

CREATE TABLE [Friendships] (
    [FriendshipID] int NOT NULL IDENTITY,
    [SenderID] int NOT NULL,
    [ReceiverID] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Friendships] PRIMARY KEY ([FriendshipID]),
    CONSTRAINT [FK_Friendships_Users_ReceiverID] FOREIGN KEY ([ReceiverID]) REFERENCES [Users] ([UserID]),
    CONSTRAINT [FK_Friendships_Users_SenderID] FOREIGN KEY ([SenderID]) REFERENCES [Users] ([UserID])
);
GO

CREATE TABLE [Notifications] (
    [NotificationID] int NOT NULL IDENTITY,
    [UserID] int NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [LinkUrl] nvarchar(max) NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationID]),
    CONSTRAINT [CK_Notifications_Type] CHECK (Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat')),
    CONSTRAINT [FK_Notifications_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
);
GO

CREATE TABLE [UserRoles] (
    [UserID] int NOT NULL,
    [RoleID] int NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserID], [RoleID]),
    CONSTRAINT [FK_UserRoles_Roles_RoleID] FOREIGN KEY ([RoleID]) REFERENCES [Roles] ([RoleID]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserSportProfiles] (
    [ProfileID] int NOT NULL IDENTITY,
    [UserID] int NOT NULL,
    [SportID] int NOT NULL,
    [CourtPosition] nvarchar(max) NULL,
    [PlayStyle] nvarchar(max) NULL,
    [StrokeStrength] nvarchar(max) NULL,
    [SelfRatedLevel] int NULL,
    [ExperienceYears] int NULL,
    [SkillLevel] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserSportProfiles] PRIMARY KEY ([ProfileID]),
    CONSTRAINT [CK_USP_CourtPosition] CHECK (CourtPosition  IS NULL OR CourtPosition  IN ('BackCourt','FrontCourt','AllRound')),
    CONSTRAINT [CK_USP_PlayStyle] CHECK (PlayStyle      IS NULL OR PlayStyle      IN ('Aggressive','Defensive','Balanced')),
    CONSTRAINT [CK_USP_SelfRated] CHECK (SelfRatedLevel IS NULL OR SelfRatedLevel BETWEEN 1 AND 5),
    CONSTRAINT [CK_USP_StrokeStrength] CHECK (StrokeStrength IS NULL OR StrokeStrength IN ('Smash','Drop','Drive','AllRound')),
    CONSTRAINT [FK_UserSportProfiles_Sports_SportID] FOREIGN KEY ([SportID]) REFERENCES [Sports] ([SportID]),
    CONSTRAINT [FK_UserSportProfiles_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
);
GO

CREATE TABLE [CourtVenues] (
    [VenueID] int NOT NULL IDENTITY,
    [OwnerID] int NOT NULL,
    [VenueName] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [Ward] nvarchar(max) NULL,
    [District] nvarchar(450) NOT NULL,
    [City] nvarchar(450) NOT NULL,
    [Latitude] decimal(10,8) NULL,
    [Longitude] decimal(11,8) NULL,
    [PhoneContact] nvarchar(max) NULL,
    [OpenTime] time NOT NULL,
    [CloseTime] time NOT NULL,
    [AmenityParking] bit NOT NULL,
    [AmenityShower] bit NOT NULL,
    [AmenityLocker] bit NOT NULL,
    [AmenityFood] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CourtVenues] PRIMARY KEY ([VenueID]),
    CONSTRAINT [FK_CourtVenues_CourtOwners_OwnerID] FOREIGN KEY ([OwnerID]) REFERENCES [CourtOwners] ([OwnerID])
);
GO

CREATE TABLE [Courts] (
    [CourtID] int NOT NULL IDENTITY,
    [VenueID] int NOT NULL,
    [SportID] int NOT NULL,
    [CourtName] nvarchar(max) NOT NULL,
    [CourtType] nvarchar(max) NULL,
    [SurfaceType] nvarchar(max) NULL,
    [MaxPlayers] tinyint NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Courts] PRIMARY KEY ([CourtID]),
    CONSTRAINT [FK_Courts_CourtVenues_VenueID] FOREIGN KEY ([VenueID]) REFERENCES [CourtVenues] ([VenueID]),
    CONSTRAINT [FK_Courts_Sports_SportID] FOREIGN KEY ([SportID]) REFERENCES [Sports] ([SportID])
);
GO

CREATE TABLE [Bookings] (
    [BookingID] int NOT NULL IDENTITY,
    [UserID] int NOT NULL,
    [CourtID] int NOT NULL,
    [BookingDate] date NOT NULL,
    [TotalAmount] decimal(12,2) NOT NULL,
    [DiscountAmount] decimal(12,2) NOT NULL,
    [FinalAmount] decimal(12,2) NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [Note] nvarchar(max) NULL,
    [CancelReason] nvarchar(max) NULL,
    [BookedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Bookings] PRIMARY KEY ([BookingID]),
    CONSTRAINT [CK_Bookings_Status] CHECK (Status IN ('Pending','Confirmed','Cancelled','Completed','NoShow')),
    CONSTRAINT [FK_Bookings_Courts_CourtID] FOREIGN KEY ([CourtID]) REFERENCES [Courts] ([CourtID]),
    CONSTRAINT [FK_Bookings_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
);
GO

CREATE TABLE [CourtImages] (
    [ImageID] int NOT NULL IDENTITY,
    [CourtID] int NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [Caption] nvarchar(max) NULL,
    [SortOrder] tinyint NOT NULL,
    [IsMain] bit NOT NULL,
    CONSTRAINT [PK_CourtImages] PRIMARY KEY ([ImageID]),
    CONSTRAINT [FK_CourtImages_Courts_CourtID] FOREIGN KEY ([CourtID]) REFERENCES [Courts] ([CourtID]) ON DELETE CASCADE
);
GO

CREATE TABLE [PricingRules] (
    [PricingID] int NOT NULL IDENTITY,
    [CourtID] int NOT NULL,
    [SlotID] int NOT NULL,
    [DayType] nvarchar(450) NOT NULL,
    [UnitPrice] decimal(12,2) NOT NULL,
    [Currency] nvarchar(max) NOT NULL,
    [ValidFrom] datetime2 NOT NULL,
    [ValidTo] datetime2 NULL,
    CONSTRAINT [PK_PricingRules] PRIMARY KEY ([PricingID]),
    CONSTRAINT [CK_PricingRules_DayType] CHECK (DayType IN ('Weekday','Weekend','Holiday')),
    CONSTRAINT [FK_PricingRules_Courts_CourtID] FOREIGN KEY ([CourtID]) REFERENCES [Courts] ([CourtID]),
    CONSTRAINT [FK_PricingRules_TimeSlots_SlotID] FOREIGN KEY ([SlotID]) REFERENCES [TimeSlots] ([SlotID])
);
GO

CREATE TABLE [BookingSlots] (
    [BookingSlotID] int NOT NULL IDENTITY,
    [BookingID] int NOT NULL,
    [SlotID] int NOT NULL,
    [UnitPrice] decimal(12,2) NOT NULL,
    CONSTRAINT [PK_BookingSlots] PRIMARY KEY ([BookingSlotID]),
    CONSTRAINT [FK_BookingSlots_Bookings_BookingID] FOREIGN KEY ([BookingID]) REFERENCES [Bookings] ([BookingID]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookingSlots_TimeSlots_SlotID] FOREIGN KEY ([SlotID]) REFERENCES [TimeSlots] ([SlotID])
);
GO

CREATE TABLE [Matches] (
    [MatchID] int NOT NULL IDENTITY,
    [CreatedByUserID] int NOT NULL,
    [CourtID] int NULL,
    [BookingID] int NULL,
    [SportID] int NOT NULL,
    [MatchDate] date NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [MatchType] nvarchar(max) NOT NULL,
    [SkillRequired] nvarchar(max) NULL,
    [MaxParticipants] tinyint NOT NULL,
    [Title] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(450) NOT NULL,
    [RequiresApproval] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Matches] PRIMARY KEY ([MatchID]),
    CONSTRAINT [CK_Matches_MatchType] CHECK (MatchType IN ('Singles','Doubles','Mixed')),
    CONSTRAINT [CK_Matches_Status] CHECK (Status IN ('Open','Full','InProgress','Completed','Cancelled')),
    CONSTRAINT [FK_Matches_Bookings_BookingID] FOREIGN KEY ([BookingID]) REFERENCES [Bookings] ([BookingID]),
    CONSTRAINT [FK_Matches_Courts_CourtID] FOREIGN KEY ([CourtID]) REFERENCES [Courts] ([CourtID]),
    CONSTRAINT [FK_Matches_Sports_SportID] FOREIGN KEY ([SportID]) REFERENCES [Sports] ([SportID]),
    CONSTRAINT [FK_Matches_Users_CreatedByUserID] FOREIGN KEY ([CreatedByUserID]) REFERENCES [Users] ([UserID])
);
GO

CREATE TABLE [Payments] (
    [PaymentID] int NOT NULL IDENTITY,
    [BookingID] int NOT NULL,
    [Amount] decimal(12,2) NOT NULL,
    [PaymentMethod] nvarchar(max) NOT NULL,
    [TransactionRef] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [PaymentType] nvarchar(max) NOT NULL,
    [ReceiptUrl] nvarchar(max) NULL,
    [PaidAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentID]),
    CONSTRAINT [CK_Payments_PaymentMethod] CHECK (PaymentMethod IN ('VNPay','MoMo','ZaloPay','BankTransfer','Cash')),
    CONSTRAINT [CK_Payments_PaymentType] CHECK (PaymentType IN ('Payment','Refund')),
    CONSTRAINT [CK_Payments_Status] CHECK (Status IN ('Pending','Success','Failed','Refunded')),
    CONSTRAINT [FK_Payments_Bookings_BookingID] FOREIGN KEY ([BookingID]) REFERENCES [Bookings] ([BookingID])
);
GO

CREATE TABLE [Reviews] (
    [ReviewID] int NOT NULL IDENTITY,
    [CourtID] int NOT NULL,
    [UserID] int NOT NULL,
    [BookingID] int NOT NULL,
    [Rating] tinyint NOT NULL,
    [Comment] nvarchar(max) NULL,
    [ReviewedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([ReviewID]),
    CONSTRAINT [CK_Reviews_Rating] CHECK (Rating BETWEEN 1 AND 5),
    CONSTRAINT [FK_Reviews_Bookings_BookingID] FOREIGN KEY ([BookingID]) REFERENCES [Bookings] ([BookingID]),
    CONSTRAINT [FK_Reviews_Courts_CourtID] FOREIGN KEY ([CourtID]) REFERENCES [Courts] ([CourtID]),
    CONSTRAINT [FK_Reviews_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
);
GO

CREATE TABLE [MatchParticipants] (
    [ParticipantID] int NOT NULL IDENTITY,
    [MatchID] int NOT NULL,
    [UserID] int NOT NULL,
    [TeamSide] nvarchar(max) NULL,
    [JoinStatus] nvarchar(max) NOT NULL,
    [JoinedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MatchParticipants] PRIMARY KEY ([ParticipantID]),
    CONSTRAINT [CK_MatchParticipants_JoinStatus] CHECK (JoinStatus IN ('Pending','Accepted','Declined','Cancelled')),
    CONSTRAINT [CK_MatchParticipants_TeamSide] CHECK (TeamSide IS NULL OR TeamSide IN ('A','B')),
    CONSTRAINT [FK_MatchParticipants_Matches_MatchID] FOREIGN KEY ([MatchID]) REFERENCES [Matches] ([MatchID]) ON DELETE CASCADE,
    CONSTRAINT [FK_MatchParticipants_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
);
GO

CREATE INDEX [IX_Bookings_CourtID_BookingDate_Status] ON [Bookings] ([CourtID], [BookingDate], [Status]);
GO

CREATE INDEX [IX_Bookings_UserID_BookingDate] ON [Bookings] ([UserID], [BookingDate]);
GO

CREATE UNIQUE INDEX [IX_BookingSlots_BookingID_SlotID] ON [BookingSlots] ([BookingID], [SlotID]);
GO

CREATE INDEX [IX_BookingSlots_SlotID] ON [BookingSlots] ([SlotID]);
GO

CREATE INDEX [IX_ChatMessages_ReceiverID] ON [ChatMessages] ([ReceiverID]);
GO

CREATE INDEX [IX_ChatMessages_SenderID] ON [ChatMessages] ([SenderID]);
GO

CREATE INDEX [IX_CourtImages_CourtID] ON [CourtImages] ([CourtID]);
GO

CREATE UNIQUE INDEX [IX_CourtOwners_UserID] ON [CourtOwners] ([UserID]);
GO

CREATE INDEX [IX_Courts_SportID_VenueID] ON [Courts] ([SportID], [VenueID]);
GO

CREATE INDEX [IX_Courts_VenueID] ON [Courts] ([VenueID]);
GO

CREATE INDEX [IX_CourtVenues_City_District] ON [CourtVenues] ([City], [District]);
GO

CREATE INDEX [IX_CourtVenues_OwnerID] ON [CourtVenues] ([OwnerID]);
GO

CREATE INDEX [IX_Friendships_ReceiverID] ON [Friendships] ([ReceiverID]);
GO

CREATE INDEX [IX_Friendships_SenderID] ON [Friendships] ([SenderID]);
GO

CREATE INDEX [IX_Matches_BookingID] ON [Matches] ([BookingID]);
GO

CREATE INDEX [IX_Matches_CourtID] ON [Matches] ([CourtID]);
GO

CREATE INDEX [IX_Matches_CreatedByUserID] ON [Matches] ([CreatedByUserID]);
GO

CREATE INDEX [IX_Matches_SportID] ON [Matches] ([SportID]);
GO

CREATE INDEX [IX_Matches_Status_MatchDate_SportID] ON [Matches] ([Status], [MatchDate], [SportID]);
GO

CREATE UNIQUE INDEX [IX_MatchParticipants_MatchID_UserID] ON [MatchParticipants] ([MatchID], [UserID]);
GO

CREATE INDEX [IX_MatchParticipants_UserID] ON [MatchParticipants] ([UserID]);
GO

CREATE INDEX [IX_Notifications_UserID_IsRead] ON [Notifications] ([UserID], [IsRead]);
GO

CREATE INDEX [IX_Payments_BookingID] ON [Payments] ([BookingID]);
GO

CREATE UNIQUE INDEX [IX_PricingRules_CourtID_SlotID_DayType_ValidFrom] ON [PricingRules] ([CourtID], [SlotID], [DayType], [ValidFrom]);
GO

CREATE INDEX [IX_PricingRules_SlotID] ON [PricingRules] ([SlotID]);
GO

CREATE UNIQUE INDEX [IX_Reviews_BookingID_UserID] ON [Reviews] ([BookingID], [UserID]);
GO

CREATE INDEX [IX_Reviews_CourtID] ON [Reviews] ([CourtID]);
GO

CREATE INDEX [IX_Reviews_UserID] ON [Reviews] ([UserID]);
GO

CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);
GO

CREATE UNIQUE INDEX [IX_Sports_SportName] ON [Sports] ([SportName]);
GO

CREATE UNIQUE INDEX [IX_TimeSlots_StartTime_EndTime] ON [TimeSlots] ([StartTime], [EndTime]);
GO

CREATE INDEX [IX_UserRoles_RoleID] ON [UserRoles] ([RoleID]);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO

CREATE INDEX [IX_UserSportProfiles_SportID] ON [UserSportProfiles] ([SportID]);
GO

CREATE UNIQUE INDEX [IX_UserSportProfiles_UserID_SportID] ON [UserSportProfiles] ([UserID], [SportID]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260602151944_InitialSchema', N'8.0.3');
GO

COMMIT;
GO

