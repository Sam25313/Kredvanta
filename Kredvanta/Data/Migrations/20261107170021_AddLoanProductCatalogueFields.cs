using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanProductCatalogueFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoanApplicationStatusHistories_LoanApplicationId_ChangedAt",
                table: "LoanApplicationStatusHistories");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "LoanProducts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "LoanProducts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableForNewApplications",
                table: "LoanProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplicationStatusHistories_LoanApplicationId_CreatedAt",
                table: "LoanApplicationStatusHistories",
                columns: new[] { "LoanApplicationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoanApplicationStatusHistories_LoanApplicationId_CreatedAt",
                table: "LoanApplicationStatusHistories");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "LoanProducts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "LoanProducts");

            migrationBuilder.DropColumn(
                name: "IsAvailableForNewApplications",
                table: "LoanProducts");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplicationStatusHistories_LoanApplicationId_ChangedAt",
                table: "LoanApplicationStatusHistories",
                columns: new[] { "LoanApplicationId", "ChangedAt" });
        }
    }
}
