using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchPaymentFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MatchParticipants_JoinStatus",
                table: "MatchParticipants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_Status",
                table: "Matches");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlayerFeeDeadline",
                table: "MatchParticipants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlayerFeeReceiptUrl",
                table: "MatchParticipants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlayerFeeStatus",
                table: "MatchParticipants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositStatus",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "NotPaid");

            migrationBuilder.AddColumn<string>(
                name: "RemainingFeeStatus",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "NotDue");

            migrationBuilder.CreateTable(
                name: "MatchPayments",
                columns: table => new
                {
                    MatchPaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchID = table.Column<int>(type: "int", nullable: false),
                    PayerUserID = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReceiptUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPayments", x => x.MatchPaymentID);
                    table.CheckConstraint("CK_MatchPayments_Status", "Status IN ('Pending','Confirmed','Expired','Refunded')");
                    table.CheckConstraint("CK_MatchPayments_Type", "PaymentType IN ('HostDeposit','HostRemaining','PlayerFee')");
                    table.ForeignKey(
                        name: "FK_MatchPayments_Matches_MatchID",
                        column: x => x.MatchID,
                        principalTable: "Matches",
                        principalColumn: "MatchID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchPayments_Users_PayerUserID",
                        column: x => x.PayerUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat','MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MatchParticipants_JoinStatus",
                table: "MatchParticipants",
                sql: "JoinStatus IN ('Pending','Approved','Accepted','Declined','Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MatchParticipants_PlayerFeeStatus",
                table: "MatchParticipants",
                sql: "PlayerFeeStatus IS NULL OR PlayerFeeStatus IN ('AwaitingPayment','Paid','Expired')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_DepositStatus",
                table: "Matches",
                sql: "DepositStatus IN ('NotPaid','Paid')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_RemainingFeeStatus",
                table: "Matches",
                sql: "RemainingFeeStatus IN ('NotDue','Notified','Paid')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_Status",
                table: "Matches",
                sql: "Status IN ('Open','Full','InProgress','Completed','Cancelled','PendingDeposit')");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPayments_MatchID_PaymentType_Status",
                table: "MatchPayments",
                columns: new[] { "MatchID", "PaymentType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchPayments_PayerUserID",
                table: "MatchPayments",
                column: "PayerUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MatchParticipants_JoinStatus",
                table: "MatchParticipants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MatchParticipants_PlayerFeeStatus",
                table: "MatchParticipants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_DepositStatus",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_RemainingFeeStatus",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Matches_Status",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PlayerFeeDeadline",
                table: "MatchParticipants");

            migrationBuilder.DropColumn(
                name: "PlayerFeeReceiptUrl",
                table: "MatchParticipants");

            migrationBuilder.DropColumn(
                name: "PlayerFeeStatus",
                table: "MatchParticipants");

            migrationBuilder.DropColumn(
                name: "DepositStatus",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RemainingFeeStatus",
                table: "Matches");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MatchParticipants_JoinStatus",
                table: "MatchParticipants",
                sql: "JoinStatus IN ('Pending','Accepted','Declined','Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Matches_Status",
                table: "Matches",
                sql: "Status IN ('Open','Full','InProgress','Completed','Cancelled')");
        }
    }
}
