using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyDailyDigest",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyMatchApproved",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyMatchCancelled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyMatchJoinRequest",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyMatchReminder",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyPaymentReminder",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyPromoCode",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyWalletCredit",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyDailyDigest",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyMatchApproved",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyMatchCancelled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyMatchJoinRequest",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyMatchReminder",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyPaymentReminder",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyPromoCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NotifyWalletCredit",
                table: "Users");
        }
    }
}
