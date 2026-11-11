using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardFulfillment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAt",
                table: "Cards",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FulfillmentStatus",
                table: "Cards",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActivated",
                table: "Cards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacesCardId",
                table: "Cards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ShippedAt",
                table: "Cards",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCarrier",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Backfill pre-slice-6 rows: the column defaults above leave FulfillmentStatus
            // at "" (not a valid CardFulfillmentStatus - would throw on the very next read,
            // the same mistake slice 3's HoldStatus backfill fixed) and IsActivated at
            // false, which would retroactively lock out physical cards that were already
            // fully usable before this migration (there was no activation concept at all).
            // The honest backfilled state for an existing row is "already arrived and
            // already usable," not "awaiting shipment."
            migrationBuilder.Sql(
                """
                UPDATE dbo.Cards
                SET FulfillmentStatus = CASE WHEN CardType = 'Physical' THEN 'Delivered' ELSE 'NotApplicable' END,
                    IsActivated = 1
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "FulfillmentStatus",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "IsActivated",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ReplacesCardId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ShippingCarrier",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "Cards");
        }
    }
}
