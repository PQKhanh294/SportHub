-- Migration: add SkillLevel to UserSportProfiles if missing
USE SportHubDB;
GO

IF COL_LENGTH('dbo.UserSportProfiles', 'SkillLevel') IS NULL
BEGIN
    ALTER TABLE dbo.UserSportProfiles ADD SkillLevel NVARCHAR(50) NULL;
    PRINT 'OK: Added SkillLevel to UserSportProfiles';
END
ELSE
    PRINT 'SKIP: SkillLevel already exists in UserSportProfiles';
GO
