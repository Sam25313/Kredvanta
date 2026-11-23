using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditAppraisal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AssessedAmount",
                table: "CreditAssessments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssessedAt",
                table: "CreditAssessments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "AssessedTermPeriods",
                table: "CreditAssessments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CalculationVersion",
                table: "CreditAssessments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreditPolicyVersionId",
                table: "CreditAssessments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "CreditAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CreditAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskFlags",
                table: "CreditAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CreditPolicyVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    MaxDebtServiceRatio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinNetMonthlyIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinDisposableAfterInstalment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculationVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditPolicyVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditAssessments_CreditPolicyVersionId",
                table: "CreditAssessments",
                column: "CreditPolicyVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditAssessments_CreditPolicyVersions_CreditPolicyVersionId",
                table: "CreditAssessments",
                column: "CreditPolicyVersionId",
                principalTable: "CreditPolicyVersions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditAssessments_CreditPolicyVersions_CreditPolicyVersionId",
                table: "CreditAssessments");

            migrationBuilder.DropTable(
                name: "CreditPolicyVersions");

            migrationBuilder.DropIndex(
                name: "IX_CreditAssessments_CreditPolicyVersionId",
                table: "CreditAssessments");

            migrationBuilder.DropColumn(
                name: "AssessedAmount",
                table: "CreditAssessments");

            migrationBuilder.DropColumn(
                name: "AssessedAt",
                table: "CreditAssessments");

            migrationBuilder.DropColumn(
                name: "AssessedTermPeriods",
                table: "CreditAssessments");

            migrationBuilder.DropColumn(
                name: "CalculationVersion",
                table: "CreditAssessments");

            migrationBuilder.DropColumn(
                name: "CreditPolicyVersionId",
                table: "CreditAssessments");

            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "CreditAssessments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CreditAssessments");

            migrationBuilder.DropColumn(
                name: "RiskFlags",
                table: "CreditAssessments");
        }
    }
}
