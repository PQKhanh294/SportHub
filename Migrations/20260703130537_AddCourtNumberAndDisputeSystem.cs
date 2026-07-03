using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddCourtNumberAndDisputeSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourtNumber",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiScore",
                table: "MatchDisputes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "MatchDisputes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DisputeWitnessResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisputeId = table.Column<int>(type: "int", nullable: false),
                    WitnessUserId = table.Column<int>(type: "int", nullable: false),
                    Stance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeWitnessResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeWitnessResponses_MatchDisputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "MatchDisputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisputeWitnessResponses_Users_WitnessUserId",
                        column: x => x.WitnessUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisputeWitnessResponses_DisputeId_WitnessUserId",
                table: "DisputeWitnessResponses",
                columns: new[] { "DisputeId", "WitnessUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisputeWitnessResponses_WitnessUserId",
                table: "DisputeWitnessResponses",
                column: "WitnessUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisputeWitnessResponses");

            migrationBuilder.DropColumn(
                name: "CourtNumber",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AiScore",
                table: "MatchDisputes");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "MatchDisputes");
        }
    }
}
