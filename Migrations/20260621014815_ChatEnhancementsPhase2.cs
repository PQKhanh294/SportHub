using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class ChatEnhancementsPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BanEndAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ChatMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReplyToMessageID",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MessageReactions",
                columns: table => new
                {
                    ReactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ReactionType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReactions", x => x.ReactionID);
                    table.ForeignKey(
                        name: "FK_MessageReactions_ChatMessages_MessageID",
                        column: x => x.MessageID,
                        principalTable: "ChatMessages",
                        principalColumn: "MessageID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageReactions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "MessageReports",
                columns: table => new
                {
                    ReportID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageID = table.Column<int>(type: "int", nullable: false),
                    ReporterID = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiAnalysis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AiViolationScore = table.Column<int>(type: "int", nullable: false),
                    AiRecommendation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByAdminID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReports", x => x.ReportID);
                    table.CheckConstraint("CK_MessageReports_Status", "Status IN ('Pending','Reviewed','Dismissed')");
                    table.ForeignKey(
                        name: "FK_MessageReports_ChatMessages_MessageID",
                        column: x => x.MessageID,
                        principalTable: "ChatMessages",
                        principalColumn: "MessageID");
                    table.ForeignKey(
                        name: "FK_MessageReports_Users_ReporterID",
                        column: x => x.ReporterID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_MessageReports_Users_ReviewedByAdminID",
                        column: x => x.ReviewedByAdminID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserBans",
                columns: table => new
                {
                    BanID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    BannedByAdminID = table.Column<int>(type: "int", nullable: false),
                    ReportID = table.Column<int>(type: "int", nullable: true),
                    BanType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBans", x => x.BanID);
                    table.ForeignKey(
                        name: "FK_UserBans_MessageReports_ReportID",
                        column: x => x.ReportID,
                        principalTable: "MessageReports",
                        principalColumn: "ReportID");
                    table.ForeignKey(
                        name: "FK_UserBans_Users_BannedByAdminID",
                        column: x => x.BannedByAdminID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_UserBans_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReplyToMessageID",
                table: "ChatMessages",
                column: "ReplyToMessageID");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageID_UserID_ReactionType",
                table: "MessageReactions",
                columns: new[] { "MessageID", "UserID", "ReactionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_UserID",
                table: "MessageReactions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReports_MessageID",
                table: "MessageReports",
                column: "MessageID");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReports_ReporterID",
                table: "MessageReports",
                column: "ReporterID");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReports_ReviewedByAdminID",
                table: "MessageReports",
                column: "ReviewedByAdminID");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_BannedByAdminID",
                table: "UserBans",
                column: "BannedByAdminID");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_ReportID",
                table: "UserBans",
                column: "ReportID");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_UserID",
                table: "UserBans",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatMessages_ReplyToMessageID",
                table: "ChatMessages",
                column: "ReplyToMessageID",
                principalTable: "ChatMessages",
                principalColumn: "MessageID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatMessages_ReplyToMessageID",
                table: "ChatMessages");

            migrationBuilder.DropTable(
                name: "MessageReactions");

            migrationBuilder.DropTable(
                name: "UserBans");

            migrationBuilder.DropTable(
                name: "MessageReports");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_ReplyToMessageID",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "BanEndAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageID",
                table: "ChatMessages");
        }
    }
}
