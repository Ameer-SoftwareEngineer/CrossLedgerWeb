using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardAuthorizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StripeAuthorizationId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MerchantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Approved = table.Column<bool>(type: "bit", nullable: false),
                    DeclineReason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardAuthorizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardAuthorizations_CardId",
                table: "CardAuthorizations",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardAuthorizations_StripeAuthorizationId",
                table: "CardAuthorizations",
                column: "StripeAuthorizationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardAuthorizations");
        }
    }
}
