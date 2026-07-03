-- ============================================================
-- SportHub Database Upgrade v6
-- Áp dụng plan fix hệ thống (duyệt 2026-07-03)
--
-- Nội dung:
--   1. [B3] Cột CourtNumber vào Matches (sân số cụ thể)
--   2. [F]  Cột AiScore, AiSummary vào MatchDisputes
--   3. [F]  Bảng DisputeWitnessResponses (xác minh nhân chứng)
--   4. Ghi __EFMigrationsHistory tương ứng (giữ sổ sách đồng bộ với local)
--
-- An toàn khi chạy nhiều lần (IF NOT EXISTS / idempotent).
-- Yêu cầu: đã chạy v5 + reconcile migration history (2026-07-02).
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '=== SportHub v6 upgrade starting... ===';
GO

-- ─────────────────────────────────────────────────────────────
-- 1. [B3] Matches.CourtNumber
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Matches' AND COLUMN_NAME = N'CourtNumber'
)
BEGIN
    ALTER TABLE [Matches] ADD [CourtNumber] NVARCHAR(MAX) NULL;
    PRINT '[B3] Column Matches.CourtNumber added.';
END
ELSE
    PRINT '[B3] Column Matches.CourtNumber already exists — skipped.';
GO

-- ─────────────────────────────────────────────────────────────
-- 2. [F] MatchDisputes.AiScore + AiSummary
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'MatchDisputes' AND COLUMN_NAME = N'AiScore'
)
BEGIN
    ALTER TABLE [MatchDisputes] ADD [AiScore] INT NULL;
    PRINT '[F] Column MatchDisputes.AiScore added.';
END
ELSE
    PRINT '[F] Column MatchDisputes.AiScore already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'MatchDisputes' AND COLUMN_NAME = N'AiSummary'
)
BEGIN
    ALTER TABLE [MatchDisputes] ADD [AiSummary] NVARCHAR(MAX) NULL;
    PRINT '[F] Column MatchDisputes.AiSummary added.';
END
ELSE
    PRINT '[F] Column MatchDisputes.AiSummary already exists — skipped.';
GO

-- ─────────────────────────────────────────────────────────────
-- 3. [F] Bảng DisputeWitnessResponses
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = N'DisputeWitnessResponses'
)
BEGIN
    CREATE TABLE [DisputeWitnessResponses] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [DisputeId]     INT             NOT NULL,
        [WitnessUserId] INT             NOT NULL,
        -- Pending | Support | Oppose | Neutral
        [Stance]        NVARCHAR(MAX)   NOT NULL,
        [Comment]       NVARCHAR(MAX)   NULL,
        [EvidenceUrl]   NVARCHAR(MAX)   NULL,
        [RespondedAt]   DATETIME2       NULL,
        [CreatedAt]     DATETIME2       NOT NULL,

        CONSTRAINT [PK_DisputeWitnessResponses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DisputeWitnessResponses_MatchDisputes_DisputeId]
            FOREIGN KEY ([DisputeId]) REFERENCES [MatchDisputes]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DisputeWitnessResponses_Users_WitnessUserId]
            FOREIGN KEY ([WitnessUserId]) REFERENCES [Users]([UserID]) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_DisputeWitnessResponses_DisputeId_WitnessUserId]
        ON [DisputeWitnessResponses]([DisputeId], [WitnessUserId]);

    CREATE INDEX [IX_DisputeWitnessResponses_WitnessUserId]
        ON [DisputeWitnessResponses]([WitnessUserId]);

    PRINT '[F] Table DisputeWitnessResponses created.';
END
ELSE
    PRINT '[F] Table DisputeWitnessResponses already exists — skipped.';
GO

-- ─────────────────────────────────────────────────────────────
-- 4. Ghi migration history (đồng bộ sổ sách với local, tránh
--    Database.Migrate() chạy lại lúc deploy)
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260703130537_AddCourtNumberAndDisputeSystem')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260703130537_AddCourtNumberAndDisputeSystem', '8.0.3');
    PRINT '[History] Migration 20260703130537_AddCourtNumberAndDisputeSystem recorded.';
END
ELSE
    PRINT '[History] Migration already recorded — skipped.';
GO

-- ─────────────────────────────────────────────────────────────
-- Verify
-- ─────────────────────────────────────────────────────────────
PRINT '';
PRINT '=== Verification ===';

SELECT 'Matches.CourtNumber' AS Check_Item, COUNT(*) AS Found
FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Matches' AND COLUMN_NAME = 'CourtNumber';

SELECT 'MatchDisputes AI cols' AS Check_Item, COUNT(*) AS Found
FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'MatchDisputes' AND COLUMN_NAME IN ('AiScore','AiSummary');

SELECT 'DisputeWitnessResponses' AS Check_Item, COUNT(*) AS Found
FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DisputeWitnessResponses';

SELECT TOP 3 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
GO

PRINT '=== SportHub v6 upgrade complete ===';
GO
