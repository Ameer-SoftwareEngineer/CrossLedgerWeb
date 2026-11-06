using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardAuthorizationHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CapturedAmount",
                table: "CardAuthorizations",
                type: "decimal(19,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "CardAuthorizations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoldStatus",
                table: "CardAuthorizations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "CardAuthorizations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeTransactionId",
                table: "CardAuthorizations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            // Backfill pre-slice-3 rows: slice 2 treated every approval as an immediate full
            // capture (TransferId was always set on approval), so those rows are
            // retroactively "Captured" for CapturedAmount == Amount, not left at the
            // placeholder "" HoldStatus the column default above would otherwise leave them
            // at - which isn't a valid CardAuthorizationHoldStatus and would throw on the
            // very next read. Declines never held anything either before or after this
            // migration, so they become NotApplicable unconditionally.
            migrationBuilder.Sql(
                """
                UPDATE dbo.CardAuthorizations
                SET HoldStatus = CASE WHEN Approved = 1 THEN 'Captured' ELSE 'NotApplicable' END,
                    CapturedAmount = CASE WHEN Approved = 1 THEN Amount ELSE NULL END,
                    ResolvedAt = CASE WHEN Approved = 1 THEN DecidedAt ELSE NULL END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CardAuthorizations_HoldStatus_ExpiresAt",
                table: "CardAuthorizations",
                columns: new[] { "HoldStatus", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CardAuthorizations_StripeTransactionId",
                table: "CardAuthorizations",
                column: "StripeTransactionId",
                unique: true,
                filter: "[StripeTransactionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CardAuthorizations_HoldStatus_ExpiresAt",
                table: "CardAuthorizations");

            migrationBuilder.DropIndex(
                name: "IX_CardAuthorizations_StripeTransactionId",
                table: "CardAuthorizations");

            migrationBuilder.DropColumn(
                name: "CapturedAmount",
                table: "CardAuthorizations");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "CardAuthorizations");

            migrationBuilder.DropColumn(
                name: "HoldStatus",
                table: "CardAuthorizations");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "CardAuthorizations");

            migrationBuilder.DropColumn(
                name: "StripeTransactionId",
                table: "CardAuthorizations");
        }
    }
}
