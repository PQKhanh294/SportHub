using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchDisputesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Table already exists if database_upgrade_v5.sql was run — guard with IF NOT EXISTS
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MatchDisputes')
BEGIN
    CREATE TABLE [MatchDisputes] (
        [Id]           INT IDENTITY(1,1) PRIMARY KEY,
        [MatchId]      INT             NOT NULL,
        [ReporterId]   INT             NOT NULL,
        [DisputeType]  NVARCHAR(50)    NOT NULL,
        [Description]  NVARCHAR(1000)  NOT NULL,
        [Status]       NVARCHAR(20)    NOT NULL DEFAULT N'Pending',
        [AdminNote]    NVARCHAR(500)   NULL,
        [Resolution]   NVARCHAR(30)    NULL,
        [RefundAmount] DECIMAL(18,2)   NULL,
        [CreatedAt]    DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [ResolvedAt]   DATETIME2       NULL,
        CONSTRAINT [FK_MatchDisputes_Matches_MatchId] FOREIGN KEY ([MatchId]) REFERENCES [Matches]([MatchID]) ON DELETE CASCADE,
        CONSTRAINT [FK_MatchDisputes_Users_ReporterId] FOREIGN KEY ([ReporterId]) REFERENCES [Users]([UserID]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_MatchDisputes_MatchId] ON [MatchDisputes]([MatchId]);
    CREATE INDEX [IX_MatchDisputes_ReporterId] ON [MatchDisputes]([ReporterId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MatchDisputes");
        }
    }
}
