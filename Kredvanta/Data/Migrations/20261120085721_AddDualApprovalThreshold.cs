using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDualApprovalThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditAssessments_CreditPolicyVersions_CreditPolicyVersionId",
                table: "CreditAssessments");

            migrationBuilder.DropIndex(
                name: "IX_CreditAssessments_LoanApplicationId",
                table: "CreditAssessments");

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "LoanApprovals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Conditions",
                table: "LoanApprovals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "CreditPolicyVersions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CreditPolicyVersions",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxDebtServiceRatio",
                table: "CreditPolicyVersions",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "CalculationVersion",
                table: "CreditPolicyVersions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "CreditPolicyVersions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DualApprovalAbove",
                table: "CreditPolicyVersions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RiskFlags",
                table: "CreditAssessments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "CreditAssessments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CalculationVersion",
                table: "CreditAssessments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_CreditPolicyVersions_IsActive",
                table: "CreditPolicyVersions",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CreditPolicyVersions_VersionNumber",
                table: "CreditPolicyVersions",
                column: "VersionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditAssessments_LoanApplicationId_IsCurrent",
                table: "CreditAssessments",
                columns: new[] { "LoanApplicationId", "IsCurrent" });

            migrationBuilder.AddForeignKey(
                name: "FK_CreditAssessments_CreditPolicyVersions_CreditPolicyVersionId",
                table: "CreditAssessments",
                column: "CreditPolicyVersionId",
                principalTable: "CreditPolicyVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditAssessments_CreditPolicyVersions_CreditPolicyVersionId",
                table: "CreditAssessments");

            migrationBuilder.DropIndex(
                name: "IX_CreditPolicyVersions_IsActive",
                table: "CreditPolicyVersions");

            migrationBuilder.DropIndex(
                name: "IX_CreditPolicyVersions_VersionNumber",
                table: "CreditPolicyVersions");

            migrationBuilder.DropIndex(
                name: "IX_CreditAssessments_LoanApplicationId_IsCurrent",
                table: "CreditAssessments");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "LoanApprovals");

            migrationBuilder.DropColumn(
                name: "Conditions",
                table: "LoanApprovals");

            migrationBuilder.DropColumn(
                name: "DualApprovalAbove",
                table: "CreditPolicyVersions");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "CreditPolicyVersions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CreditPolicyVersions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxDebtServiceRatio",
                table: "CreditPolicyVersions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "CalculationVersion",
                table: "CreditPolicyVersions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "CreditPolicyVersions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RiskFlags",
                table: "CreditAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "CreditAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CalculationVersion",
                table: "CreditAssessments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_CreditAssessments_LoanApplicationId",
                table: "CreditAssessments",
                column: "LoanApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditAssessments_CreditPolicyVersions_CreditPolicyVersionId",
                table: "CreditAssessments",
                column: "CreditPolicyVersionId",
                principalTable: "CreditPolicyVersions",
                principalColumn: "Id");
        }
    }
}
