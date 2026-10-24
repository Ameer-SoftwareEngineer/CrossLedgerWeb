using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCardsForPrepaidModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LinkedWalletId",
                table: "Cards",
                newName: "CardWalletId");

            migrationBuilder.AddColumn<string>(
                name: "CardType",
                table: "Cards",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployerCompanyName",
                table: "Cards",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedGrossAnnualIncome",
                table: "Cards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedNetWorth",
                table: "Cards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActivelyEmployed",
                table: "Cards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Cards",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredCity",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegisteredCountry",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegisteredNumber",
                table: "Cards",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegisteredPostcode",
                table: "Cards",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegisteredState",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegisteredStreet",
                table: "Cards",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingCity",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingCountry",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingNumber",
                table: "Cards",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingPostcode",
                table: "Cards",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingState",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingStreet",
                table: "Cards",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceOfFunds",
                table: "Cards",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardType",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "EmployerCompanyName",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "EstimatedGrossAnnualIncome",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "EstimatedNetWorth",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "IsActivelyEmployed",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "RegisteredCity",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "RegisteredCountry",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "RegisteredNumber",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "RegisteredPostcode",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "RegisteredState",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "RegisteredStreet",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ShippingCity",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ShippingCountry",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ShippingNumber",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ShippingPostcode",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ShippingState",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ShippingStreet",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "SourceOfFunds",
                table: "Cards");

            migrationBuilder.RenameColumn(
                name: "CardWalletId",
                table: "Cards",
                newName: "LinkedWalletId");
        }
    }
}
