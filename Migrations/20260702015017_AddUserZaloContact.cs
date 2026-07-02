using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddUserZaloContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZaloContact",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZaloContact",
                table: "Users");
        }
    }
}
