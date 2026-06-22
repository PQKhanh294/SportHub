using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTopUpRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WalletTransactions_Type",
                table: "WalletTransactions");

            migrationBuilder.CreateTable(
                name: "WalletTopUpRequests",
                columns: table => new
                {
                    WalletTopUpID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TransactionRef = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTopUpRequests", x => x.WalletTopUpID);
                    table.CheckConstraint("CK_WalletTopUpRequests_Status", "Status IN ('Pending','Confirmed','Expired')");
                    table.ForeignKey(
                        name: "FK_WalletTopUpRequests_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_WalletTransactions_Type",
                table: "WalletTransactions",
                sql: "Type IN ('Refund','Deduction','AdminCredit','TopUp','MatchPayment')");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUpRequests_TransactionRef",
                table: "WalletTopUpRequests",
                column: "TransactionRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUpRequests_UserID_Status",
                table: "WalletTopUpRequests",
                columns: new[] { "UserID", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTopUpRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WalletTransactions_Type",
                table: "WalletTransactions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WalletTransactions_Type",
                table: "WalletTransactions",
                sql: "Type IN ('Refund','Deduction','AdminCredit')");
        }
    }
}
