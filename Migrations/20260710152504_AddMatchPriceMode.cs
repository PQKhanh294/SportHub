using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportHub.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchPriceMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PriceFemaleVnd",
                table: "Matches",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceMaleVnd",
                table: "Matches",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceMode",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "PerPerson");

            // Backfill: trận cũ đang bật chia đều giữ nguyên ý nghĩa cũ — phải chạy TRƯỚC khi drop cột IsSplitFee
            migrationBuilder.Sql("UPDATE Matches SET PriceMode = 'SplitEven' WHERE IsSplitFee = 1");

            migrationBuilder.DropColumn(
                name: "IsSplitFee",
                table: "Matches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSplitFee",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE Matches SET IsSplitFee = 1 WHERE PriceMode = 'SplitEven'");

            migrationBuilder.DropColumn(
                name: "PriceFemaleVnd",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PriceMaleVnd",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PriceMode",
                table: "Matches");
        }
    }
}
