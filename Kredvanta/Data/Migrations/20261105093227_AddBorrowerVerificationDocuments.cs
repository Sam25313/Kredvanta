using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowerVerificationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BorrowerVerificationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    BorrowerId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    DocumentType = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),

                    OriginalFileName = table.Column<string>(
                        type: "nvarchar(255)",
                        maxLength: 255,
                        nullable: false),

                    StorageKey = table.Column<string>(
                        type: "nvarchar(255)",
                        maxLength: 255,
                        nullable: false),

                    ContentType = table.Column<string>(
                        type: "nvarchar(150)",
                        maxLength: 150,
                        nullable: false),

                    FileExtension = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false),

                    FileSizeBytes = table.Column<long>(
                        type: "bigint",
                        nullable: false),

                    UploadedByUserId = table.Column<string>(
                        type: "nvarchar(450)",
                        maxLength: 450,
                        nullable: false),

                    ReviewStatus = table.Column<int>(
                        type: "int",
                        nullable: false),

                    ReviewComments = table.Column<string>(
                        type: "nvarchar(1000)",
                        maxLength: 1000,
                        nullable: true),

                    ReviewedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: true),

                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false),

                    UpdatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_BorrowerVerificationDocuments",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_BorrowerVerificationDocuments_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerVerificationDocuments_BorrowerId_DocumentType",
                table: "BorrowerVerificationDocuments",
                columns: new[] { "BorrowerId", "DocumentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BorrowerVerificationDocuments");
        }
    }
}
