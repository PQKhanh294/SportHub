-- ============================================================
-- SportHub Database Upgrade v7
-- Quyền riêng tư liên hệ (duyệt 2026-07-04)
--
-- Nội dung:
--   1. Cột Users.ShowContactToTeammates — cho phép user tự ẩn SĐT/Zalo
--      khỏi người chơi cùng trận. Mặc định TRUE (giữ nguyên hành vi hiện
--      tại — hiện liên hệ — cho tới khi user tự tắt trong Settings).
--   2. Ghi __EFMigrationsHistory tương ứng
--
-- An toàn khi chạy nhiều lần (IF NOT EXISTS / idempotent).
-- Yêu cầu: đã chạy v6.
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '=== SportHub v7 upgrade starting... ===';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Users' AND COLUMN_NAME = N'ShowContactToTeammates'
)
BEGIN
    ALTER TABLE [Users] ADD [ShowContactToTeammates] BIT NOT NULL DEFAULT 1;
    PRINT '[1/1] Column Users.ShowContactToTeammates added (default 1 — giữ nguyên hành vi cũ).';
END
ELSE
    PRINT '[1/1] Column Users.ShowContactToTeammates already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260704184453_AddUserPrivacyToggle')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260704184453_AddUserPrivacyToggle', '8.0.3');
    PRINT '[History] Migration 20260704184453_AddUserPrivacyToggle recorded.';
END
ELSE
    PRINT '[History] Migration already recorded — skipped.';
GO

-- Verify
PRINT '';
PRINT '=== Verification ===';
SELECT COLUMN_NAME, DATA_TYPE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'ShowContactToTeammates';

SELECT TOP 3 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
GO

PRINT '=== SportHub v7 upgrade complete ===';
GO
