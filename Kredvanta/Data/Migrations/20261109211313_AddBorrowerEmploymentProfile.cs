using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowerEmploymentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailableForNewApplications",
                table: "LoanProducts");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "LoanProductRequiredDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredPeriodMonths",
                table: "LoanProductRequiredDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmployerOrBusinessName",
                table: "Borrowers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerAddress",
                table: "Borrowers",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerContactPerson",
                table: "Borrowers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerEmail",
                table: "Borrowers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerPhoneNumber",
                table: "Borrowers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentContractType",
                table: "Borrowers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EmploymentEndDate",
                table: "Borrowers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EmploymentStartDate",
                table: "Borrowers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossMonthlySalary",
                table: "Borrowers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Borrowers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetMonthlySalary",
                table: "Borrowers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayrollFrequency",
                table: "Borrowers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProbationEndDate",
                table: "Borrowers",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoanProductRequiredDocument_Period",
                table: "LoanProductRequiredDocuments",
                sql: "[RequiredPeriodMonths] IS NULL OR [RequiredPeriodMonths] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LoanProductRequiredDocument_Period",
                table: "LoanProductRequiredDocuments");

            migrationBuilder.DropColumn(
                name: "RequiredPeriodMonths",
                table: "LoanProductRequiredDocuments");

            migrationBuilder.DropColumn(
                name: "EmployerAddress",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "EmployerContactPerson",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "EmployerEmail",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "EmployerPhoneNumber",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "EmploymentContractType",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "EmploymentEndDate",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "EmploymentStartDate",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "GrossMonthlySalary",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "NetMonthlySalary",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "PayrollFrequency",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "ProbationEndDate",
                table: "Borrowers");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableForNewApplications",
                table: "LoanProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "LoanProductRequiredDocuments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmployerOrBusinessName",
                table: "Borrowers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
