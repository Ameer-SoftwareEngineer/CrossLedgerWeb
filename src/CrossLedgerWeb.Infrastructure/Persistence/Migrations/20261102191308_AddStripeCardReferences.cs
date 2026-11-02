using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeCardReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeCardId",
                table: "Cards",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCardholderId",
                table: "Cards",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeCardId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "StripeCardholderId",
                table: "Cards");
        }
    }
}
