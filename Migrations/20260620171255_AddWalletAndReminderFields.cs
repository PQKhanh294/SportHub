using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletAndReminderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "Users",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "MatchPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    WalletTransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedMatchID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.WalletTransactionID);
                    table.CheckConstraint("CK_WalletTransactions_Type", "Type IN ('Refund','Deduction','AdminCredit')");
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat','MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed','MatchCompleted','MatchReviewReminder','RemainingFeeReminder','WalletCredit')");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_UserID",
                table: "WalletTransactions",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "MatchPayments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat','MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed','MatchCompleted','MatchReviewReminder')");
        }
    }
}
