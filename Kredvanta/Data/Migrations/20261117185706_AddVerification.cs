using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmploymentVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    ContactedParty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPersonName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactDetailUsed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactIndependentlySourced = table.Column<bool>(type: "bit", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    ConfirmedJobTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfirmedContractType = table.Column<int>(type: "int", nullable: true),
                    ConfirmedStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ConfirmedNetSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentVerifications_AspNetUsers_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmploymentVerifications_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KycReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BorrowerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    IdentityMatchesProfile = table.Column<bool>(type: "bit", nullable: false),
                    DocumentsAppearGenuine = table.Column<bool>(type: "bit", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KycReviews_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KycReviews_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentVerifications_LoanApplicationId",
                table: "EmploymentVerifications",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentVerifications_VerifiedByUserId",
                table: "EmploymentVerifications",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KycReviews_BorrowerId",
                table: "KycReviews",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_KycReviews_ReviewedByUserId",
                table: "KycReviews",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmploymentVerifications");

            migrationBuilder.DropTable(
                name: "KycReviews");
        }
    }
}
