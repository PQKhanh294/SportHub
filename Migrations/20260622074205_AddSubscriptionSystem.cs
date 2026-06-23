using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionOrders",
                columns: table => new
                {
                    SubscriptionOrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    PlanKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TransactionRef = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionOrders", x => x.SubscriptionOrderID);
                    table.CheckConstraint("CK_SubscriptionOrders_Status", "Status IN ('Pending','Confirmed','Expired')");
                    table.ForeignKey(
                        name: "FK_SubscriptionOrders_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    PlanID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriceMonthly = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    PriceQuarterly = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    PriceAnnual = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MonthlyJoinLimit = table.Column<int>(type: "int", nullable: false),
                    MonthlyCreateLimit = table.Column<int>(type: "int", nullable: false),
                    CanSeePhoneNumber = table.Column<bool>(type: "bit", nullable: false),
                    CanFilterByDistance = table.Column<bool>(type: "bit", nullable: false),
                    HasAiSuggestions = table.Column<bool>(type: "bit", nullable: false),
                    HasDetailedStats = table.Column<bool>(type: "bit", nullable: false),
                    HasPriorityListing = table.Column<bool>(type: "bit", nullable: false),
                    PriorityScore = table.Column<int>(type: "int", nullable: false),
                    HasVerifiedBadge = table.Column<bool>(type: "bit", nullable: false),
                    HasPlayerFeeExempt = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.PlanID);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionUsages",
                columns: table => new
                {
                    SubscriptionUsageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    YearMonth = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JoinCount = table.Column<int>(type: "int", nullable: false),
                    CreateCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionUsages", x => x.SubscriptionUsageID);
                    table.ForeignKey(
                        name: "FK_SubscriptionUsages_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserMatchCredits",
                columns: table => new
                {
                    UserMatchCreditID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    RemainingCredits = table.Column<int>(type: "int", nullable: false),
                    TransactionRef = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMatchCredits", x => x.UserMatchCreditID);
                    table.CheckConstraint("CK_UserMatchCredits_Status", "Status IN ('Pending','Confirmed')");
                    table.ForeignKey(
                        name: "FK_UserMatchCredits_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    UserSubscriptionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    PlanKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.UserSubscriptionID);
                    table.CheckConstraint("CK_UserSubscriptions_BillingCycle", "BillingCycle IN ('Monthly','Quarterly','Annual','Trial')");
                    table.CheckConstraint("CK_UserSubscriptions_Status", "Status IN ('Active','Expired','Cancelled')");
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "PlanID", "CanFilterByDistance", "CanSeePhoneNumber", "Description", "HasAiSuggestions", "HasDetailedStats", "HasPlayerFeeExempt", "HasPriorityListing", "HasVerifiedBadge", "IsActive", "MonthlyCreateLimit", "MonthlyJoinLimit", "Name", "PlanKey", "PriceAnnual", "PriceMonthly", "PriceQuarterly", "PriorityScore", "SortOrder" },
                values: new object[,]
                {
                    { 1, false, false, "Dành cho người mới bắt đầu", false, false, false, false, false, true, 1, 3, "Miễn phí", "Free", 0m, 0m, 0m, 0, 0 },
                    { 2, true, true, "Cho người chơi thường xuyên", false, false, false, false, false, true, 3, 10, "Starter", "Starter", 0m, 39000m, 99000m, 1, 1 },
                    { 3, true, true, "Cho người chơi nghiêm túc", true, true, false, true, false, true, -1, -1, "Pro", "Pro", 890000m, 99000m, 249000m, 2, 2 },
                    { 4, true, true, "Dành cho đội nhóm & tổ chức", true, true, true, true, true, true, -1, -1, "Club", "Club", 0m, 199000m, 499000m, 3, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionOrders_TransactionRef",
                table: "SubscriptionOrders",
                column: "TransactionRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionOrders_UserID_Status",
                table: "SubscriptionOrders",
                columns: new[] { "UserID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_PlanKey",
                table: "SubscriptionPlans",
                column: "PlanKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionUsages_UserID_YearMonth",
                table: "SubscriptionUsages",
                columns: new[] { "UserID", "YearMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMatchCredits_TransactionRef",
                table: "UserMatchCredits",
                column: "TransactionRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMatchCredits_UserID_Status",
                table: "UserMatchCredits",
                columns: new[] { "UserID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserID_Status",
                table: "UserSubscriptions",
                columns: new[] { "UserID", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionOrders");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "SubscriptionUsages");

            migrationBuilder.DropTable(
                name: "UserMatchCredits");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");
        }
    }
}
