using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossLedgerWeb.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardAuthorizationManualReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AttemptedCaptureAmount",
                table: "CardAuthorizations",
                type: "decimal(19,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ManualReviewFlaggedAt",
                table: "CardAuthorizations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualReviewReason",
                table: "CardAuthorizations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptedCaptureAmount",
                table: "CardAuthorizations");

            migrationBuilder.DropColumn(
                name: "ManualReviewFlaggedAt",
                table: "CardAuthorizations");

            migrationBuilder.DropColumn(
                name: "ManualReviewReason",
                table: "CardAuthorizations");
        }
    }
}
