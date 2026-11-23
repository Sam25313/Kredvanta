using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicantCreditPolicyPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicantCreditPolicyPublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PurposeAndScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EligibilityAndDocuments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentAndDecision = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PricingAndOffers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RepaymentAndComplaints = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PersonalInformation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportantNotice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantCreditPolicyPublications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantCreditPolicyReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicantCreditPolicyPublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantCreditPolicyReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantCreditPolicyReferences_ApplicantCreditPolicyPublications_ApplicantCreditPolicyPublicationId",
                        column: x => x.ApplicantCreditPolicyPublicationId,
                        principalTable: "ApplicantCreditPolicyPublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantCreditPolicyPublications_IsPublished",
                table: "ApplicantCreditPolicyPublications",
                column: "IsPublished",
                unique: true,
                filter: "[IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantCreditPolicyPublications_VersionNumber",
                table: "ApplicantCreditPolicyPublications",
                column: "VersionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantCreditPolicyReferences_ApplicantCreditPolicyPublicationId",
                table: "ApplicantCreditPolicyReferences",
                column: "ApplicantCreditPolicyPublicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicantCreditPolicyReferences");

            migrationBuilder.DropTable(
                name: "ApplicantCreditPolicyPublications");
        }
    }
}
