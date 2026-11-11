using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardSpendingControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedCountriesCsv",
                table: "Cards",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlockedMerchantCategoryCodesCsv",
                table: "Cards",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DailySpendingLimit",
                table: "Cards",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlySpendingLimit",
                table: "Cards",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedCountriesCsv",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "BlockedMerchantCategoryCodesCsv",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "DailySpendingLimit",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "MonthlySpendingLimit",
                table: "Cards");
        }
    }
}
