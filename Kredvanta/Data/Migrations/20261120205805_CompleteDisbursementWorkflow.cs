using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDisbursementWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LoanOfferId",
                table: "Loans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "LoanDisbursements",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loans_LoanOfferId",
                table: "Loans",
                column: "LoanOfferId",
                unique: true,
                filter: "[LoanOfferId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_IdempotencyKey",
                table: "LoanDisbursements",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_LoanOffers_LoanOfferId",
                table: "Loans",
                column: "LoanOfferId",
                principalTable: "LoanOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Loans_LoanOffers_LoanOfferId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_LoanOfferId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_LoanDisbursements_IdempotencyKey",
                table: "LoanDisbursements");

            migrationBuilder.DropColumn(
                name: "LoanOfferId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "LoanDisbursements");
        }
    }
}
