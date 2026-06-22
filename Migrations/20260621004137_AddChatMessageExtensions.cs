using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchCardJson",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "MatchCardJson",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "ChatMessages");
        }
    }
}
