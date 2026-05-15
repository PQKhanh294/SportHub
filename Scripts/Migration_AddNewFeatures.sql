-- ============================================================
-- SportHub - Migration Script (dùng khi đã có DB cũ)
-- Chạy script này nếu DB đã tồn tại và cần thêm tính năng mới
-- Nếu tạo DB mới từ đầu → dùng SQL_SportHub.sql (đã bao gồm tất cả)
-- ============================================================

USE SportHubDB;
GO

-- 1. Thêm cột RequiresApproval vào bảng Matches (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM sys.columns 
               WHERE object_id = OBJECT_ID('dbo.Matches') AND name = 'RequiresApproval')
BEGIN
    ALTER TABLE dbo.Matches ADD RequiresApproval BIT NOT NULL DEFAULT 0;
    PRINT 'OK: Added RequiresApproval to Matches';
END
ELSE
    PRINT 'SKIP: RequiresApproval already exists';

-- 2. Tạo bảng UserSportProfiles (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserSportProfiles')
BEGIN
    CREATE TABLE dbo.UserSportProfiles (
        ProfileID       INT             NOT NULL IDENTITY(1,1),
        UserID          INT             NOT NULL,
        SportID         INT             NOT NULL,
        CourtPosition   NVARCHAR(20)    NULL
                            CHECK (CourtPosition IS NULL OR CourtPosition IN ('BackCourt','FrontCourt','AllRound')),
        PlayStyle       NVARCHAR(20)    NULL
                            CHECK (PlayStyle IS NULL OR PlayStyle IN ('Aggressive','Defensive','Balanced')),
        StrokeStrength  NVARCHAR(20)    NULL
                            CHECK (StrokeStrength IS NULL OR StrokeStrength IN ('Smash','Drop','Drive','AllRound')),
        SelfRatedLevel  TINYINT         NULL CHECK (SelfRatedLevel IS NULL OR SelfRatedLevel BETWEEN 1 AND 5),
        ExperienceYears TINYINT         NULL,
        Notes           NVARCHAR(500)   NULL,
        UpdatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_UserSportProfiles PRIMARY KEY (ProfileID),
        CONSTRAINT FK_USP_Users  FOREIGN KEY (UserID)  REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_USP_Sports FOREIGN KEY (SportID) REFERENCES dbo.Sports(SportID),
        CONSTRAINT UQ_UserSportProfiles UNIQUE (UserID, SportID)
    );
    CREATE NONCLUSTERED INDEX IX_UserSportProfiles_User ON dbo.UserSportProfiles (UserID);
    PRINT 'OK: Created UserSportProfiles table';
END
ELSE
    PRINT 'SKIP: UserSportProfiles already exists';

-- 3. Tạo bảng Notifications (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE dbo.Notifications (
        NotificationID  INT             NOT NULL IDENTITY(1,1),
        UserID          INT             NOT NULL,
        Type            NVARCHAR(50)    NOT NULL
                            CHECK (Type IN ('MatchJoin','MatchApprove','MatchReject','BookingConfirmed','BookingCancelled','System')),
        Title           NVARCHAR(200)   NOT NULL,
        Message         NVARCHAR(500)   NOT NULL,
        LinkUrl         NVARCHAR(300)   NULL,
        IsRead          BIT             NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Notifications PRIMARY KEY (NotificationID),
        CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
    );
    CREATE NONCLUSTERED INDEX IX_Notifications_Unread ON dbo.Notifications (UserID, IsRead);
    PRINT 'OK: Created Notifications table';
END
ELSE
    PRINT 'SKIP: Notifications already exists';

PRINT N'Migration completed successfully.';
GO
