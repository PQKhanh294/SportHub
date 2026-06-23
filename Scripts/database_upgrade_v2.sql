-- ============================================================
-- SportHub Database Upgrade Script v2
-- Áp dụng 5 migration kế tiếp lên database đã chạy v1.
-- An toàn khi chạy nhiều lần (idempotent).
--
-- Yêu cầu: Database đã có 3 migration từ file database_upgrade.sql (v1):
--   20260610142509_SportHubUxEnhancements
--   20260616224007_AddMatchPaymentFlow
--   20260617015221_AddMatchReviewSystem
--
-- Thứ tự chạy:
--   4. AddWalletAndReminderFields    (20260620171255)
--   5. AddChatMessageExtensions      (20260621004137)
--   6. ChatEnhancementsPhase2        (20260621014815)
--   7. AddLoginCount                 (20260621072541)
--   8. AddWalletTopUpRequest         (20260622022145)
-- ============================================================

-- ============================================================
-- MIGRATION 4: AddWalletAndReminderFields (20260620171255)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620171255_AddWalletAndReminderFields'
)
BEGIN
    -- Cập nhật CHECK CONSTRAINT CK_Notifications_Type
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = 'CK_Notifications_Type')
        ALTER TABLE [Notifications] DROP CONSTRAINT [CK_Notifications_Type];

    ALTER TABLE [Notifications] ADD CONSTRAINT [CK_Notifications_Type] CHECK (Type IN (
        'MatchJoin','MatchApprove','MatchReject','MatchJoinExpired',
        'BookingConfirmed','BookingCancelled','System','Chat',
        'MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed',
        'MatchCompleted','MatchReviewReminder','RemainingFeeReminder','WalletCredit'
    ));

    -- Thêm cột WalletBalance cho Users
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'WalletBalance')
        ALTER TABLE [Users] ADD [WalletBalance] decimal(12,2) NOT NULL DEFAULT 0;

    -- Thêm cột ReminderSentAt cho MatchPayments
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'MatchPayments' AND COLUMN_NAME = 'ReminderSentAt')
        ALTER TABLE [MatchPayments] ADD [ReminderSentAt] datetime2 NULL;

    -- Bảng WalletTransactions
    IF OBJECT_ID(N'[WalletTransactions]') IS NULL
    BEGIN
        CREATE TABLE [WalletTransactions] (
            [WalletTransactionID] int NOT NULL IDENTITY,
            [UserID]              int NOT NULL,
            [Amount]              decimal(12,2) NOT NULL,
            [Type]                nvarchar(max) NOT NULL,
            [Description]         nvarchar(max) NOT NULL,
            [RelatedMatchID]      int NULL,
            [CreatedAt]           datetime2 NOT NULL,
            CONSTRAINT [PK_WalletTransactions]      PRIMARY KEY ([WalletTransactionID]),
            CONSTRAINT [CK_WalletTransactions_Type] CHECK (Type IN ('Refund','Deduction','AdminCredit')),
            CONSTRAINT [FK_WalletTransactions_Users_UserID]
                FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
        );

        CREATE INDEX [IX_WalletTransactions_UserID]
            ON [WalletTransactions] ([UserID]);
    END

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260620171255_AddWalletAndReminderFields', N'8.0.3');

    PRINT 'Migration 4/8 applied: AddWalletAndReminderFields';
END
ELSE
    PRINT 'Migration 4/8 already applied: AddWalletAndReminderFields';
GO

-- ============================================================
-- MIGRATION 5: AddChatMessageExtensions (20260621004137)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621004137_AddChatMessageExtensions'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ChatMessages' AND COLUMN_NAME = 'ImageUrl')
        ALTER TABLE [ChatMessages] ADD [ImageUrl] nvarchar(max) NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ChatMessages' AND COLUMN_NAME = 'MatchCardJson')
        ALTER TABLE [ChatMessages] ADD [MatchCardJson] nvarchar(max) NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ChatMessages' AND COLUMN_NAME = 'MessageType')
        ALTER TABLE [ChatMessages] ADD [MessageType] nvarchar(max) NOT NULL DEFAULT N'';

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260621004137_AddChatMessageExtensions', N'8.0.3');

    PRINT 'Migration 5/8 applied: AddChatMessageExtensions';
END
ELSE
    PRINT 'Migration 5/8 already applied: AddChatMessageExtensions';
GO

-- ============================================================
-- MIGRATION 6: ChatEnhancementsPhase2 (20260621014815)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621014815_ChatEnhancementsPhase2'
)
BEGIN
    -- Cột mới cho Users
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'BanEndAt')
        ALTER TABLE [Users] ADD [BanEndAt] datetime2 NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsBanned')
        ALTER TABLE [Users] ADD [IsBanned] bit NOT NULL DEFAULT 0;

    -- Cột mới cho ChatMessages
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ChatMessages' AND COLUMN_NAME = 'DeletedAt')
        ALTER TABLE [ChatMessages] ADD [DeletedAt] datetime2 NULL;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ChatMessages' AND COLUMN_NAME = 'IsDeleted')
        ALTER TABLE [ChatMessages] ADD [IsDeleted] bit NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ChatMessages' AND COLUMN_NAME = 'IsPinned')
        ALTER TABLE [ChatMessages] ADD [IsPinned] bit NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ChatMessages' AND COLUMN_NAME = 'ReplyToMessageID')
        ALTER TABLE [ChatMessages] ADD [ReplyToMessageID] int NULL;

    -- Self-reference FK cho ChatMessages.ReplyToMessageID
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_ChatMessages_ChatMessages_ReplyToMessageID')
        EXEC('ALTER TABLE [ChatMessages] ADD CONSTRAINT [FK_ChatMessages_ChatMessages_ReplyToMessageID] FOREIGN KEY ([ReplyToMessageID]) REFERENCES [ChatMessages] ([MessageID])');

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChatMessages_ReplyToMessageID' AND object_id = OBJECT_ID('ChatMessages'))
        CREATE INDEX [IX_ChatMessages_ReplyToMessageID] ON [ChatMessages] ([ReplyToMessageID]);

    -- Bảng MessageReactions
    IF OBJECT_ID(N'[MessageReactions]') IS NULL
    BEGIN
        CREATE TABLE [MessageReactions] (
            [ReactionID]   int NOT NULL IDENTITY,
            [MessageID]    int NOT NULL,
            [UserID]       int NOT NULL,
            [ReactionType] nvarchar(450) NOT NULL,
            [CreatedAt]    datetime2 NOT NULL,
            CONSTRAINT [PK_MessageReactions] PRIMARY KEY ([ReactionID]),
            CONSTRAINT [FK_MessageReactions_ChatMessages_MessageID]
                FOREIGN KEY ([MessageID]) REFERENCES [ChatMessages] ([MessageID]) ON DELETE CASCADE,
            CONSTRAINT [FK_MessageReactions_Users_UserID]
                FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
        );

        CREATE UNIQUE INDEX [IX_MessageReactions_MessageID_UserID_ReactionType]
            ON [MessageReactions] ([MessageID], [UserID], [ReactionType]);

        CREATE INDEX [IX_MessageReactions_UserID]
            ON [MessageReactions] ([UserID]);
    END

    -- Bảng MessageReports
    IF OBJECT_ID(N'[MessageReports]') IS NULL
    BEGIN
        CREATE TABLE [MessageReports] (
            [ReportID]           int NOT NULL IDENTITY,
            [MessageID]          int NOT NULL,
            [ReporterID]         int NOT NULL,
            [Reason]             nvarchar(max) NULL,
            [AiAnalysis]         nvarchar(max) NOT NULL,
            [AiViolationScore]   int NOT NULL,
            [AiRecommendation]   nvarchar(max) NOT NULL,
            [Status]             nvarchar(max) NOT NULL,
            [AdminNote]          nvarchar(max) NULL,
            [CreatedAt]          datetime2 NOT NULL,
            [ReviewedAt]         datetime2 NULL,
            [ReviewedByAdminID]  int NULL,
            CONSTRAINT [PK_MessageReports]        PRIMARY KEY ([ReportID]),
            CONSTRAINT [CK_MessageReports_Status] CHECK (Status IN ('Pending','Reviewed','Dismissed')),
            CONSTRAINT [FK_MessageReports_ChatMessages_MessageID]
                FOREIGN KEY ([MessageID]) REFERENCES [ChatMessages] ([MessageID]),
            CONSTRAINT [FK_MessageReports_Users_ReporterID]
                FOREIGN KEY ([ReporterID]) REFERENCES [Users] ([UserID]),
            CONSTRAINT [FK_MessageReports_Users_ReviewedByAdminID]
                FOREIGN KEY ([ReviewedByAdminID]) REFERENCES [Users] ([UserID])
        );

        CREATE INDEX [IX_MessageReports_MessageID]        ON [MessageReports] ([MessageID]);
        CREATE INDEX [IX_MessageReports_ReporterID]       ON [MessageReports] ([ReporterID]);
        CREATE INDEX [IX_MessageReports_ReviewedByAdminID] ON [MessageReports] ([ReviewedByAdminID]);
    END

    -- Bảng UserBans (phụ thuộc MessageReports → tạo sau)
    IF OBJECT_ID(N'[UserBans]') IS NULL
    BEGIN
        CREATE TABLE [UserBans] (
            [BanID]            int NOT NULL IDENTITY,
            [UserID]           int NOT NULL,
            [BannedByAdminID]  int NOT NULL,
            [ReportID]         int NULL,
            [BanType]          nvarchar(max) NOT NULL,
            [Reason]           nvarchar(max) NOT NULL,
            [StartAt]          datetime2 NOT NULL,
            [EndAt]            datetime2 NULL,
            [IsActive]         bit NOT NULL,
            CONSTRAINT [PK_UserBans] PRIMARY KEY ([BanID]),
            CONSTRAINT [FK_UserBans_Users_UserID]
                FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID]),
            CONSTRAINT [FK_UserBans_Users_BannedByAdminID]
                FOREIGN KEY ([BannedByAdminID]) REFERENCES [Users] ([UserID]),
            CONSTRAINT [FK_UserBans_MessageReports_ReportID]
                FOREIGN KEY ([ReportID]) REFERENCES [MessageReports] ([ReportID])
        );

        CREATE INDEX [IX_UserBans_UserID]          ON [UserBans] ([UserID]);
        CREATE INDEX [IX_UserBans_BannedByAdminID] ON [UserBans] ([BannedByAdminID]);
        CREATE INDEX [IX_UserBans_ReportID]        ON [UserBans] ([ReportID]);
    END

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260621014815_ChatEnhancementsPhase2', N'8.0.3');

    PRINT 'Migration 6/8 applied: ChatEnhancementsPhase2';
END
ELSE
    PRINT 'Migration 6/8 already applied: ChatEnhancementsPhase2';
GO

-- ============================================================
-- MIGRATION 7: AddLoginCount (20260621072541)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621072541_AddLoginCount'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'LoginCount')
        ALTER TABLE [Users] ADD [LoginCount] int NOT NULL DEFAULT 0;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260621072541_AddLoginCount', N'8.0.3');

    PRINT 'Migration 7/8 applied: AddLoginCount';
END
ELSE
    PRINT 'Migration 7/8 already applied: AddLoginCount';
GO

-- ============================================================
-- MIGRATION 8: AddWalletTopUpRequest (20260622022145)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622022145_AddWalletTopUpRequest'
)
BEGIN
    -- Cập nhật CHECK CONSTRAINT CK_WalletTransactions_Type (thêm TopUp, MatchPayment)
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = 'CK_WalletTransactions_Type')
        ALTER TABLE [WalletTransactions] DROP CONSTRAINT [CK_WalletTransactions_Type];

    ALTER TABLE [WalletTransactions] ADD CONSTRAINT [CK_WalletTransactions_Type]
        CHECK (Type IN ('Refund','Deduction','AdminCredit','TopUp','MatchPayment'));

    -- Bảng WalletTopUpRequests
    IF OBJECT_ID(N'[WalletTopUpRequests]') IS NULL
    BEGIN
        CREATE TABLE [WalletTopUpRequests] (
            [WalletTopUpID]  int NOT NULL IDENTITY,
            [UserID]         int NOT NULL,
            [Amount]         decimal(12,2) NOT NULL,
            [TransactionRef] nvarchar(450) NOT NULL,
            [Status]         nvarchar(450) NOT NULL,
            [CreatedAt]      datetime2 NOT NULL,
            [ExpiresAt]      datetime2 NOT NULL,
            [ConfirmedAt]    datetime2 NULL,
            [ActualAmount]   decimal(12,2) NULL,
            CONSTRAINT [PK_WalletTopUpRequests]        PRIMARY KEY ([WalletTopUpID]),
            CONSTRAINT [CK_WalletTopUpRequests_Status] CHECK (Status IN ('Pending','Confirmed','Expired')),
            CONSTRAINT [FK_WalletTopUpRequests_Users_UserID]
                FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
        );

        CREATE UNIQUE INDEX [IX_WalletTopUpRequests_TransactionRef]
            ON [WalletTopUpRequests] ([TransactionRef]);

        CREATE INDEX [IX_WalletTopUpRequests_UserID_Status]
            ON [WalletTopUpRequests] ([UserID], [Status]);
    END

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622022145_AddWalletTopUpRequest', N'8.0.3');

    PRINT 'Migration 8/8 applied: AddWalletTopUpRequest';
END
ELSE
    PRINT 'Migration 8/8 already applied: AddWalletTopUpRequest';
GO

PRINT 'Upgrade v2 complete.';
GO
