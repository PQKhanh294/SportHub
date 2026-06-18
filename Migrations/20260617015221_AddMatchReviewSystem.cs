using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchReviewSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.CreateTable(
                name: "MatchReviews",
                columns: table => new
                {
                    MatchReviewID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchID = table.Column<int>(type: "int", nullable: false),
                    ReviewerUserID = table.Column<int>(type: "int", nullable: false),
                    ReviewedUserID = table.Column<int>(type: "int", nullable: false),
                    ReviewType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScoreOrganization = table.Column<byte>(type: "tinyint", nullable: true),
                    ScoreEquipment = table.Column<byte>(type: "tinyint", nullable: true),
                    ScoreAtmosphere = table.Column<byte>(type: "tinyint", nullable: true),
                    ScoreHost = table.Column<byte>(type: "tinyint", nullable: true),
                    ScoreValueForMoney = table.Column<byte>(type: "tinyint", nullable: true),
                    ScorePunctuality = table.Column<byte>(type: "tinyint", nullable: true),
                    ScoreSportsmanship = table.Column<byte>(type: "tinyint", nullable: true),
                    ScoreSkillAccuracy = table.Column<byte>(type: "tinyint", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchReviews", x => x.MatchReviewID);
                    table.CheckConstraint("CK_MatchReviews_ScoreAtmos", "ScoreAtmosphere    IS NULL OR ScoreAtmosphere    BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_MatchReviews_ScoreEquip", "ScoreEquipment     IS NULL OR ScoreEquipment     BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_MatchReviews_ScoreHost", "ScoreHost          IS NULL OR ScoreHost          BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_MatchReviews_ScoreOrg", "ScoreOrganization  IS NULL OR ScoreOrganization  BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_MatchReviews_ScorePunct", "ScorePunctuality   IS NULL OR ScorePunctuality   BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_MatchReviews_ScoreSkill", "ScoreSkillAccuracy IS NULL OR ScoreSkillAccuracy BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_MatchReviews_ScoreSport", "ScoreSportsmanship IS NULL OR ScoreSportsmanship BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_MatchReviews_ScoreValue", "ScoreValueForMoney IS NULL OR ScoreValueForMoney BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_MatchReviews_Type", "ReviewType IN ('PlayerToMatch','HostToPlayer')");
                    table.ForeignKey(
                        name: "FK_MatchReviews_Matches_MatchID",
                        column: x => x.MatchID,
                        principalTable: "Matches",
                        principalColumn: "MatchID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchReviews_Users_ReviewedUserID",
                        column: x => x.ReviewedUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_MatchReviews_Users_ReviewerUserID",
                        column: x => x.ReviewerUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat','MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed','MatchCompleted','MatchReviewReminder')");

            migrationBuilder.CreateIndex(
                name: "IX_MatchReviews_MatchID_ReviewerUserID_ReviewedUserID_ReviewType",
                table: "MatchReviews",
                columns: new[] { "MatchID", "ReviewerUserID", "ReviewedUserID", "ReviewType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchReviews_ReviewedUserID",
                table: "MatchReviews",
                column: "ReviewedUserID");

            migrationBuilder.CreateIndex(
                name: "IX_MatchReviews_ReviewerUserID",
                table: "MatchReviews",
                column: "ReviewerUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchReviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "Type IN ('MatchJoin','MatchApprove','MatchReject','MatchJoinExpired','BookingConfirmed','BookingCancelled','System','Chat','MatchPaymentRequired','MatchRemainingFeeRequired','MatchPaymentConfirmed')");
        }
    }
}
