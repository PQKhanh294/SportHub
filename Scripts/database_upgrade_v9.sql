-- ============================================================
-- SportHub Database Upgrade v9
-- Xác thực email bằng mã OTP (duyệt 2026-07-04)
--
-- Nội dung:
--   1. Cột Users.EmailConfirmed — email đã xác thực hay chưa (mặc định FALSE
--      cho MỌI user hiện có, kể cả tài khoản cũ dùng mật khẩu — buộc xác thực
--      lại lần đăng nhập tiếp theo, theo yêu cầu sản phẩm)
--   2. Cột Users.EmailVerificationCode / EmailVerificationCodeExpiresAt /
--      EmailVerificationSentAt — mã OTP 6 số, hạn 10 phút, chặn spam gửi lại 60s
--   3. Backfill: tài khoản đã liên kết Google (GoogleId có giá trị) được coi
--      là đã xác thực sẵn (Google đã xác thực email đó) — EmailConfirmed = 1
--   4. Ghi __EFMigrationsHistory tương ứng
--
-- An toàn khi chạy nhiều lần (IF NOT EXISTS / idempotent).
-- Yêu cầu: đã chạy v8.
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '=== SportHub v9 upgrade starting... ===';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Users' AND COLUMN_NAME = N'EmailConfirmed'
)
BEGIN
    ALTER TABLE [Users] ADD [EmailConfirmed] BIT NOT NULL DEFAULT 0;
    PRINT '[1/4] Column Users.EmailConfirmed added (default 0 — bắt buộc xác thực lại).';
END
ELSE
    PRINT '[1/4] Column Users.EmailConfirmed already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Users' AND COLUMN_NAME = N'EmailVerificationCode'
)
BEGIN
    ALTER TABLE [Users] ADD [EmailVerificationCode] NVARCHAR(MAX) NULL;
    PRINT '[2/4] Column Users.EmailVerificationCode added.';
END
ELSE
    PRINT '[2/4] Column Users.EmailVerificationCode already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Users' AND COLUMN_NAME = N'EmailVerificationCodeExpiresAt'
)
BEGIN
    ALTER TABLE [Users] ADD [EmailVerificationCodeExpiresAt] DATETIME2 NULL;
    PRINT '[3/4] Column Users.EmailVerificationCodeExpiresAt added.';
END
ELSE
    PRINT '[3/4] Column Users.EmailVerificationCodeExpiresAt already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Users' AND COLUMN_NAME = N'EmailVerificationSentAt'
)
BEGIN
    ALTER TABLE [Users] ADD [EmailVerificationSentAt] DATETIME2 NULL;
    PRINT '[4/4] Column Users.EmailVerificationSentAt added.';
END
ELSE
    PRINT '[4/4] Column Users.EmailVerificationSentAt already exists — skipped.';
GO

-- Backfill: tài khoản Google coi như đã xác thực email sẵn
UPDATE [Users] SET [EmailConfirmed] = 1 WHERE [GoogleId] IS NOT NULL AND [GoogleId] <> '' AND [EmailConfirmed] = 0;
PRINT '[Backfill] Đã đánh dấu EmailConfirmed=1 cho ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' tài khoản Google.';
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260704194058_AddEmailVerification')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260704194058_AddEmailVerification', '8.0.3');
    PRINT '[History] Migration 20260704194058_AddEmailVerification recorded.';
END
ELSE
    PRINT '[History] Migration already recorded — skipped.';
GO

-- Verify
PRINT '';
PRINT '=== Verification ===';
SELECT COLUMN_NAME, DATA_TYPE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME IN ('EmailConfirmed', 'EmailVerificationCode', 'EmailVerificationCodeExpiresAt', 'EmailVerificationSentAt');

SELECT EmailConfirmed, COUNT(*) AS SoLuong FROM [Users] GROUP BY EmailConfirmed;

SELECT TOP 3 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
GO

PRINT '=== SportHub v9 upgrade complete ===';
GO
