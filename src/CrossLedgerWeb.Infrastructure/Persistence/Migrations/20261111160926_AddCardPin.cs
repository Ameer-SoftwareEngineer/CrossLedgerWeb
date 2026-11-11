using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardPin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedPin",
                table: "Cards",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPin",
                table: "Cards");
        }
    }
}
