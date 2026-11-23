using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kredvanta.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step1Corrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_RepaymentAllocations_RepaymentId",
                table: "RepaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Repayments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RepaymentSchedule_Amounts",
                table: "RepaymentSchedules",
                sql: "[PrincipalDue] >= 0 AND [InterestDue] >= 0 AND [FeesDue] >= 0 AND [TotalDue] >= 0 AND [AmountPaid] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Repayments_ExternalTransactionId",
                table: "Repayments",
                column: "ExternalTransactionId",
                unique: true,
                filter: "[ExternalTransactionId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Repayment_Amount",
                table: "Repayments",
                sql: "[Amount] > 0");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoanDisbursement_Amount",
                table: "LoanDisbursements",
                sql: "[Amount] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActionDate",
                table: "AuditLogs",
                column: "ActionDate");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RepaymentSchedule_Amounts",
                table: "RepaymentSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Repayments_ExternalTransactionId",
                table: "Repayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Repayment_Amount",
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

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoanDisbursement_Amount",
                table: "LoanDisbursements");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActionDate",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Repayments");

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentAllocations_RepaymentId",
                table: "RepaymentAllocations",
                column: "RepaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
