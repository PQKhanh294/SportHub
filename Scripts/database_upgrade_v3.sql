-- ============================================================
-- SportHub Database Upgrade Script v3 (gộp v3 + v4 + v5)
-- Áp dụng 3 migration kế tiếp lên database đã chạy v2.
-- An toàn khi chạy nhiều lần (idempotent).
--
-- Yêu cầu: Database đã có 8 migration từ v1 + v2:
--   ...20260622022145_AddWalletTopUpRequest (migration cuối của v2)
--
-- Thứ tự chạy:
--    9. AddGoogleAuth         (20260622064222)
--   10. AddSubscriptionSystem (20260622074205)
--   11. AddMatchExtensions    (20260622115901)
-- ============================================================


-- ============================================================
-- MIGRATION 9: AddGoogleAuth (20260622064222)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622064222_AddGoogleAuth'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'GoogleId')
        ALTER TABLE [Users] ADD [GoogleId] nvarchar(max) NULL;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622064222_AddGoogleAuth', N'8.0.3');

    PRINT 'Migration 9/11 applied: AddGoogleAuth';
END
ELSE
    PRINT 'Migration 9/11 already applied: AddGoogleAuth';
GO


-- ============================================================
-- MIGRATION 10: AddSubscriptionSystem (20260622074205)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622074205_AddSubscriptionSystem'
)
BEGIN
    PRINT 'Applying Migration 10/11: AddSubscriptionSystem...';

    -- SubscriptionOrders
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubscriptionOrders')
    BEGIN
        CREATE TABLE [SubscriptionOrders] (
            [SubscriptionOrderID] int          NOT NULL IDENTITY,
            [UserID]              int          NOT NULL,
            [PlanKey]             nvarchar(max) NOT NULL,
            [BillingCycle]        nvarchar(max) NOT NULL,
            [Amount]              decimal(12,2) NOT NULL,
            [TransactionRef]      nvarchar(450) NOT NULL,
            [Status]              nvarchar(450) NOT NULL,
            [CreatedAt]           datetime2    NOT NULL,
            [ExpiresAt]           datetime2    NOT NULL,
            [ConfirmedAt]         datetime2    NULL,
            CONSTRAINT [PK_SubscriptionOrders]        PRIMARY KEY ([SubscriptionOrderID]),
            CONSTRAINT [CK_SubscriptionOrders_Status] CHECK (Status IN ('Pending','Confirmed','Expired')),
            CONSTRAINT [FK_SubscriptionOrders_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
        );
        CREATE UNIQUE INDEX [IX_SubscriptionOrders_TransactionRef]   ON [SubscriptionOrders] ([TransactionRef]);
        CREATE        INDEX [IX_SubscriptionOrders_UserID_Status]    ON [SubscriptionOrders] ([UserID], [Status]);
    END

    -- SubscriptionPlans
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubscriptionPlans')
    BEGIN
        CREATE TABLE [SubscriptionPlans] (
            [PlanID]              int          NOT NULL IDENTITY,
            [PlanKey]             nvarchar(450) NOT NULL,
            [Name]                nvarchar(max) NOT NULL,
            [Description]         nvarchar(max) NOT NULL,
            [PriceMonthly]        decimal(12,2) NOT NULL,
            [PriceQuarterly]      decimal(12,2) NOT NULL,
            [PriceAnnual]         decimal(12,2) NOT NULL,
            [MonthlyJoinLimit]    int          NOT NULL,
            [MonthlyCreateLimit]  int          NOT NULL,
            [CanSeePhoneNumber]   bit          NOT NULL,
            [CanFilterByDistance] bit          NOT NULL,
            [HasAiSuggestions]    bit          NOT NULL,
            [HasDetailedStats]    bit          NOT NULL,
            [HasPriorityListing]  bit          NOT NULL,
            [PriorityScore]       int          NOT NULL,
            [HasVerifiedBadge]    bit          NOT NULL,
            [HasPlayerFeeExempt]  bit          NOT NULL,
            [IsActive]            bit          NOT NULL,
            [SortOrder]           int          NOT NULL,
            CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY ([PlanID])
        );
        CREATE UNIQUE INDEX [IX_SubscriptionPlans_PlanKey] ON [SubscriptionPlans] ([PlanKey]);

        -- Seed default plans
        SET IDENTITY_INSERT [SubscriptionPlans] ON;
        INSERT INTO [SubscriptionPlans]
            ([PlanID],[PlanKey],[Name],[Description],
             [PriceMonthly],[PriceQuarterly],[PriceAnnual],
             [MonthlyJoinLimit],[MonthlyCreateLimit],
             [CanSeePhoneNumber],[CanFilterByDistance],[HasAiSuggestions],
             [HasDetailedStats],[HasPriorityListing],[PriorityScore],
             [HasVerifiedBadge],[HasPlayerFeeExempt],[IsActive],[SortOrder])
        VALUES
            (1,'Free',   'Miễn phí',  'Dành cho người mới bắt đầu',    0,      0,      0,      3,  1,  0,0,0,0,0,0,0,0,1,0),
            (2,'Starter','Starter',   'Cho người chơi thường xuyên',    39000,  99000,  0,     10,  3,  1,1,0,0,0,1,0,0,1,1),
            (3,'Pro',    'Pro',        'Cho người chơi nghiêm túc',     99000, 249000, 890000, -1, -1,  1,1,1,1,1,2,0,0,1,2),
            (4,'Club',   'Club',       'Dành cho đội nhóm & tổ chức', 199000, 499000,  0,     -1, -1,  1,1,1,1,1,3,1,1,1,3);
        SET IDENTITY_INSERT [SubscriptionPlans] OFF;
    END

    -- SubscriptionUsages
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubscriptionUsages')
    BEGIN
        CREATE TABLE [SubscriptionUsages] (
            [SubscriptionUsageID] int          NOT NULL IDENTITY,
            [UserID]              int          NOT NULL,
            [YearMonth]           nvarchar(450) NOT NULL,
            [JoinCount]           int          NOT NULL,
            [CreateCount]         int          NOT NULL,
            CONSTRAINT [PK_SubscriptionUsages]            PRIMARY KEY ([SubscriptionUsageID]),
            CONSTRAINT [FK_SubscriptionUsages_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
        );
        CREATE UNIQUE INDEX [IX_SubscriptionUsages_UserID_YearMonth] ON [SubscriptionUsages] ([UserID], [YearMonth]);
    END

    -- UserMatchCredits
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserMatchCredits')
    BEGIN
        CREATE TABLE [UserMatchCredits] (
            [UserMatchCreditID] int          NOT NULL IDENTITY,
            [UserID]            int          NOT NULL,
            [RemainingCredits]  int          NOT NULL,
            [TransactionRef]    nvarchar(450) NOT NULL,
            [AmountPaid]        decimal(12,2) NOT NULL,
            [Status]            nvarchar(450) NOT NULL,
            [PurchasedAt]       datetime2    NOT NULL,
            [ExpiresAt]         datetime2    NOT NULL,
            CONSTRAINT [PK_UserMatchCredits]            PRIMARY KEY ([UserMatchCreditID]),
            CONSTRAINT [CK_UserMatchCredits_Status]     CHECK (Status IN ('Pending','Confirmed')),
            CONSTRAINT [FK_UserMatchCredits_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
        );
        CREATE UNIQUE INDEX [IX_UserMatchCredits_TransactionRef]  ON [UserMatchCredits] ([TransactionRef]);
        CREATE        INDEX [IX_UserMatchCredits_UserID_Status]   ON [UserMatchCredits] ([UserID], [Status]);
    END

    -- UserSubscriptions
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserSubscriptions')
    BEGIN
        CREATE TABLE [UserSubscriptions] (
            [UserSubscriptionID] int          NOT NULL IDENTITY,
            [UserID]             int          NOT NULL,
            [PlanKey]            nvarchar(max) NOT NULL,
            [BillingCycle]       nvarchar(max) NOT NULL,
            [StartAt]            datetime2    NOT NULL,
            [EndAt]              datetime2    NOT NULL,
            [Status]             nvarchar(450) NOT NULL,
            [CreatedAt]          datetime2    NOT NULL,
            CONSTRAINT [PK_UserSubscriptions]              PRIMARY KEY ([UserSubscriptionID]),
            CONSTRAINT [CK_UserSubscriptions_BillingCycle] CHECK (BillingCycle IN ('Monthly','Quarterly','Annual','Trial')),
            CONSTRAINT [CK_UserSubscriptions_Status]       CHECK (Status IN ('Active','Expired','Cancelled')),
            CONSTRAINT [FK_UserSubscriptions_Users_UserID] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
        );
        CREATE INDEX [IX_UserSubscriptions_UserID_Status] ON [UserSubscriptions] ([UserID], [Status]);
    END

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622074205_AddSubscriptionSystem', N'8.0.3');

    PRINT 'Migration 10/11 applied: AddSubscriptionSystem';
END
ELSE
    PRINT 'Migration 10/11 already applied: AddSubscriptionSystem';
GO


-- ============================================================
-- MIGRATION 11: AddMatchExtensions (20260622115901)
-- Thêm các field hỗ trợ trận đấu định kỳ & chia phí
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622115901_AddMatchExtensions'
)
BEGIN
    PRINT 'Applying Migration 11/11: AddMatchExtensions...';

    -- Bỏ check constraint cũ trên MatchType (nếu còn tồn tại)
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_NAME = 'CK_Matches_MatchType' AND TABLE_NAME = 'Matches'
    )
        ALTER TABLE [Matches] DROP CONSTRAINT [CK_Matches_MatchType];

    -- Thêm cột IsRecurring
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Matches' AND COLUMN_NAME = 'IsRecurring')
        ALTER TABLE [Matches] ADD [IsRecurring] bit NOT NULL DEFAULT 0;

    -- Thêm cột IsSplitFee
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Matches' AND COLUMN_NAME = 'IsSplitFee')
        ALTER TABLE [Matches] ADD [IsSplitFee] bit NOT NULL DEFAULT 0;

    -- Thêm cột ParentMatchId
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Matches' AND COLUMN_NAME = 'ParentMatchId')
        ALTER TABLE [Matches] ADD [ParentMatchId] int NULL;

    -- Thêm cột RecurringDays
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Matches' AND COLUMN_NAME = 'RecurringDays')
        ALTER TABLE [Matches] ADD [RecurringDays] nvarchar(max) NULL;

    -- Thêm cột RecurringUntil
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Matches' AND COLUMN_NAME = 'RecurringUntil')
        ALTER TABLE [Matches] ADD [RecurringUntil] datetime2 NULL;

    -- Index + FK tự tham chiếu cho ParentMatchId
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Matches_ParentMatchId' AND object_id = OBJECT_ID('Matches'))
        CREATE INDEX [IX_Matches_ParentMatchId] ON [Matches] ([ParentMatchId]);

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_NAME = 'FK_Matches_Matches_ParentMatchId' AND TABLE_NAME = 'Matches'
    )
        ALTER TABLE [Matches]
            ADD CONSTRAINT [FK_Matches_Matches_ParentMatchId]
            FOREIGN KEY ([ParentMatchId]) REFERENCES [Matches] ([MatchID]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622115901_AddMatchExtensions', N'8.0.3');

    PRINT 'Migration 11/11 applied: AddMatchExtensions';
END
ELSE
    PRINT 'Migration 11/11 already applied: AddMatchExtensions';
GO


PRINT '==> Upgrade v3 (gộp v3+v4+v5) complete. Database is up to date.';
GO
