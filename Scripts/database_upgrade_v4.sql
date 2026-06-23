-- ============================================================
-- SportHub Database Upgrade v4
-- Gồm: fix encoding SubscriptionPlans + hệ thống Ví & Khuyến mãi
-- An toàn khi chạy nhiều lần (IF NOT EXISTS + idempotent UPDATEs).
-- ============================================================

-- ─────────────────────────────────────────────────────────────
-- 1. Fix encoding SubscriptionPlans (từ v3)
-- ─────────────────────────────────────────────────────────────
UPDATE [SubscriptionPlans] SET [Name] = N'Miễn phí',  [Description] = N'Dành cho người mới bắt đầu'   WHERE [PlanKey] = N'Free';
UPDATE [SubscriptionPlans] SET [Name] = N'Starter',   [Description] = N'Cho người chơi thường xuyên'  WHERE [PlanKey] = N'Starter';
UPDATE [SubscriptionPlans] SET [Name] = N'Pro',        [Description] = N'Cho người chơi nghiêm túc'    WHERE [PlanKey] = N'Pro';
UPDATE [SubscriptionPlans] SET [Name] = N'Club',       [Description] = N'Dành cho đội nhóm & tổ chức' WHERE [PlanKey] = N'Club';
PRINT 'SubscriptionPlans encoding fixed.';
GO

-- ─────────────────────────────────────────────────────────────
-- 2. Promotion Campaigns
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PromotionCampaigns')
BEGIN
    CREATE TABLE [PromotionCampaigns] (
        [CampaignID]        INT IDENTITY(1,1) PRIMARY KEY,
        [Name]              NVARCHAR(200)   NOT NULL,
        [Description]       NVARCHAR(1000)  NULL,
        -- Manual | FirstLogin | Birthday | WomensDay | MensDay | PromoCode | PersonalVoucher
        [TriggerType]       NVARCHAR(50)    NOT NULL DEFAULT N'Manual',
        [Amount]            DECIMAL(18,2)   NOT NULL,
        [IsActive]          BIT             NOT NULL DEFAULT 1,
        [StartDate]         DATETIME2       NULL,
        [EndDate]           DATETIME2       NULL,
        -- All | HostOnly | Gender:F | Gender:M | NewUser
        [ApplicableScope]   NVARCHAR(100)   NOT NULL DEFAULT N'All',
        [MaxRedemptions]    INT             NULL,
        [RedemptionCount]   INT             NOT NULL DEFAULT 0,
        [CreatedByAdminID]  INT             NOT NULL,
        [CreatedAt]         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_PromotionCampaigns_User] FOREIGN KEY ([CreatedByAdminID]) REFERENCES [Users]([UserID]) ON DELETE NO ACTION
    );
    PRINT 'Table PromotionCampaigns created.';
END
GO

-- ─────────────────────────────────────────────────────────────
-- 3. Promo Codes (mã chia sẻ, nhiều người dùng)
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PromoCodes')
BEGIN
    CREATE TABLE [PromoCodes] (
        [PromoCodeID]   INT IDENTITY(1,1) PRIMARY KEY,
        [CampaignID]    INT             NOT NULL,
        [Code]          NVARCHAR(50)    NOT NULL,
        [MaxUses]       INT             NULL,  -- NULL = không giới hạn
        [UseCount]      INT             NOT NULL DEFAULT 0,
        [ExpiresAt]     DATETIME2       NULL,
        [IsActive]      BIT             NOT NULL DEFAULT 1,
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_PromoCodes_Campaign] FOREIGN KEY ([CampaignID]) REFERENCES [PromotionCampaigns]([CampaignID]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [UX_PromoCodes_Code] ON [PromoCodes]([Code]);
    PRINT 'Table PromoCodes created.';
END
GO

-- ─────────────────────────────────────────────────────────────
-- 4. User Vouchers (mã cá nhân, cấp riêng cho từng user)
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserVouchers')
BEGIN
    CREATE TABLE [UserVouchers] (
        [VoucherID]     INT IDENTITY(1,1) PRIMARY KEY,
        [UserID]        INT             NOT NULL,
        [CampaignID]    INT             NULL,
        [Code]          NVARCHAR(50)    NOT NULL,  -- mã hiển thị cho user
        [Amount]        DECIMAL(18,2)   NOT NULL,
        [IsUsed]        BIT             NOT NULL DEFAULT 0,
        [UsedAt]        DATETIME2       NULL,
        [ExpiresAt]     DATETIME2       NULL,
        [IssuedAt]      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [Note]          NVARCHAR(500)   NULL,      -- lý do cấp (admin ghi)
        [IssuedByAdminID] INT           NULL,
        CONSTRAINT [FK_UserVouchers_User]    FOREIGN KEY ([UserID])    REFERENCES [Users]([UserID]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserVouchers_Campaign] FOREIGN KEY ([CampaignID]) REFERENCES [PromotionCampaigns]([CampaignID]) ON DELETE SET NULL,
        CONSTRAINT [FK_UserVouchers_Admin]   FOREIGN KEY ([IssuedByAdminID]) REFERENCES [Users]([UserID]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [UX_UserVouchers_Code] ON [UserVouchers]([Code]);
    PRINT 'Table UserVouchers created.';
END
GO

-- ─────────────────────────────────────────────────────────────
-- 5. Promotion Redemptions (lịch sử nhận thưởng)
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PromotionRedemptions')
BEGIN
    CREATE TABLE [PromotionRedemptions] (
        [RedemptionID]          INT IDENTITY(1,1) PRIMARY KEY,
        [UserID]                INT             NOT NULL,
        [CampaignID]            INT             NULL,
        [PromoCodeID]           INT             NULL,
        [VoucherID]             INT             NULL,
        [AmountCredited]        DECIMAL(18,2)   NOT NULL,
        [WalletTransactionID]   INT             NULL,
        [Note]                  NVARCHAR(500)   NULL,
        [RedeemedAt]            DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_Redemptions_User]        FOREIGN KEY ([UserID])               REFERENCES [Users]([UserID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Redemptions_Campaign]    FOREIGN KEY ([CampaignID])           REFERENCES [PromotionCampaigns]([CampaignID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Redemptions_PromoCode]   FOREIGN KEY ([PromoCodeID])          REFERENCES [PromoCodes]([PromoCodeID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Redemptions_Voucher]     FOREIGN KEY ([VoucherID])            REFERENCES [UserVouchers]([VoucherID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Redemptions_Transaction] FOREIGN KEY ([WalletTransactionID])  REFERENCES [WalletTransactions]([WalletTransactionID]) ON DELETE NO ACTION
    );
    -- Ngăn nhận 2 lần cùng auto-trigger (FirstLogin, Birthday...) — chỉ áp dụng khi PromoCodeID IS NULL và VoucherID IS NULL
    CREATE UNIQUE INDEX [UX_Redemptions_User_Campaign_Auto]
        ON [PromotionRedemptions]([UserID], [CampaignID])
        WHERE [PromoCodeID] IS NULL AND [VoucherID] IS NULL;
    PRINT 'Table PromotionRedemptions created.';
END
GO

-- ─────────────────────────────────────────────────────────────
-- 6. Saved Promo Codes (mã user lưu để dùng sau)
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SavedPromoCodes')
BEGIN
    CREATE TABLE [SavedPromoCodes] (
        [SavedCodeID]   INT IDENTITY(1,1) PRIMARY KEY,
        [UserID]        INT             NOT NULL,
        [Code]          NVARCHAR(50)    NOT NULL,
        [SavedAt]       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_SavedPromoCodes_User] FOREIGN KEY ([UserID]) REFERENCES [Users]([UserID]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [UX_SavedPromoCodes_User_Code] ON [SavedPromoCodes]([UserID], [Code]);
    PRINT 'Table SavedPromoCodes created.';
END
GO

-- ─────────────────────────────────────────────────────────────
-- 7. User Notifications (thông báo in-app persistent)
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserNotifications')
BEGIN
    CREATE TABLE [UserNotifications] (
        [NotificationID]    INT IDENTITY(1,1) PRIMARY KEY,
        [UserID]            INT             NOT NULL,
        -- WalletCredit | Promotion | System | Match
        [Type]              NVARCHAR(50)    NOT NULL DEFAULT N'System',
        [Title]             NVARCHAR(200)   NOT NULL,
        [Message]           NVARCHAR(1000)  NOT NULL,
        [IconClass]         NVARCHAR(100)   NULL,   -- material-symbols tên icon
        [ActionUrl]         NVARCHAR(500)   NULL,   -- link khi click
        [IsRead]            BIT             NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_UserNotifications_User] FOREIGN KEY ([UserID]) REFERENCES [Users]([UserID]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_UserNotifications_User_Read] ON [UserNotifications]([UserID], [IsRead], [CreatedAt] DESC);
    PRINT 'Table UserNotifications created.';
END
GO

PRINT '=== SportHub v4 upgrade complete (includes SavedPromoCodes) ===';
GO
