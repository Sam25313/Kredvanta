using Kredvanta.Models.Applications;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Loans;
using Kredvanta.Models.Products;
using Kredvanta.Models.Repayments;
using Kredvanta.Models.System;
using Kredvanta.Models.Verification;
using Kredvanta.Models.Credit;
using Kredvanta.Models.Contracts;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Borrower> Borrowers => Set<Borrower>();
        public DbSet<Guarantor> Guarantors => Set<Guarantor>();
        public DbSet<LoanProduct> LoanProducts => Set<LoanProduct>();
        public DbSet<LoanProductRequiredDocument> LoanProductRequiredDocuments => Set<LoanProductRequiredDocument>();
        public DbSet<BorrowerVerificationDocument> BorrowerVerificationDocuments => Set<BorrowerVerificationDocument>();
        public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
        public DbSet<LoanApplicationDocument> LoanApplicationDocuments => Set<LoanApplicationDocument>();
        public DbSet<LoanApplicationGuarantor> LoanApplicationGuarantors => Set<LoanApplicationGuarantor>();
        public DbSet<CreditAssessment> CreditAssessments => Set<CreditAssessment>();
        public DbSet<LoanApplicationStatusHistory> LoanApplicationStatusHistories => Set<LoanApplicationStatusHistory>();
        public DbSet<LoanApproval> LoanApprovals => Set<LoanApproval>();
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<LoanDisbursement> LoanDisbursements => Set<LoanDisbursement>();
        public DbSet<RepaymentSchedule> RepaymentSchedules => Set<RepaymentSchedule>();
        public DbSet<Repayment> Repayments => Set<Repayment>();
        public DbSet<RepaymentAllocation> RepaymentAllocations => Set<RepaymentAllocation>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<KycReview> KycReviews => Set<KycReview>();
        public DbSet<EmploymentVerification> EmploymentVerifications => Set<EmploymentVerification>();
        public DbSet<CreditPolicyVersion> CreditPolicyVersions => Set<CreditPolicyVersion>();
        public DbSet<ApplicantCreditPolicyPublication> ApplicantCreditPolicyPublications => Set<ApplicantCreditPolicyPublication>();
        public DbSet<ApplicantCreditPolicyReference> ApplicantCreditPolicyReferences => Set<ApplicantCreditPolicyReference>();
        public DbSet<LoanOffer> LoanOffers => Set<LoanOffer>();
        public DbSet<LoanAgreement> LoanAgreements => Set<LoanAgreement>();
        public DbSet<AgreementSignature> AgreementSignatures => Set<AgreementSignature>();



        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampUpdatedAt();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            StampUpdatedAt();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void StampUpdatedAt()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<BaseEntity>()
                         .Where(e => e.State == EntityState.Modified))
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ConfigureBorrowers(builder);
            ConfigureGuarantors(builder);
            ConfigureLoanProducts(builder);
            ConfigureApplications(builder);
            ConfigureLoanProcessing(builder);
            ConfigureRepayments(builder);
            ConfigureSystem(builder);
            ConfigureBorrowerVerificationDocuments(builder);
            ConfigureCredit(builder);
            ConfigureContracts(builder);
        }

        private static void ConfigureBorrowers(ModelBuilder builder)
        {
            builder.Entity<Borrower>(entity =>
            {
                entity.HasIndex(x => x.BorrowerNumber).IsUnique();
                entity.HasIndex(x => new { x.IdentificationType, x.NationalIdNumber }).IsUnique();
                entity.HasIndex(x => x.PhoneNumber);
                entity.HasIndex(x => x.ApplicationUserId).IsUnique().HasFilter("[ApplicationUserId] IS NOT NULL");

                entity.Property(x => x.BorrowerNumber).HasMaxLength(30).IsRequired();
                entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.MiddleName).HasMaxLength(100);
                entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.NationalIdNumber).HasMaxLength(30).IsRequired();
                entity.Property(x => x.IdentificationType).HasDefaultValue(IdentificationType.NationalId);
                entity.Property(x => x.Email).HasMaxLength(150);
                entity.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
                entity.Property(x => x.AlternativePhoneNumber).HasMaxLength(20);
                entity.Property(x => x.County).HasMaxLength(100).IsRequired();
                entity.Property(x => x.SubCounty).HasMaxLength(100);
                entity.Property(x => x.Ward).HasMaxLength(100);
                entity.Property(x => x.EstateOrVillage).HasMaxLength(150);
                entity.Property(x => x.PhysicalAddress).HasMaxLength(250);
                entity.Property(x => x.EmployerOrBusinessName).HasMaxLength(200);
                entity.Property(x => x.Occupation).HasMaxLength(150);
                entity.Property(x => x.Industry).HasMaxLength(150);
                entity.Property(x => x.JobTitle).HasMaxLength(150);
                entity.Property(x => x.EmployerAddress).HasMaxLength(300);
                entity.Property(x => x.EmployerContactPerson).HasMaxLength(150);
                entity.Property(x => x.EmployerPhoneNumber).HasMaxLength(20);
                entity.Property(x => x.EmployerEmail).HasMaxLength(150);
                entity.Property(x => x.MonthlyIncome).HasPrecision(18, 2);
                entity.Property(x => x.GrossMonthlySalary).HasPrecision(18, 2);
                entity.Property(x => x.NetMonthlySalary).HasPrecision(18, 2);
                entity.Property(x => x.OtherMonthlyIncome).HasPrecision(18, 2);
                entity.Property(x => x.OtherIncomeSource).HasMaxLength(200);
                entity.Property(x => x.IsProfileComplete).HasDefaultValue(false);
                entity.Property(x => x.ProfileCompletionStep).HasDefaultValue(1);

                entity.HasOne(x => x.ApplicationUser)
                    .WithOne(x => x.Borrower)
                    .HasForeignKey<Borrower>(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void ConfigureGuarantors(ModelBuilder builder)
        {
            builder.Entity<Guarantor>(entity =>
            {
                entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.NationalIdNumber).HasMaxLength(30).IsRequired();
                entity.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
                entity.Property(x => x.Email).HasMaxLength(150);
            });
        }

        private static void ConfigureLoanProducts(ModelBuilder builder)
        {
            builder.Entity<LoanProduct>(entity =>
            {
                entity.HasIndex(x => x.ProductCode).IsUnique();
                entity.Property(x => x.ProductCode).HasMaxLength(30).IsRequired();
                entity.Property(x => x.ProductName).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
                entity.Property(x => x.ImageUrl).HasMaxLength(500);
                entity.Property(x => x.MinimumAmount).HasPrecision(18, 2);
                entity.Property(x => x.MaximumAmount).HasPrecision(18, 2);
                entity.Property(x => x.AnnualInterestRate).HasPrecision(9, 4);
                entity.Property(x => x.ProcessingFeePercentage).HasPrecision(9, 4);
                entity.Property(x => x.ProcessingFeeFixed).HasPrecision(18, 2);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_LoanProduct_AmountRange", "[MinimumAmount] >= 0 AND [MaximumAmount] >= [MinimumAmount]");
                    table.HasCheckConstraint("CK_LoanProduct_InterestRate", "[AnnualInterestRate] >= 0");
                    table.HasCheckConstraint("CK_LoanProduct_Term", "[MinimumTermPeriods] > 0 AND [MaximumTermPeriods] >= [MinimumTermPeriods]");
                });
            });

            builder.Entity<LoanProductRequiredDocument>(entity =>
            {
                entity.Property(x => x.DocumentName).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.HasOne(x => x.LoanProduct)
                    .WithMany(x => x.RequiredDocuments)
                    .HasForeignKey(x => x.LoanProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint("CK_LoanProductRequiredDocument_Period",
                 "[RequiredPeriodMonths] IS NULL OR [RequiredPeriodMonths] > 0"));
            });
        }

        private static void ConfigureApplications(ModelBuilder builder)
        {
            builder.Entity<LoanApplication>(entity =>
            {
                entity.HasIndex(x => x.ApplicationNumber).IsUnique();
                entity.Property(x => x.ApplicationNumber).HasMaxLength(40).IsRequired();
                entity.Property(x => x.RequestedAmount).HasPrecision(18, 2);
                entity.Property(x => x.LoanPurpose).HasMaxLength(1000).IsRequired();
                entity.Property(x => x.CancellationReason).HasMaxLength(2000);
                entity.Property(x => x.RowVersion).IsRowVersion();

                entity.HasOne(x => x.Borrower)
                    .WithMany(x => x.LoanApplications)
                    .HasForeignKey(x => x.BorrowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.LoanProduct)
                    .WithMany(x => x.LoanApplications)
                    .HasForeignKey(x => x.LoanProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.AssignedOfficer)
                    .WithMany()
                    .HasForeignKey(x => x.AssignedOfficerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(x => new { x.BorrowerId, x.Status });
                entity.HasIndex(x => x.Status);

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_LoanApplication_Request", "[RequestedAmount] > 0 AND [RequestedTermPeriods] > 0");
                });
            });

            builder.Entity<LoanApplicationStatusHistory>(entity =>
            {
                entity.Property(x => x.ChangedByUserId).HasMaxLength(450);
                entity.Property(x => x.Notes).HasMaxLength(2000);
                entity.HasIndex(x => new { x.LoanApplicationId, x.ChangedAt });
                entity.HasOne(x => x.LoanApplication)
                    .WithMany(x => x.StatusHistory)
                    .HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ChangedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

               
            });

            builder.Entity<LoanApplicationDocument>(entity =>
            {
                entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
                entity.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
                entity.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
                entity.Property(x => x.FileExtension).HasMaxLength(20).IsRequired();
                entity.Property(x => x.UploadedByUserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.ReviewedByUserId).HasMaxLength(450);
                entity.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
                entity.Property(x => x.RejectionReasonCode).HasMaxLength(50);
                entity.Property(x => x.ReviewComments).HasMaxLength(1000);
                entity.HasIndex(x => x.StorageKey).IsUnique();
                entity.HasIndex(x => new { x.LoanApplicationId, x.LoanProductRequiredDocumentId }).IsUnique().HasFilter("[IsCurrent] = 1 AND [LoanProductRequiredDocumentId] IS NOT NULL");
                entity.Property(x => x.ScreeningSummary).HasMaxLength(500);
                entity.HasIndex(x => x.ScreeningStatus);

                entity.HasOne(x => x.LoanApplication)
                    .WithMany(x => x.Documents)
                    .HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.RequiredDocument)
                    .WithMany()
                    .HasForeignKey(x => x.LoanProductRequiredDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ReplacesDocument)
                    .WithMany()
                    .HasForeignKey(x => x.ReplacesDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.UploadedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<LoanApplicationGuarantor>(entity =>
            {
                entity.HasIndex(x => new { x.LoanApplicationId, x.GuarantorId }).IsUnique();
                entity.Property(x => x.GuaranteedAmount).HasPrecision(18, 2);

                entity.HasOne(x => x.LoanApplication)
                    .WithMany(x => x.Guarantors)
                    .HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Guarantor)
                    .WithMany(x => x.LoanApplicationGuarantors)
                    .HasForeignKey(x => x.GuarantorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CreditAssessment>(entity =>
            {
                entity.Property(x => x.MonthlyIncome).HasPrecision(18, 2);
                entity.Property(x => x.MonthlyExpenses).HasPrecision(18, 2);
                entity.Property(x => x.ExistingMonthlyLoanPayments).HasPrecision(18, 2);
                entity.Property(x => x.OtherMonthlyObligations).HasPrecision(18, 2);
                entity.Property(x => x.DisposableIncome).HasPrecision(18, 2);
                entity.Property(x => x.ProposedMonthlyInstallment).HasPrecision(18, 2);
                entity.Property(x => x.DebtServiceRatio).HasPrecision(9, 4);
                entity.Property(x => x.AssessedByUserId).HasMaxLength(450).IsRequired();

                entity.HasOne(x => x.LoanApplication)
                    .WithMany(x => x.CreditAssessments)
                    .HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.AssessedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.AssessedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureLoanProcessing(ModelBuilder builder)
        {
            builder.Entity<LoanApproval>(entity =>
            {
                entity.HasIndex(x => new { x.LoanApplicationId, x.ApprovalLevel });
                entity.Property(x => x.ReviewedByUserId).HasMaxLength(450).IsRequired();

                entity.HasOne(x => x.LoanApplication)
                    .WithMany(x => x.Approvals)
                    .HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Loan>(entity =>
            {
                entity.HasIndex(x => x.LoanNumber).IsUnique();
                entity.HasIndex(x => x.LoanApplicationId).IsUnique();
                entity.HasIndex(x => x.LoanOfferId).IsUnique();
                entity.Property(x => x.LoanNumber).HasMaxLength(40).IsRequired();
                entity.Property(x => x.ProductNameSnapshot).HasMaxLength(150).IsRequired();
                entity.Property(x => x.ProductCodeSnapshot).HasMaxLength(30).IsRequired();
                entity.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
                entity.Property(x => x.AnnualInterestRate).HasPrecision(9, 4);
                entity.Property(x => x.TotalInterest).HasPrecision(18, 2);
                entity.Property(x => x.ProcessingFee).HasPrecision(18, 2);
                entity.Property(x => x.TotalRepayable).HasPrecision(18, 2);
                entity.Property(x => x.OutstandingBalance).HasPrecision(18, 2);
                entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
                entity.Property(x => x.RowVersion).IsRowVersion();

                entity.HasOne(x => x.LoanApplication)
                    .WithOne(x => x.Loan)
                    .HasForeignKey<Loan>(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.LoanOffer)
                    .WithMany()
                    .HasForeignKey(x => x.LoanOfferId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Borrower)
                    .WithMany(x => x.Loans)
                    .HasForeignKey(x => x.BorrowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t => t.HasCheckConstraint("CK_Loan_FinancialAmounts",
                    "[PrincipalAmount] >= 0 AND [OutstandingBalance] >= 0 AND [TotalRepayable] >= 0"));
            });

            builder.Entity<LoanDisbursement>(entity =>
            {
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.TransactionReference).HasMaxLength(100);
                entity.Property(x => x.RecipientAccount).HasMaxLength(100);
                entity.Property(x => x.ProcessedByUserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.IdempotencyKey).HasMaxLength(64);
                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasIndex(x => x.TransactionReference).IsUnique().HasFilter("[TransactionReference] IS NOT NULL");
                entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");

                entity.HasOne(x => x.Loan)
                    .WithMany(x => x.Disbursements)
                    .HasForeignKey(x => x.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ProcessedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ProcessedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t => t.HasCheckConstraint("CK_LoanDisbursement_Amount", "[Amount] > 0"));
            });
        }

        private static void ConfigureRepayments(ModelBuilder builder)
        {
            builder.Entity<RepaymentSchedule>(entity =>
            {
                entity.HasIndex(x => new { x.LoanId, x.InstallmentNumber }).IsUnique();
                entity.Property(x => x.PrincipalDue).HasPrecision(18, 2);
                entity.Property(x => x.InterestDue).HasPrecision(18, 2);
                entity.Property(x => x.FeesDue).HasPrecision(18, 2);
                entity.Property(x => x.TotalDue).HasPrecision(18, 2);
                entity.Property(x => x.AmountPaid).HasPrecision(18, 2);


                entity.HasOne(x => x.Loan)
                    .WithMany(x => x.RepaymentSchedules)
                    .HasForeignKey(x => x.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint("CK_RepaymentSchedule_Amounts",
                 "[PrincipalDue] >= 0 AND [InterestDue] >= 0 AND [FeesDue] >= 0 AND [TotalDue] >= 0 AND [AmountPaid] >= 0"));
            });

            builder.Entity<Repayment>(entity =>
            {
                entity.HasIndex(x => x.ReceiptNumber).IsUnique();
                entity.Property(x => x.ReceiptNumber).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.TransactionReference).HasMaxLength(100);
                entity.Property(x => x.ExternalTransactionId).HasMaxLength(100);
                entity.Property(x => x.PayerPhoneNumber).HasMaxLength(20);
                entity.Property(x => x.ReceivedByUserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.ReversalReason).HasMaxLength(500);
                entity.Property(x => x.ReversedByUserId).HasMaxLength(450);

                entity.HasOne(x => x.Loan)
                    .WithMany(x => x.Repayments)
                    .HasForeignKey(x => x.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ReceivedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ReceivedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.ExternalTransactionId).IsUnique().HasFilter("[ExternalTransactionId] IS NOT NULL");
                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.ToTable(t => t.HasCheckConstraint("CK_Repayment_Amount", "[Amount] > 0"));
            });

            builder.Entity<RepaymentAllocation>(entity =>
            {
                entity.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
                entity.Property(x => x.InterestAmount).HasPrecision(18, 2);
                entity.Property(x => x.FeesAmount).HasPrecision(18, 2);
                entity.Property(x => x.AllocatedAmount).HasPrecision(18, 2);

                entity.HasOne(x => x.Repayment)
                    .WithMany(x => x.Allocations)
                    .HasForeignKey(x => x.RepaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.RepaymentSchedule)
                    .WithMany(x => x.Allocations)
                    .HasForeignKey(x => x.RepaymentScheduleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.RepaymentId, x.RepaymentScheduleId }).IsUnique();
            });
        }

        private static void ConfigureSystem(ModelBuilder builder)
        {
            builder.Entity<Notification>(entity =>
            {
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
                entity.Property(x => x.RelatedEntityId).HasMaxLength(200);
                entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
                entity.HasIndex(x => new { x.UserId, x.IsRead });

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Notifications)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AuditLog>(entity =>
            {
                entity.Property(x => x.UserId).HasMaxLength(450);
                entity.Property(x => x.Action).HasMaxLength(200).IsRequired();
                entity.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.EntityId).HasMaxLength(200);
                entity.Property(x => x.Description).HasMaxLength(4000);
                entity.Property(x => x.IpAddress).HasMaxLength(100);
                entity.Property(x => x.UserAgent).HasMaxLength(500);

                entity.HasIndex(x => x.ActionDate);
                entity.HasIndex(x => new { x.EntityName, x.EntityId });
                entity.HasOne(x => x.User)
                    .WithMany(x => x.AuditLogs)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBorrowerVerificationDocuments(ModelBuilder builder)
        {
            builder.Entity<BorrowerVerificationDocument>(entity =>
            {
                entity.HasIndex(x => new { x.BorrowerId, x.DocumentType, x.CreatedAt });
                entity.Property(x => x.DocumentType).HasMaxLength(150).IsRequired();
                entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
                entity.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
                entity.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
                entity.Property(x => x.FileExtension).HasMaxLength(20).IsRequired();
                entity.Property(x => x.UploadedByUserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.ReviewComments).HasMaxLength(1000);

                entity.HasOne(x => x.Borrower)
                    .WithMany(x => x.VerificationDocuments)
                    .HasForeignKey(x => x.BorrowerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
        

        private static void ConfigureVerification(ModelBuilder builder)
        {
            builder.Entity<KycReview>(entity =>
            {
                entity.Property(x => x.ReasonCode).HasMaxLength(50);
                entity.Property(x => x.Notes).HasMaxLength(2000);
                entity.Property(x => x.ReviewedByUserId).HasMaxLength(450).IsRequired();
                entity.HasIndex(x => new { x.BorrowerId, x.ReviewedAt });

                entity.HasOne(x => x.Borrower).WithMany()
                    .HasForeignKey(x => x.BorrowerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.ReviewedByUser).WithMany()
                    .HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<EmploymentVerification>(entity =>
            {
                entity.Property(x => x.ContactedParty).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ContactPersonName).HasMaxLength(150);
                entity.Property(x => x.ContactDetailUsed).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ConfirmedJobTitle).HasMaxLength(150);
                entity.Property(x => x.ConfirmedNetSalary).HasPrecision(18, 2);
                entity.Property(x => x.Notes).HasMaxLength(2000);
                entity.Property(x => x.VerifiedByUserId).HasMaxLength(450).IsRequired();
                entity.HasIndex(x => new { x.LoanApplicationId, x.AttemptedAt });

                entity.HasOne(x => x.LoanApplication).WithMany()
                    .HasForeignKey(x => x.LoanApplicationId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.VerifiedByUser).WithMany()
                    .HasForeignKey(x => x.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
        }
       

        private static void ConfigureCredit(ModelBuilder builder)
        {
            builder.Entity<CreditPolicyVersion>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.Property(x => x.DualApprovalAbove).HasPrecision(18, 2);
                e.Property(x => x.MaxDebtServiceRatio).HasPrecision(9, 4);
                e.Property(x => x.MinNetMonthlyIncome).HasPrecision(18, 2);
                e.Property(x => x.MinDisposableAfterInstalment).HasPrecision(18, 2);
                e.Property(x => x.CalculationVersion).HasMaxLength(20).IsRequired();
                e.Property(x => x.ApprovedBy).HasMaxLength(450);
                e.Property(x => x.Notes).HasMaxLength(2000);
                e.HasIndex(x => x.VersionNumber).IsUnique();
                // Only one active policy at a time
                e.HasIndex(x => x.IsActive).IsUnique().HasFilter("[IsActive] = 1");
            });

            builder.Entity<CreditAssessment>(e =>
            {
                e.Property(x => x.RiskFlags).HasMaxLength(1000);
                e.Property(x => x.Notes).HasMaxLength(2000);
                e.Property(x => x.CalculationVersion).HasMaxLength(20).IsRequired();
                e.Property(x => x.AssessedAmount).HasPrecision(18, 2);
                e.HasIndex(x => new { x.LoanApplicationId, x.IsCurrent });

                e.HasOne(x => x.CreditPolicyVersion).WithMany()
                    .HasForeignKey(x => x.CreditPolicyVersionId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ApplicantCreditPolicyPublication>(entity =>
            {
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.PublishedByUserId).HasMaxLength(450);
                entity.HasIndex(x => x.VersionNumber).IsUnique();
                entity.HasIndex(x => x.IsPublished).IsUnique().HasFilter("[IsPublished] = 1");
            });

            builder.Entity<ApplicantCreditPolicyReference>(entity =>
            {
                entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
                entity.Property(x => x.Url).HasMaxLength(1000).IsRequired();
                entity.HasOne(x => x.Publication)
                    .WithMany(x => x.References)
                    .HasForeignKey(x => x.ApplicantCreditPolicyPublicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


        }
      

        private static void ConfigureContracts(ModelBuilder builder)
        {
            builder.Entity<LoanOffer>(e =>
            {
                e.Property(x => x.ProductCodeSnapshot).HasMaxLength(30).IsRequired();
                e.Property(x => x.ProductNameSnapshot).HasMaxLength(150).IsRequired();
                e.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
                e.Property(x => x.IssuedByUserId).HasMaxLength(450).IsRequired();
                e.Property(x => x.AcceptedByUserId).HasMaxLength(450);
                e.Property(x => x.Principal).HasPrecision(18, 2);
                e.Property(x => x.AnnualInterestRate).HasPrecision(9, 4);
                e.Property(x => x.ProcessingFee).HasPrecision(18, 2);
                e.Property(x => x.TotalInterest).HasPrecision(18, 2);
                e.Property(x => x.TotalRepayable).HasPrecision(18, 2);
                e.Property(x => x.InstalmentAmount).HasPrecision(18, 2);
                e.Property(x => x.Conditions).HasMaxLength(2000);
                e.Property(x => x.DeclineReason).HasMaxLength(1000);
                e.Property(x => x.RowVersion).IsRowVersion();

                e.HasIndex(x => new { x.LoanApplicationId, x.Version }).IsUnique();
                // At most one live offer (Issued = 0 or Accepted = 1) per application
                e.HasIndex(x => x.LoanApplicationId).IsUnique().HasFilter("[Status] IN (0, 1)");

                e.HasOne(x => x.LoanApplication).WithMany().HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.IssuedByUser).WithMany().HasForeignKey(x => x.IssuedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.AcceptedByUser).WithMany().HasForeignKey(x => x.AcceptedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.ToTable(t => t.HasCheckConstraint("CK_LoanOffer_Amounts",
                    "[Principal] > 0 AND [TotalRepayable] > 0 AND [InstalmentAmount] > 0"));
            });

            builder.Entity<LoanAgreement>(e =>
            {
                e.Property(x => x.AgreementNumber).HasMaxLength(60).IsRequired();
                e.Property(x => x.TemplateVersion).HasMaxLength(30).IsRequired();
                e.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
                e.HasIndex(x => x.AgreementNumber).IsUnique();
                e.HasIndex(x => x.LoanOfferId).IsUnique();

                e.HasOne(x => x.LoanApplication).WithMany().HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.LoanOffer).WithMany().HasForeignKey(x => x.LoanOfferId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AgreementSignature>(e =>
            {
                e.Property(x => x.SignedByUserId).HasMaxLength(450).IsRequired();
                e.Property(x => x.Method).HasMaxLength(50).IsRequired();
                e.Property(x => x.AgreementHash).HasMaxLength(64).IsRequired();
                e.Property(x => x.IpAddress).HasMaxLength(100);
                e.Property(x => x.UserAgent).HasMaxLength(500);
                e.HasIndex(x => x.LoanAgreementId).IsUnique();

                e.HasOne(x => x.LoanAgreement).WithMany().HasForeignKey(x => x.LoanAgreementId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.SignedByUser).WithMany().HasForeignKey(x => x.SignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
