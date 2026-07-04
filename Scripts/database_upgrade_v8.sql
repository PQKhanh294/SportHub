-- ============================================================
-- SportHub Database Upgrade v8
-- Chức năng quên mật khẩu (duyệt 2026-07-04)
--
-- Nội dung:
--   1. Cột Users.PasswordResetToken — token đặt lại mật khẩu (nullable)
--   2. Cột Users.PasswordResetTokenExpiresAt — hạn dùng token, 1 giờ (nullable)
--   3. Ghi __EFMigrationsHistory tương ứng
--
-- An toàn khi chạy nhiều lần (IF NOT EXISTS / idempotent).
-- Yêu cầu: đã chạy v7.
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '=== SportHub v8 upgrade starting... ===';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Users' AND COLUMN_NAME = N'PasswordResetToken'
)
BEGIN
    ALTER TABLE [Users] ADD [PasswordResetToken] NVARCHAR(MAX) NULL;
    PRINT '[1/2] Column Users.PasswordResetToken added.';
END
ELSE
    PRINT '[1/2] Column Users.PasswordResetToken already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Users' AND COLUMN_NAME = N'PasswordResetTokenExpiresAt'
)
BEGIN
    ALTER TABLE [Users] ADD [PasswordResetTokenExpiresAt] DATETIME2 NULL;
    PRINT '[2/2] Column Users.PasswordResetTokenExpiresAt added.';
END
ELSE
    PRINT '[2/2] Column Users.PasswordResetTokenExpiresAt already exists — skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260704192101_AddUserPasswordResetToken')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260704192101_AddUserPasswordResetToken', '8.0.3');
    PRINT '[History] Migration 20260704192101_AddUserPasswordResetToken recorded.';
END
ELSE
    PRINT '[History] Migration already recorded — skipped.';
GO

-- Verify
PRINT '';
PRINT '=== Verification ===';
SELECT COLUMN_NAME, DATA_TYPE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME IN ('PasswordResetToken', 'PasswordResetTokenExpiresAt');

SELECT TOP 3 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
GO

PRINT '=== SportHub v8 upgrade complete ===';
GO
