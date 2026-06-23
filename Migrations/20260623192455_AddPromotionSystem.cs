using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WalletTransactions_Type",
                table: "WalletTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.CreateTable(
                name: "PromotionCampaigns",
                columns: table => new
                {
                    CampaignID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApplicableScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxRedemptions = table.Column<int>(type: "int", nullable: true),
                    RedemptionCount = table.Column<int>(type: "int", nullable: false),
                    CreatedByAdminID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCampaigns", x => x.CampaignID);
                    table.ForeignKey(
                        name: "FK_PromotionCampaigns_Users_CreatedByAdminID",
                        column: x => x.CreatedByAdminID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    PromoCodeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignID = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: true),
                    UseCount = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodes", x => x.PromoCodeID);
                    table.ForeignKey(
                        name: "FK_PromoCodes_PromotionCampaigns_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "PromotionCampaigns",
                        principalColumn: "CampaignID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserVouchers",
                columns: table => new
                {
                    VoucherID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CampaignID = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuedByAdminID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVouchers", x => x.VoucherID);
                    table.ForeignKey(
                        name: "FK_UserVouchers_PromotionCampaigns_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "PromotionCampaigns",
                        principalColumn: "CampaignID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserVouchers_Users_IssuedByAdminID",
                        column: x => x.IssuedByAdminID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_UserVouchers_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionRedemptions",
                columns: table => new
                {
                    RedemptionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CampaignID = table.Column<int>(type: "int", nullable: true),
                    PromoCodeID = table.Column<int>(type: "int", nullable: true),
                    VoucherID = table.Column<int>(type: "int", nullable: true),
                    AmountCredited = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WalletTransactionID = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRedemptions", x => x.RedemptionID);
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_PromoCodes_PromoCodeID",
                        column: x => x.PromoCodeID,
                        principalTable: "PromoCodes",
                        principalColumn: "PromoCodeID");
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_PromotionCampaigns_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "PromotionCampaigns",
                        principalColumn: "CampaignID");
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_UserVouchers_VoucherID",
                        column: x => x.VoucherID,
                        principalTable: "UserVouchers",
                        principalColumn: "VoucherID");
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_WalletTransactions_WalletTransactionID",
                        column: x => x.WalletTransactionID,
                        principalTable: "WalletTransactions",
                        principalColumn: "WalletTransactionID");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_WalletTransactions_Type",
                table: "WalletTransactions",
                sql: "Type IN ('Refund','Deduction','AdminCredit','TopUp','MatchPayment','Promotion')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat','MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed','MatchCompleted','MatchReviewReminder','RemainingFeeReminder','WalletCredit','Promotion','AdminCredit','Voucher')");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_CampaignID",
                table: "PromoCodes",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_Code",
                table: "PromoCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaigns_CreatedByAdminID",
                table: "PromotionCampaigns",
                column: "CreatedByAdminID");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_CampaignID",
                table: "PromotionRedemptions",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_PromoCodeID",
                table: "PromotionRedemptions",
                column: "PromoCodeID");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_UserID_CampaignID",
                table: "PromotionRedemptions",
                columns: new[] { "UserID", "CampaignID" },
                unique: true,
                filter: "[PromoCodeID] IS NULL AND [VoucherID] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_VoucherID",
                table: "PromotionRedemptions",
                column: "VoucherID");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_WalletTransactionID",
                table: "PromotionRedemptions",
                column: "WalletTransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_CampaignID",
                table: "UserVouchers",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_Code",
                table: "UserVouchers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_IssuedByAdminID",
                table: "UserVouchers",
                column: "IssuedByAdminID");

            migrationBuilder.CreateIndex(
                name: "IX_UserVouchers_UserID",
                table: "UserVouchers",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionRedemptions");

            migrationBuilder.DropTable(
                name: "PromoCodes");

            migrationBuilder.DropTable(
                name: "UserVouchers");

            migrationBuilder.DropTable(
                name: "PromotionCampaigns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WalletTransactions_Type",
                table: "WalletTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WalletTransactions_Type",
                table: "WalletTransactions",
                sql: "Type IN ('Refund','Deduction','AdminCredit','TopUp','MatchPayment')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat','MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed','MatchCompleted','MatchReviewReminder','RemainingFeeReminder','WalletCredit')");
        }
    }
}
