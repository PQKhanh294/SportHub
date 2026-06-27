using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchHostCancelAndLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columns already exist if database_upgrade_v5.sql was run — guard with IF NOT EXISTS
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Matches' AND COLUMN_NAME = 'CancelReason')
    ALTER TABLE [Matches] ADD [CancelReason] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Matches' AND COLUMN_NAME = 'CancelledAt')
    ALTER TABLE [Matches] ADD [CancelledAt] DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Matches' AND COLUMN_NAME = 'IsLockedByHost')
    ALTER TABLE [Matches] ADD [IsLockedByHost] BIT NOT NULL DEFAULT 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CancelReason",  table: "Matches");
            migrationBuilder.DropColumn(name: "CancelledAt",   table: "Matches");
            migrationBuilder.DropColumn(name: "IsLockedByHost", table: "Matches");
        }
    }
}
