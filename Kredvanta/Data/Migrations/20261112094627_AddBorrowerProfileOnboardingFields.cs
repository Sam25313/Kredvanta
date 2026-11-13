using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowerProfileOnboardingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Borrowers_NationalIdNumber",
                table: "Borrowers");

            migrationBuilder.AlterColumn<string>(
                name: "SubCounty",
                table: "Borrowers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhysicalAddress",
                table: "Borrowers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Borrowers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AlternativePhoneNumber",
                table: "Borrowers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessYearsInOperation",
                table: "Borrowers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentStatus",
                table: "Borrowers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstateOrVillage",
                table: "Borrowers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdentificationType",
                table: "Borrowers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "Borrowers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProfileComplete",
                table: "Borrowers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "Borrowers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherIncomeSource",
                table: "Borrowers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherMonthlyIncome",
                table: "Borrowers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfileCompletedAt",
                table: "Borrowers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileCompletionStep",
                table: "Borrowers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ResidenceType",
                table: "Borrowers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "Borrowers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Borrowers_IdentificationType_NationalIdNumber",
                table: "Borrowers",
                columns: new[] { "IdentificationType", "NationalIdNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Borrowers_IdentificationType_NationalIdNumber",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "BusinessYearsInOperation",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "EmploymentStatus",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "EstateOrVillage",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "IdentificationType",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "IsProfileComplete",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "OtherIncomeSource",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "OtherMonthlyIncome",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "ProfileCompletedAt",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "ProfileCompletionStep",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "ResidenceType",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "Borrowers");

            migrationBuilder.AlterColumn<string>(
                name: "SubCounty",
                table: "Borrowers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhysicalAddress",
                table: "Borrowers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Borrowers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AlternativePhoneNumber",
                table: "Borrowers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Borrowers_NationalIdNumber",
                table: "Borrowers",
                column: "NationalIdNumber",
                unique: true);
        }
    }
}
