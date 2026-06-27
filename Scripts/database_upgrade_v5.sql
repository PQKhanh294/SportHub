-- ============================================================
-- SportHub Database Upgrade v5
-- Áp dụng các thay đổi từ Plan A1-C5 (duyệt 2026-06-27)
--
-- Nội dung:
--   1.  [A3]  Unique index chống double-claim mã khuyến mãi
--   2.  [B1]  Cột IsLockedByHost, CancelReason, CancelledAt vào Matches
--   3.  [B2]  Cột NotifyByEmail vào Users
--   4.  [B3]  Bảng MatchDisputes (hệ thống tranh chấp)
--   5.  [B3+] Cột EvidenceUrl vào MatchDisputes
--   6.  [C2]  Cột ReadAt vào ChatMessages (read receipts)
--   7.  [C1]  B-tree indexes tối ưu query thường dùng
--   8.  [C1]  SQL Server Full-Text Search catalog + indexes
--
-- An toàn khi chạy nhiều lần (IF NOT EXISTS / idempotent).
-- Yêu cầu: Database đã chạy upgrade v4.
-- ============================================================

-- Filtered indexes yêu cầu QUOTED_IDENTIFIER ON
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '=== SportHub v5 upgrade starting... ===';
GO


-- ─────────────────────────────────────────────────────────────
-- 1. [A3] Unique index PromotionRedemptions — chống double-claim PromoCode
--    Bổ sung bên cạnh UX_Redemptions_User_Campaign_Auto đã có từ v4
--    (cái đó chỉ chặn auto-trigger, không chặn PromoCode)
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_Redemptions_User_PromoCode'
      AND object_id = OBJECT_ID(N'PromotionRedemptions')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Redemptions_User_PromoCode]
        ON [PromotionRedemptions]([UserID], [PromoCodeID])
        WHERE [PromoCodeID] IS NOT NULL;
    PRINT '[A3] Index UX_Redemptions_User_PromoCode created.';
END
ELSE
    PRINT '[A3] Index UX_Redemptions_User_PromoCode already exists — skipped.';
GO


-- ─────────────────────────────────────────────────────────────
-- 2. [B1] Host khóa / hủy trận — thêm 3 cột vào Matches
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Matches' AND COLUMN_NAME = N'IsLockedByHost'
)
BEGIN
    ALTER TABLE [Matches] ADD [IsLockedByHost] BIT NOT NULL DEFAULT 0;
    PRINT '[B1] Column Matches.IsLockedByHost added.';
END
ELSE
    PRINT '[B1] Column Matches.IsLockedByHost already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Matches' AND COLUMN_NAME = N'CancelReason'
)
BEGIN
    ALTER TABLE [Matches] ADD [CancelReason] NVARCHAR(500) NULL;
    PRINT '[B1] Column Matches.CancelReason added.';
END
ELSE
    PRINT '[B1] Column Matches.CancelReason already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Matches' AND COLUMN_NAME = N'CancelledAt'
)
BEGIN
    ALTER TABLE [Matches] ADD [CancelledAt] DATETIME2 NULL;
    PRINT '[B1] Column Matches.CancelledAt added.';
END
ELSE
    PRINT '[B1] Column Matches.CancelledAt already exists — skipped.';
GO


-- ─────────────────────────────────────────────────────────────
-- 3. [B2] Email notification setting — thêm cột vào Users
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Users' AND COLUMN_NAME = N'NotifyByEmail'
)
BEGIN
    ALTER TABLE [Users] ADD [NotifyByEmail] BIT NOT NULL DEFAULT 1;
    PRINT '[B2] Column Users.NotifyByEmail added (default ON for all users).';
END
ELSE
    PRINT '[B2] Column Users.NotifyByEmail already exists — skipped.';
GO


-- ─────────────────────────────────────────────────────────────
-- 4. [B3] Bảng MatchDisputes — hệ thống báo cáo tranh chấp
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = N'MatchDisputes'
)
BEGIN
    CREATE TABLE [MatchDisputes] (
        [Id]            INT IDENTITY(1,1) PRIMARY KEY,
        [MatchId]       INT             NOT NULL,
        [ReporterId]    INT             NOT NULL,
        -- HostNoShow | PlayerNoShow | WrongVenue | QualityIssue | PaymentDispute
        [DisputeType]   NVARCHAR(50)    NOT NULL,
        [Description]   NVARCHAR(1000)  NOT NULL,
        -- Pending | UnderReview | AutoResolved | AdminResolved | Dismissed
        [Status]        NVARCHAR(20)    NOT NULL DEFAULT N'Pending',
        [AdminNote]     NVARCHAR(500)   NULL,
        -- FullRefund | PartialRefund | NoRefund | Warning
        [Resolution]    NVARCHAR(30)    NULL,
        [RefundAmount]  DECIMAL(18,2)   NULL,
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [ResolvedAt]    DATETIME2       NULL,

        CONSTRAINT [FK_MatchDisputes_Match]
            FOREIGN KEY ([MatchId])    REFERENCES [Matches]([MatchID])  ON DELETE CASCADE,
        CONSTRAINT [FK_MatchDisputes_Reporter]
            FOREIGN KEY ([ReporterId]) REFERENCES [Users]([UserID])     ON DELETE NO ACTION
    );

    -- Index để admin lọc theo Status nhanh
    CREATE INDEX [IX_MatchDisputes_Status_Created]
        ON [MatchDisputes]([Status], [CreatedAt] DESC);

    -- Index để lấy tranh chấp theo trận
    CREATE INDEX [IX_MatchDisputes_MatchId]
        ON [MatchDisputes]([MatchId]);

    PRINT '[B3] Table MatchDisputes created.';
END
ELSE
    PRINT '[B3] Table MatchDisputes already exists — skipped.';
GO


-- ─────────────────────────────────────────────────────────────
-- 5. [B3+] Cột EvidenceUrl vào MatchDisputes
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'MatchDisputes' AND COLUMN_NAME = N'EvidenceUrl'
)
BEGIN
    ALTER TABLE [MatchDisputes] ADD [EvidenceUrl] NVARCHAR(MAX) NULL;
    PRINT '[B3+] Column MatchDisputes.EvidenceUrl added.';
END
ELSE
    PRINT '[B3+] Column MatchDisputes.EvidenceUrl already exists — skipped.';
GO


-- ─────────────────────────────────────────────────────────────
-- 6. [C2] Cột ReadAt vào ChatMessages (chat read receipts)
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'ChatMessages' AND COLUMN_NAME = N'ReadAt'
)
BEGIN
    ALTER TABLE [ChatMessages] ADD [ReadAt] DATETIME2 NULL;
    PRINT '[C2] Column ChatMessages.ReadAt added.';
END
ELSE
    PRINT '[C2] Column ChatMessages.ReadAt already exists — skipped.';
GO


-- ─────────────────────────────────────────────────────────────
-- 7. [C1] B-tree indexes — tối ưu các query thường dùng
--    Tất cả dùng IF NOT EXISTS để idempotent
-- ─────────────────────────────────────────────────────────────

-- Matches: lọc theo Status + MatchDate (trang Ghép trận)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Matches_Status_MatchDate'
      AND object_id = OBJECT_ID(N'Matches')
)
BEGIN
    CREATE INDEX [IX_Matches_Status_MatchDate]
        ON [Matches]([Status], [MatchDate]);
    PRINT '[C1] Index IX_Matches_Status_MatchDate created.';
END
ELSE
    PRINT '[C1] Index IX_Matches_Status_MatchDate already exists — skipped.';
GO

-- Matches: lọc theo SportID
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Matches_SportID'
      AND object_id = OBJECT_ID(N'Matches')
)
BEGIN
    CREATE INDEX [IX_Matches_SportID]
        ON [Matches]([SportID]);
    PRINT '[C1] Index IX_Matches_SportID created.';
END
ELSE
    PRINT '[C1] Index IX_Matches_SportID already exists — skipped.';
GO

-- Matches: lấy theo host (trang quản lý trận của host)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Matches_CreatedByUserID'
      AND object_id = OBJECT_ID(N'Matches')
)
BEGIN
    CREATE INDEX [IX_Matches_CreatedByUserID]
        ON [Matches]([CreatedByUserID]);
    PRINT '[C1] Index IX_Matches_CreatedByUserID created.';
END
ELSE
    PRINT '[C1] Index IX_Matches_CreatedByUserID already exists — skipped.';
GO

-- MatchParticipants: lấy trận của 1 user
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_MatchParticipants_UserID'
      AND object_id = OBJECT_ID(N'MatchParticipants')
)
BEGIN
    CREATE INDEX [IX_MatchParticipants_UserID]
        ON [MatchParticipants]([UserID]);
    PRINT '[C1] Index IX_MatchParticipants_UserID created.';
END
ELSE
    PRINT '[C1] Index IX_MatchParticipants_UserID already exists — skipped.';
GO

-- WalletTransactions: lịch sử giao dịch theo user + thời gian (trang Ví)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_WalletTransactions_UserID_CreatedAt'
      AND object_id = OBJECT_ID(N'WalletTransactions')
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_UserID_CreatedAt]
        ON [WalletTransactions]([UserID], [CreatedAt] DESC);
    PRINT '[C1] Index IX_WalletTransactions_UserID_CreatedAt created.';
END
ELSE
    PRINT '[C1] Index IX_WalletTransactions_UserID_CreatedAt already exists — skipped.';
GO

-- Notifications: thông báo chưa đọc theo user (badge count trên navbar)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Notifications_UserID_IsRead'
      AND object_id = OBJECT_ID(N'Notifications')
)
BEGIN
    CREATE INDEX [IX_Notifications_UserID_IsRead]
        ON [Notifications]([UserID], [IsRead], [CreatedAt] DESC);
    PRINT '[C1] Index IX_Notifications_UserID_IsRead created.';
END
ELSE
    PRINT '[C1] Index IX_Notifications_UserID_IsRead already exists — skipped.';
GO

-- ChatMessages: load conversation giữa 2 user
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ChatMessages_Sender_Receiver'
      AND object_id = OBJECT_ID(N'ChatMessages')
)
BEGIN
    CREATE INDEX [IX_ChatMessages_Sender_Receiver]
        ON [ChatMessages]([SenderID], [ReceiverID], [CreatedAt]);
    PRINT '[C1] Index IX_ChatMessages_Sender_Receiver created.';
END
ELSE
    PRINT '[C1] Index IX_ChatMessages_Sender_Receiver already exists — skipped.';
GO

-- ChatMessages: đếm tin chưa đọc theo người nhận
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ChatMessages_ReceiverID_IsRead'
      AND object_id = OBJECT_ID(N'ChatMessages')
)
BEGIN
    CREATE INDEX [IX_ChatMessages_ReceiverID_IsRead]
        ON [ChatMessages]([ReceiverID], [IsRead]);
    PRINT '[C1] Index IX_ChatMessages_ReceiverID_IsRead created.';
END
ELSE
    PRINT '[C1] Index IX_ChatMessages_ReceiverID_IsRead already exists — skipped.';
GO

-- PromotionRedemptions: kiểm tra user đã nhận campaign chưa (dùng trong distribute)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PromotionRedemptions_UserID_CampaignID'
      AND object_id = OBJECT_ID(N'PromotionRedemptions')
)
BEGIN
    CREATE INDEX [IX_PromotionRedemptions_UserID_CampaignID]
        ON [PromotionRedemptions]([UserID], [CampaignID]);
    PRINT '[C1] Index IX_PromotionRedemptions_UserID_CampaignID created.';
END
ELSE
    PRINT '[C1] Index IX_PromotionRedemptions_UserID_CampaignID already exists — skipped.';
GO

-- WalletTopUpRequests: tra cứu request theo ref (webhook lookup)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_WalletTopUpRequests_TransactionRef'
      AND object_id = OBJECT_ID(N'WalletTopUpRequests')
)
BEGIN
    CREATE INDEX [IX_WalletTopUpRequests_TransactionRef]
        ON [WalletTopUpRequests]([TransactionRef]);
    PRINT '[C1] Index IX_WalletTopUpRequests_TransactionRef created.';
END
ELSE
    PRINT '[C1] Index IX_WalletTopUpRequests_TransactionRef already exists — skipped.';
GO


-- ─────────────────────────────────────────────────────────────
-- 8. [C1] SQL Server Full-Text Search
--    Tự động bỏ qua nếu FTS feature chưa được cài trên server
-- ─────────────────────────────────────────────────────────────
IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
BEGIN
    -- Tạo FTS Catalog nếu chưa có
    IF NOT EXISTS (
        SELECT 1 FROM sys.fulltext_catalogs
        WHERE name = N'SportHubCatalog'
    )
    BEGIN
        CREATE FULLTEXT CATALOG [SportHubCatalog] AS DEFAULT;
        PRINT '[C1] Full-Text Catalog SportHubCatalog created.';
    END
    ELSE
        PRINT '[C1] Full-Text Catalog SportHubCatalog already exists — skipped.';

    -- FTS Index trên Matches
    IF NOT EXISTS (
        SELECT 1 FROM sys.fulltext_indexes
        WHERE object_id = OBJECT_ID(N'Matches')
    )
    BEGIN
        CREATE FULLTEXT INDEX ON [Matches](
            [Title]             LANGUAGE 1066,   -- Vietnamese
            [Description]       LANGUAGE 1066,
            [CustomCourtName]   LANGUAGE 1066,
            [CustomCourtAddress] LANGUAGE 1066
        )
        KEY INDEX [PK_Matches]
        ON [SportHubCatalog]
        WITH CHANGE_TRACKING AUTO;
        PRINT '[C1] Full-Text Index on Matches created.';
    END
    ELSE
        PRINT '[C1] Full-Text Index on Matches already exists — skipped.';

    -- FTS Index trên Users (tìm kiếm theo tên)
    IF NOT EXISTS (
        SELECT 1 FROM sys.fulltext_indexes
        WHERE object_id = OBJECT_ID(N'Users')
    )
    BEGIN
        CREATE FULLTEXT INDEX ON [Users](
            [FullName] LANGUAGE 1066
        )
        KEY INDEX [PK_Users]
        ON [SportHubCatalog]
        WITH CHANGE_TRACKING AUTO;
        PRINT '[C1] Full-Text Index on Users created.';
    END
    ELSE
        PRINT '[C1] Full-Text Index on Users already exists — skipped.';
END
ELSE
BEGIN
    PRINT '[C1] Full-Text Search not installed on this SQL Server — FTS indexes skipped.';
    PRINT '     To enable: SQL Server Installation Center > Add features > Full-Text Search';
END
GO


-- ─────────────────────────────────────────────────────────────
-- Verify kết quả
-- ─────────────────────────────────────────────────────────────
PRINT '';
PRINT '=== Verification ===';

-- Kiểm tra các cột mới
SELECT
    'Matches columns' AS Check_Item,
    COUNT(*) AS Found
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Matches'
  AND COLUMN_NAME IN ('IsLockedByHost', 'CancelReason', 'CancelledAt');

SELECT
    'Users.NotifyByEmail' AS Check_Item,
    COUNT(*) AS Found
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'NotifyByEmail';

SELECT
    'MatchDisputes table' AS Check_Item,
    COUNT(*) AS Found
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'MatchDisputes';

SELECT
    'Unique index promo' AS Check_Item,
    COUNT(*) AS Found
FROM sys.indexes
WHERE name = 'UX_Redemptions_User_PromoCode';

SELECT
    'B-tree indexes added' AS Check_Item,
    COUNT(*) AS Found
FROM sys.indexes
WHERE name IN (
    'IX_Matches_Status_MatchDate',
    'IX_Matches_SportID',
    'IX_Matches_CreatedByUserID',
    'IX_MatchParticipants_UserID',
    'IX_WalletTransactions_UserID_CreatedAt',
    'IX_ChatMessages_Sender_Receiver',
    'IX_ChatMessages_ReceiverID_IsRead',
    'IX_PromotionRedemptions_UserID_CampaignID',
    'IX_WalletTopUpRequests_TransactionRef'
);
GO

PRINT '=== SportHub v5 upgrade complete ===';
PRINT 'Next: Chạy EF migrations tương ứng để C# model đồng bộ với schema.';
PRINT '      /migrate AddMatchCancelAndLockFields';
PRINT '      /migrate AddMatchDisputesTable';
PRINT '      /migrate AddUserNotifyByEmail';
GO
