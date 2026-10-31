using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Payouts",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ProviderCode_UpdatedAt",
                table: "Payouts",
                columns: new[] { "ProviderCode", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payouts_ProviderCode_UpdatedAt",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Payouts");
        }
    }
}
