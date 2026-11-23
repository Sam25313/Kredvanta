using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepaymentReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                table: "Repayments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAt",
                table: "Repayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversedByUserId",
                table: "Repayments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReversalReason",
                table: "Repayments");

            migrationBuilder.DropColumn(
                name: "ReversedAt",
                table: "Repayments");

            migrationBuilder.DropColumn(
                name: "ReversedByUserId",
                table: "Repayments");
        }
    }
}
