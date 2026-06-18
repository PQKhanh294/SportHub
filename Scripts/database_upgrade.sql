-- ============================================================
-- SportHub Database Upgrade Script
-- Áp dụng 3 migration sau InitialSchema lên database đã có sẵn.
-- An toàn khi chạy nhiều lần (idempotent).
--
-- Từ:    20260602151944_InitialSchema
-- Đến:   20260617015221_AddMatchReviewSystem
--
-- Thứ tự chạy:
--   1. SportHubUxEnhancements    (20260610142509)
--   2. AddMatchPaymentFlow       (20260616224007)
--   3. AddMatchReviewSystem      (20260617015221)
-- ============================================================

-- ============================================================
-- MIGRATION 1: SportHubUxEnhancements (20260610142509)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260610142509_SportHubUxEnhancements'
)
BEGIN
    -- Thêm cột tuỳ chỉnh sân vào Matches
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Matches' AND COLUMN_NAME='CustomCourtName')
        ALTER TABLE [Matches] ADD [CustomCourtName] nvarchar(max) NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Matches' AND COLUMN_NAME='CustomCourtAddress')
        ALTER TABLE [Matches] ADD [CustomCourtAddress] nvarchar(max) NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Matches' AND COLUMN_NAME='CustomPriceVnd')
        ALTER TABLE [Matches] ADD [CustomPriceVnd] decimal(12,2) NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Matches' AND COLUMN_NAME='CustomLatitude')
        ALTER TABLE [Matches] ADD [CustomLatitude] decimal(10,8) NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Matches' AND COLUMN_NAME='CustomLongitude')
        ALTER TABLE [Matches] ADD [CustomLongitude] decimal(11,8) NULL;

    -- Bảng ChatBookingProposals
    IF OBJECT_ID(N'[ChatBookingProposals]') IS NULL
    BEGIN
        CREATE TABLE [ChatBookingProposals] (
            [ProposalID]    int NOT NULL IDENTITY,
            [MatchID]       int NOT NULL,
            [SenderID]      int NOT NULL,
            [ReceiverID]    int NOT NULL,
            [CourtID]       int NULL,
            [BookingDate]   date NOT NULL,
            [StartTime]     time NOT NULL,
            [EndTime]       time NOT NULL,
            [EstimatedCost] decimal(12,2) NULL,
            [SplitMode]     nvarchar(max) NOT NULL,
            [Status]        nvarchar(450) NOT NULL,
            [Note]          nvarchar(max) NULL,
            [CreatedAt]     datetime2 NOT NULL,
            [UpdatedAt]     datetime2 NOT NULL,
            CONSTRAINT [PK_ChatBookingProposals] PRIMARY KEY ([ProposalID]),
            CONSTRAINT [CK_ChatBookingProposals_SplitMode] CHECK (SplitMode IN ('Equal','HostPays')),
            CONSTRAINT [CK_ChatBookingProposals_Status]    CHECK (Status    IN ('Waiting','Accepted','Rejected','Booked')),
            CONSTRAINT [FK_ChatBookingProposals_Courts_CourtID]       FOREIGN KEY ([CourtID])    REFERENCES [Courts]  ([CourtID]),
            CONSTRAINT [FK_ChatBookingProposals_Matches_MatchID]      FOREIGN KEY ([MatchID])    REFERENCES [Matches] ([MatchID]),
            CONSTRAINT [FK_ChatBookingProposals_Users_ReceiverID]     FOREIGN KEY ([ReceiverID]) REFERENCES [Users]   ([UserID]),
            CONSTRAINT [FK_ChatBookingProposals_Users_SenderID]       FOREIGN KEY ([SenderID])   REFERENCES [Users]   ([UserID])
        );

        CREATE INDEX [IX_ChatBookingProposals_CourtID]
            ON [ChatBookingProposals] ([CourtID]);

        CREATE INDEX [IX_ChatBookingProposals_MatchID_SenderID_ReceiverID_Status]
            ON [ChatBookingProposals] ([MatchID], [SenderID], [ReceiverID], [Status]);

        CREATE INDEX [IX_ChatBookingProposals_ReceiverID]
            ON [ChatBookingProposals] ([ReceiverID]);

        CREATE INDEX [IX_ChatBookingProposals_SenderID]
            ON [ChatBookingProposals] ([SenderID]);
    END

    -- Bảng MatchInteractions
    IF OBJECT_ID(N'[MatchInteractions]') IS NULL
    BEGIN
        CREATE TABLE [MatchInteractions] (
            [InteractionID] int NOT NULL IDENTITY,
            [MatchID]       int NOT NULL,
            [UserID]        int NOT NULL,
            [Action]        nvarchar(450) NOT NULL,
            [CreatedAt]     datetime2 NOT NULL,
            CONSTRAINT [PK_MatchInteractions] PRIMARY KEY ([InteractionID]),
            CONSTRAINT [CK_MatchInteractions_Action] CHECK (Action IN ('View','Skip','Request')),
            CONSTRAINT [FK_MatchInteractions_Matches_MatchID] FOREIGN KEY ([MatchID]) REFERENCES [Matches] ([MatchID]) ON DELETE CASCADE,
            CONSTRAINT [FK_MatchInteractions_Users_UserID]    FOREIGN KEY ([UserID])  REFERENCES [Users]   ([UserID])
        );

        CREATE INDEX [IX_MatchInteractions_MatchID_UserID_Action]
            ON [MatchInteractions] ([MatchID], [UserID], [Action]);

        CREATE INDEX [IX_MatchInteractions_UserID]
            ON [MatchInteractions] ([UserID]);
    END

    -- Bảng UserBadges
    IF OBJECT_ID(N'[UserBadges]') IS NULL
    BEGIN
        CREATE TABLE [UserBadges] (
            [BadgeID]       int NOT NULL IDENTITY,
            [UserID]        int NOT NULL,
            [BadgeKey]      nvarchar(450) NOT NULL,
            [BadgeName]     nvarchar(max) NOT NULL,
            [Level]         nvarchar(max) NOT NULL,
            [ProgressValue] int NOT NULL,
            [TargetValue]   int NOT NULL,
            [Description]   nvarchar(max) NOT NULL,
            [EarnedAt]      datetime2 NOT NULL,
            [UpdatedAt]     datetime2 NOT NULL,
            CONSTRAINT [PK_UserBadges]       PRIMARY KEY ([BadgeID]),
            CONSTRAINT [CK_UserBadges_Level] CHECK (Level IN ('Bronze','Silver','Gold')),
            CONSTRAINT [FK_UserBadges_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
        );

        CREATE UNIQUE INDEX [IX_UserBadges_UserID_BadgeKey]
            ON [UserBadges] ([UserID], [BadgeKey]);
    END

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260610142509_SportHubUxEnhancements', N'8.0.3');

    PRINT 'Migration 1/3 applied: SportHubUxEnhancements';
END
ELSE
    PRINT 'Migration 1/3 already applied: SportHubUxEnhancements';
GO

-- ============================================================
-- MIGRATION 2: AddMatchPaymentFlow (20260616224007)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260616224007_AddMatchPaymentFlow'
)
BEGIN
    -- Cột mới cho MatchParticipants
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MatchParticipants' AND COLUMN_NAME='PlayerFeeStatus')
        ALTER TABLE [MatchParticipants] ADD [PlayerFeeStatus] nvarchar(max) NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MatchParticipants' AND COLUMN_NAME='PlayerFeeDeadline')
        ALTER TABLE [MatchParticipants] ADD [PlayerFeeDeadline] datetime2 NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MatchParticipants' AND COLUMN_NAME='PlayerFeeReceiptUrl')
        ALTER TABLE [MatchParticipants] ADD [PlayerFeeReceiptUrl] nvarchar(max) NULL;

    -- Cột mới cho Matches
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Matches' AND COLUMN_NAME='DepositStatus')
        ALTER TABLE [Matches] ADD [DepositStatus] nvarchar(max) NOT NULL DEFAULT N'NotPaid';

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Matches' AND COLUMN_NAME='RemainingFeeStatus')
        ALTER TABLE [Matches] ADD [RemainingFeeStatus] nvarchar(max) NOT NULL DEFAULT N'NotDue';

    -- Cập nhật CHECK CONSTRAINT CK_Notifications_Type
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='CK_Notifications_Type')
        ALTER TABLE [Notifications] DROP CONSTRAINT [CK_Notifications_Type];

    ALTER TABLE [Notifications] ADD CONSTRAINT [CK_Notifications_Type] CHECK (Type IN (
        'MatchJoin','MatchApprove','MatchReject','MatchJoinExpired',
        'BookingConfirmed','BookingCancelled','System','Chat',
        'MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed'
    ));

    -- Cập nhật CHECK CONSTRAINT CK_MatchParticipants_JoinStatus (thêm 'Approved')
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='CK_MatchParticipants_JoinStatus')
        ALTER TABLE [MatchParticipants] DROP CONSTRAINT [CK_MatchParticipants_JoinStatus];

    ALTER TABLE [MatchParticipants] ADD CONSTRAINT [CK_MatchParticipants_JoinStatus]
        CHECK (JoinStatus IN ('Pending','Approved','Accepted','Declined','Cancelled'));

    -- Thêm CHECK CONSTRAINT mới cho PlayerFeeStatus
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='CK_MatchParticipants_PlayerFeeStatus')
        ALTER TABLE [MatchParticipants] ADD CONSTRAINT [CK_MatchParticipants_PlayerFeeStatus]
            CHECK (PlayerFeeStatus IS NULL OR PlayerFeeStatus IN ('AwaitingPayment','Paid','Expired'));

    -- Cập nhật CHECK CONSTRAINT CK_Matches_Status (thêm 'PendingDeposit')
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='CK_Matches_Status')
        ALTER TABLE [Matches] DROP CONSTRAINT [CK_Matches_Status];

    ALTER TABLE [Matches] ADD CONSTRAINT [CK_Matches_Status]
        CHECK (Status IN ('Open','Full','InProgress','Completed','Cancelled','PendingDeposit'));

    -- Thêm CHECK CONSTRAINTs mới cho Matches payment
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='CK_Matches_DepositStatus')
        ALTER TABLE [Matches] ADD CONSTRAINT [CK_Matches_DepositStatus]
            CHECK (DepositStatus IN ('NotPaid','Paid'));

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='CK_Matches_RemainingFeeStatus')
        ALTER TABLE [Matches] ADD CONSTRAINT [CK_Matches_RemainingFeeStatus]
            CHECK (RemainingFeeStatus IN ('NotDue','Notified','Paid'));

    -- Bảng MatchPayments
    IF OBJECT_ID(N'[MatchPayments]') IS NULL
    BEGIN
        CREATE TABLE [MatchPayments] (
            [MatchPaymentID] int NOT NULL IDENTITY,
            [MatchID]        int NOT NULL,
            [PayerUserID]    int NOT NULL,
            [PaymentType]    nvarchar(450) NOT NULL,
            [Amount]         decimal(12,2) NOT NULL,
            [Status]         nvarchar(450) NOT NULL,
            [ReceiptUrl]     nvarchar(max) NULL,
            [TransactionRef] nvarchar(max) NULL,
            [CreatedAt]      datetime2 NOT NULL,
            [ExpiresAt]      datetime2 NULL,
            [ConfirmedAt]    datetime2 NULL,
            CONSTRAINT [PK_MatchPayments]        PRIMARY KEY ([MatchPaymentID]),
            CONSTRAINT [CK_MatchPayments_Status] CHECK (Status      IN ('Pending','Confirmed','Expired','Refunded')),
            CONSTRAINT [CK_MatchPayments_Type]   CHECK (PaymentType IN ('HostDeposit','HostRemaining','PlayerFee')),
            CONSTRAINT [FK_MatchPayments_Matches_MatchID]      FOREIGN KEY ([MatchID])     REFERENCES [Matches] ([MatchID]) ON DELETE CASCADE,
            CONSTRAINT [FK_MatchPayments_Users_PayerUserID]    FOREIGN KEY ([PayerUserID]) REFERENCES [Users]   ([UserID])
        );

        CREATE INDEX [IX_MatchPayments_MatchID_PaymentType_Status]
            ON [MatchPayments] ([MatchID], [PaymentType], [Status]);

        CREATE INDEX [IX_MatchPayments_PayerUserID]
            ON [MatchPayments] ([PayerUserID]);
    END

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260616224007_AddMatchPaymentFlow', N'8.0.3');

    PRINT 'Migration 2/3 applied: AddMatchPaymentFlow';
END
ELSE
    PRINT 'Migration 2/3 already applied: AddMatchPaymentFlow';
GO

-- ============================================================
-- MIGRATION 3: AddMatchReviewSystem (20260617015221)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617015221_AddMatchReviewSystem'
)
BEGIN
    -- Cập nhật CHECK CONSTRAINT CK_Notifications_Type (thêm MatchCompleted, MatchReviewReminder)
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='CK_Notifications_Type')
        ALTER TABLE [Notifications] DROP CONSTRAINT [CK_Notifications_Type];

    ALTER TABLE [Notifications] ADD CONSTRAINT [CK_Notifications_Type] CHECK (Type IN (
        'MatchJoin','MatchApprove','MatchReject','MatchJoinExpired',
        'BookingConfirmed','BookingCancelled','System','Chat',
        'MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed',
        'MatchCompleted','MatchReviewReminder'
    ));

    -- Bảng MatchReviews
    IF OBJECT_ID(N'[MatchReviews]') IS NULL
    BEGIN
        CREATE TABLE [MatchReviews] (
            [MatchReviewID]     int NOT NULL IDENTITY,
            [MatchID]           int NOT NULL,
            [ReviewerUserID]    int NOT NULL,
            [ReviewedUserID]    int NOT NULL,
            [ReviewType]        nvarchar(450) NOT NULL,
            [ScoreOrganization] tinyint NULL,
            [ScoreEquipment]    tinyint NULL,
            [ScoreAtmosphere]   tinyint NULL,
            [ScoreHost]         tinyint NULL,
            [ScoreValueForMoney] tinyint NULL,
            [ScorePunctuality]  tinyint NULL,
            [ScoreSportsmanship] tinyint NULL,
            [ScoreSkillAccuracy] tinyint NULL,
            [Comment]           nvarchar(max) NULL,
            [IsVisible]         bit NOT NULL,
            [CreatedAt]         datetime2 NOT NULL,
            CONSTRAINT [PK_MatchReviews]            PRIMARY KEY ([MatchReviewID]),
            CONSTRAINT [CK_MatchReviews_Type]        CHECK (ReviewType IN ('PlayerToMatch','HostToPlayer')),
            CONSTRAINT [CK_MatchReviews_ScoreOrg]    CHECK (ScoreOrganization  IS NULL OR ScoreOrganization  BETWEEN 1 AND 5),
            CONSTRAINT [CK_MatchReviews_ScoreEquip]  CHECK (ScoreEquipment     IS NULL OR ScoreEquipment     BETWEEN 1 AND 5),
            CONSTRAINT [CK_MatchReviews_ScoreAtmos]  CHECK (ScoreAtmosphere    IS NULL OR ScoreAtmosphere    BETWEEN 1 AND 5),
            CONSTRAINT [CK_MatchReviews_ScoreHost]   CHECK (ScoreHost          IS NULL OR ScoreHost          BETWEEN 1 AND 5),
            CONSTRAINT [CK_MatchReviews_ScoreValue]  CHECK (ScoreValueForMoney IS NULL OR ScoreValueForMoney BETWEEN 1 AND 5),
            CONSTRAINT [CK_MatchReviews_ScorePunct]  CHECK (ScorePunctuality   IS NULL OR ScorePunctuality   BETWEEN 1 AND 5),
            CONSTRAINT [CK_MatchReviews_ScoreSport]  CHECK (ScoreSportsmanship IS NULL OR ScoreSportsmanship BETWEEN 1 AND 5),
            CONSTRAINT [CK_MatchReviews_ScoreSkill]  CHECK (ScoreSkillAccuracy IS NULL OR ScoreSkillAccuracy BETWEEN 1 AND 5),
            CONSTRAINT [FK_MatchReviews_Matches_MatchID]           FOREIGN KEY ([MatchID])         REFERENCES [Matches] ([MatchID]) ON DELETE CASCADE,
            CONSTRAINT [FK_MatchReviews_Users_ReviewedUserID]      FOREIGN KEY ([ReviewedUserID])  REFERENCES [Users]   ([UserID]),
            CONSTRAINT [FK_MatchReviews_Users_ReviewerUserID]      FOREIGN KEY ([ReviewerUserID])  REFERENCES [Users]   ([UserID])
        );

        CREATE UNIQUE INDEX [IX_MatchReviews_MatchID_ReviewerUserID_ReviewedUserID_ReviewType]
            ON [MatchReviews] ([MatchID], [ReviewerUserID], [ReviewedUserID], [ReviewType]);

        CREATE INDEX [IX_MatchReviews_ReviewedUserID]
            ON [MatchReviews] ([ReviewedUserID]);

        CREATE INDEX [IX_MatchReviews_ReviewerUserID]
            ON [MatchReviews] ([ReviewerUserID]);
    END

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260617015221_AddMatchReviewSystem', N'8.0.3');

    PRINT 'Migration 3/3 applied: AddMatchReviewSystem';
END
ELSE
    PRINT 'Migration 3/3 already applied: AddMatchReviewSystem';
GO

PRINT 'Upgrade complete.';
GO
