using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_MatchType",
                table: "Matches");

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSplitFee",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentMatchId",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecurringDays",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecurringUntil",
                table: "Matches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ParentMatchId",
                table: "Matches",
                column: "ParentMatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Matches_ParentMatchId",
                table: "Matches",
                column: "ParentMatchId",
                principalTable: "Matches",
                principalColumn: "MatchID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Matches_ParentMatchId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_ParentMatchId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsSplitFee",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ParentMatchId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RecurringDays",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RecurringUntil",
                table: "Matches");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_MatchType",
                table: "Matches",
                sql: "MatchType IN ('Singles','Doubles','Mixed')");
        }
    }
}
