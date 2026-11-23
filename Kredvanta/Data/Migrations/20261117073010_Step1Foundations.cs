using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step1Foundations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BorrowerVerificationDocuments_Borrowers_BorrowerId",
                table: "BorrowerVerificationDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_LoanApplicationDocuments_AspNetUsers_ReviewedByUserId",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_LoanApplicationStatusHistories_AspNetUsers_ChangedByUserId",
                table: "LoanApplicationStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Repayments_ExternalTransactionId",
                table: "Repayments");

            migrationBuilder.DropIndex(
                name: "IX_RepaymentAllocations_RepaymentId_RepaymentScheduleId",
                table: "RepaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Loan_FinancialAmounts",
                table: "Loans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoanProductRequiredDocument_Period",
                table: "LoanProductRequiredDocuments");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplicationStatusHistories_LoanApplicationId_CreatedAt",
                table: "LoanApplicationStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplicationDocuments_LoanApplicationId",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropIndex(
                name: "IX_BorrowerVerificationDocuments_BorrowerId_DocumentType",
                table: "BorrowerVerificationDocuments");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActionDate",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptNumber",
                table: "Repayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "RelatedEntityId",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Loans",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LoanDisbursements",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "LoanApplications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LoanApplications",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "ReviewComments",
                table: "LoanApplicationDocuments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "LoanApplicationDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReasonCode",
                table: "LoanApplicationDocuments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacesDocumentId",
                table: "LoanApplicationDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256Hash",
                table: "LoanApplicationDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "LoanApplicationDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "BorrowerVerificationDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentType",
                table: "BorrowerVerificationDocuments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityName",
                table: "AuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "AuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AuditLogs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentAllocations_RepaymentId",
                table: "RepaymentAllocations",
                column: "RepaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDisbursements_TransactionReference",
                table: "LoanDisbursements",
                column: "TransactionReference",
                unique: true,
                filter: "[TransactionReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplicationStatusHistories_LoanApplicationId_ChangedAt",
                table: "LoanApplicationStatusHistories",
                columns: new[] { "LoanApplicationId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_Status",
                table: "LoanApplications",
                column: "Status");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoanApplication_Request",
                table: "LoanApplications",
                sql: "[RequestedAmount] > 0 AND [RequestedTermPeriods] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplicationDocuments_LoanApplicationId_LoanProductRequiredDocumentId",
                table: "LoanApplicationDocuments",
                columns: new[] { "LoanApplicationId", "LoanProductRequiredDocumentId" },
                unique: true,
                filter: "[IsCurrent] = 1 AND [LoanProductRequiredDocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplicationDocuments_ReplacesDocumentId",
                table: "LoanApplicationDocuments",
                column: "ReplacesDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplicationDocuments_StorageKey",
                table: "LoanApplicationDocuments",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerVerificationDocuments_BorrowerId_DocumentType_CreatedAt",
                table: "BorrowerVerificationDocuments",
                columns: new[] { "BorrowerId", "DocumentType", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowerVerificationDocuments_Borrowers_BorrowerId",
                table: "BorrowerVerificationDocuments",
                column: "BorrowerId",
                principalTable: "Borrowers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoanApplicationDocuments_AspNetUsers_ReviewedByUserId",
                table: "LoanApplicationDocuments",
                column: "ReviewedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoanApplicationDocuments_LoanApplicationDocuments_ReplacesDocumentId",
                table: "LoanApplicationDocuments",
                column: "ReplacesDocumentId",
                principalTable: "LoanApplicationDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LoanApplicationStatusHistories_AspNetUsers_ChangedByUserId",
                table: "LoanApplicationStatusHistories",
                column: "ChangedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BorrowerVerificationDocuments_Borrowers_BorrowerId",
                table: "BorrowerVerificationDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_LoanApplicationDocuments_AspNetUsers_ReviewedByUserId",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_LoanApplicationDocuments_LoanApplicationDocuments_ReplacesDocumentId",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_LoanApplicationStatusHistories_AspNetUsers_ChangedByUserId",
                table: "LoanApplicationStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_RepaymentAllocations_RepaymentId",
                table: "RepaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_LoanDisbursements_TransactionReference",
                table: "LoanDisbursements");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplicationStatusHistories_LoanApplicationId_ChangedAt",
                table: "LoanApplicationStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplications_Status",
                table: "LoanApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoanApplication_Request",
                table: "LoanApplications");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplicationDocuments_LoanApplicationId_LoanProductRequiredDocumentId",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplicationDocuments_ReplacesDocumentId",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplicationDocuments_StorageKey",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropIndex(
                name: "IX_BorrowerVerificationDocuments_BorrowerId_DocumentType_CreatedAt",
                table: "BorrowerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LoanDisbursements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "RejectionReasonCode",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "ReplacesDocumentId",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "Sha256Hash",
                table: "LoanApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LoanApplicationDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptNumber",
                table: "Repayments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "RelatedEntityId",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "LoanApplications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReviewComments",
                table: "LoanApplicationDocuments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "BorrowerVerificationDocuments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentType",
                table: "BorrowerVerificationDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityName",
                table: "AuditLogs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Repayments_ExternalTransactionId",
                table: "Repayments",
                column: "ExternalTransactionId",
                unique: true,
                filter: "[ExternalTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentAllocations_RepaymentId_RepaymentScheduleId",
                table: "RepaymentAllocations",
                columns: new[] { "RepaymentId", "RepaymentScheduleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Loan_FinancialAmounts",
                table: "Loans",
                sql: "[PrincipalAmount] >= 0 AND [OutstandingBalance] >= 0 AND [TotalRepayable] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoanProductRequiredDocument_Period",
                table: "LoanProductRequiredDocuments",
                sql: "[RequiredPeriodMonths] IS NULL OR [RequiredPeriodMonths] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplicationStatusHistories_LoanApplicationId_CreatedAt",
                table: "LoanApplicationStatusHistories",
                columns: new[] { "LoanApplicationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplicationDocuments_LoanApplicationId",
                table: "LoanApplicationDocuments",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerVerificationDocuments_BorrowerId_DocumentType",
                table: "BorrowerVerificationDocuments",
                columns: new[] { "BorrowerId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActionDate",
                table: "AuditLogs",
                column: "ActionDate");

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowerVerificationDocuments_Borrowers_BorrowerId",
                table: "BorrowerVerificationDocuments",
                column: "BorrowerId",
                principalTable: "Borrowers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LoanApplicationDocuments_AspNetUsers_ReviewedByUserId",
                table: "LoanApplicationDocuments",
                column: "ReviewedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LoanApplicationStatusHistories_AspNetUsers_ChangedByUserId",
                table: "LoanApplicationStatusHistories",
                column: "ChangedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
