using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOffersAndAgreements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssuedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductCodeSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Principal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualInterestRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    InterestMethod = table.Column<int>(type: "int", nullable: false),
                    RepaymentFrequency = table.Column<int>(type: "int", nullable: false),
                    TermPeriods = table.Column<int>(type: "int", nullable: false),
                    ProcessingFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalInterest = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRepayable = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InstalmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FirstDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MaturityDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DeclineReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanOffers", x => x.Id);
                    table.CheckConstraint("CK_LoanOffer_Amounts", "[Principal] > 0 AND [TotalRepayable] > 0 AND [InstalmentAmount] > 0");
                    table.ForeignKey(
                        name: "FK_LoanOffers_AspNetUsers_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanOffers_AspNetUsers_IssuedByUserId",
                        column: x => x.IssuedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanOffers_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanAgreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgreementNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    TemplateVersion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanAgreements_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanAgreements_LoanOffers_LoanOfferId",
                        column: x => x.LoanOfferId,
                        principalTable: "LoanOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgreementSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AgreementHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgreementSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgreementSignatures_AspNetUsers_SignedByUserId",
                        column: x => x.SignedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgreementSignatures_LoanAgreements_LoanAgreementId",
                        column: x => x.LoanAgreementId,
                        principalTable: "LoanAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgreementSignatures_LoanAgreementId",
                table: "AgreementSignatures",
                column: "LoanAgreementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgreementSignatures_SignedByUserId",
                table: "AgreementSignatures",
                column: "SignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAgreements_AgreementNumber",
                table: "LoanAgreements",
                column: "AgreementNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanAgreements_LoanApplicationId",
                table: "LoanAgreements",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAgreements_LoanOfferId",
                table: "LoanAgreements",
                column: "LoanOfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanOffers_AcceptedByUserId",
                table: "LoanOffers",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanOffers_IssuedByUserId",
                table: "LoanOffers",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanOffers_LoanApplicationId",
                table: "LoanOffers",
                column: "LoanApplicationId",
                unique: true,
                filter: "[Status] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_LoanOffers_LoanApplicationId_Version",
                table: "LoanOffers",
                columns: new[] { "LoanApplicationId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgreementSignatures");

            migrationBuilder.DropTable(
                name: "LoanAgreements");

            migrationBuilder.DropTable(
                name: "LoanOffers");
        }
    }
}
