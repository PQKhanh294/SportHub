using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class SportHubUxEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomCourtAddress",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomCourtName",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomLatitude",
                table: "Matches",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomLongitude",
                table: "Matches",
                type: "decimal(11,8)",
                precision: 11,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomPriceVnd",
                table: "Matches",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatBookingProposals",
                columns: table => new
                {
                    ProposalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchID = table.Column<int>(type: "int", nullable: false),
                    SenderID = table.Column<int>(type: "int", nullable: false),
                    ReceiverID = table.Column<int>(type: "int", nullable: false),
                    CourtID = table.Column<int>(type: "int", nullable: true),
                    BookingDate = table.Column<DateTime>(type: "date", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    SplitMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatBookingProposals", x => x.ProposalID);
                    table.CheckConstraint("CK_ChatBookingProposals_SplitMode", "SplitMode IN ('Equal','HostPays')");
                    table.CheckConstraint("CK_ChatBookingProposals_Status", "Status IN ('Waiting','Accepted','Rejected','Booked')");
                    table.ForeignKey(
                        name: "FK_ChatBookingProposals_Courts_CourtID",
                        column: x => x.CourtID,
                        principalTable: "Courts",
                        principalColumn: "CourtID");
                    table.ForeignKey(
                        name: "FK_ChatBookingProposals_Matches_MatchID",
                        column: x => x.MatchID,
                        principalTable: "Matches",
                        principalColumn: "MatchID");
                    table.ForeignKey(
                        name: "FK_ChatBookingProposals_Users_ReceiverID",
                        column: x => x.ReceiverID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ChatBookingProposals_Users_SenderID",
                        column: x => x.SenderID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "MatchInteractions",
                columns: table => new
                {
                    InteractionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchInteractions", x => x.InteractionID);
                    table.CheckConstraint("CK_MatchInteractions_Action", "Action IN ('View','Skip','Request')");
                    table.ForeignKey(
                        name: "FK_MatchInteractions_Matches_MatchID",
                        column: x => x.MatchID,
                        principalTable: "Matches",
                        principalColumn: "MatchID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchInteractions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserBadges",
                columns: table => new
                {
                    BadgeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    BadgeKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BadgeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProgressValue = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBadges", x => x.BadgeID);
                    table.CheckConstraint("CK_UserBadges_Level", "Level IN ('Bronze','Silver','Gold')");
                    table.ForeignKey(
                        name: "FK_UserBadges_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatBookingProposals_CourtID",
                table: "ChatBookingProposals",
                column: "CourtID");

            migrationBuilder.CreateIndex(
                name: "IX_ChatBookingProposals_MatchID_SenderID_ReceiverID_Status",
                table: "ChatBookingProposals",
                columns: new[] { "MatchID", "SenderID", "ReceiverID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatBookingProposals_ReceiverID",
                table: "ChatBookingProposals",
                column: "ReceiverID");

            migrationBuilder.CreateIndex(
                name: "IX_ChatBookingProposals_SenderID",
                table: "ChatBookingProposals",
                column: "SenderID");

            migrationBuilder.CreateIndex(
                name: "IX_MatchInteractions_MatchID_UserID_Action",
                table: "MatchInteractions",
                columns: new[] { "MatchID", "UserID", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchInteractions_UserID",
                table: "MatchInteractions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserID_BadgeKey",
                table: "UserBadges",
                columns: new[] { "UserID", "BadgeKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatBookingProposals");

            migrationBuilder.DropTable(
                name: "MatchInteractions");

            migrationBuilder.DropTable(
                name: "UserBadges");

            migrationBuilder.DropColumn(
                name: "CustomCourtAddress",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CustomCourtName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CustomLatitude",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CustomLongitude",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CustomPriceVnd",
                table: "Matches");
        }
    }
}
