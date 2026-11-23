using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class DocumentScreening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScreenedAt",
                table: "LoanApplicationDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreeningDetailsJson",
                table: "LoanApplicationDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScreeningStartedAt",
                table: "LoanApplicationDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScreeningStatus",
                table: "LoanApplicationDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScreeningSummary",
                table: "LoanApplicationDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplicationDocuments_ScreeningStatus",
                table: "LoanApplicationDocuments",
                column: "ScreeningStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoanApplicationDocuments_ScreeningStatus",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "ScreenedAt",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "ScreeningDetailsJson",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "ScreeningStartedAt",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "ScreeningStatus",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "ScreeningSummary",
                table: "LoanApplicationDocuments");
        }
    }
}
