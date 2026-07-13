using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityListings",
                columns: table => new
                {
                    CommunityListingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SportID = table.Column<int>(type: "int", nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceAuthorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedByUserID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VenueName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MatchDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    SkillRequired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostMaleVnd = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CostFemaleVnd = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SlotsNeeded = table.Column<int>(type: "int", nullable: true),
                    ParseStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParsedJsonRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityListings", x => x.CommunityListingID);
                    table.ForeignKey(
                        name: "FK_CommunityListings_Sports_SportID",
                        column: x => x.SportID,
                        principalTable: "Sports",
                        principalColumn: "SportID");
                    table.ForeignKey(
                        name: "FK_CommunityListings_Users_SubmittedByUserID",
                        column: x => x.SubmittedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityListings_SourceUrl",
                table: "CommunityListings",
                column: "SourceUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityListings_SportID",
                table: "CommunityListings",
                column: "SportID");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityListings_SubmittedByUserID",
                table: "CommunityListings",
                column: "SubmittedByUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityListings");
        }
    }
}
