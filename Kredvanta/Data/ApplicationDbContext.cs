
using Kredvanta.Models.Applications;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Loans;
using Kredvanta.Models.Products;
using Kredvanta.Models.Repayments;
using Kredvanta.Models.System;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ==========================================
        // BORROWER MANAGEMENT
        // ==========================================

        public DbSet<Borrower> Borrowers => Set<Borrower>();

        public DbSet<Guarantor> Guarantors => Set<Guarantor>();


        // ==========================================
        // LOAN PRODUCTS
        // ==========================================

        public DbSet<LoanProduct> LoanProducts => Set<LoanProduct>();

        public DbSet<LoanProductRequiredDocument> LoanProductRequiredDocuments
            => Set<LoanProductRequiredDocument>();


        // ==========================================
        // LOAN APPLICATIONS
        // ==========================================

        public DbSet<BorrowerVerificationDocument>
            BorrowerVerificationDocuments => Set<BorrowerVerificationDocument>();

        public DbSet<LoanApplication> LoanApplications
            => Set<LoanApplication>();

        public DbSet<LoanApplicationDocument> LoanApplicationDocuments
            => Set<LoanApplicationDocument>();

        public DbSet<LoanApplicationGuarantor> LoanApplicationGuarantors
            => Set<LoanApplicationGuarantor>();

        public DbSet<CreditAssessment> CreditAssessments
            => Set<CreditAssessment>();

        public DbSet<LoanApplicationStatusHistory>
            LoanApplicationStatusHistories => Set<LoanApplicationStatusHistory>();


        // ==========================================
        // LOAN PROCESSING
        // ==========================================

        public DbSet<LoanApproval> LoanApprovals
            => Set<LoanApproval>();

        public DbSet<Loan> Loans
            => Set<Loan>();

        public DbSet<LoanDisbursement> LoanDisbursements
            => Set<LoanDisbursement>();


        // ==========================================
        // REPAYMENTS
        // ==========================================

        public DbSet<RepaymentSchedule> RepaymentSchedules
            => Set<RepaymentSchedule>();

        public DbSet<Repayment> Repayments
            => Set<Repayment>();

        public DbSet<RepaymentAllocation> RepaymentAllocations
            => Set<RepaymentAllocation>();


        // ==========================================
        // SYSTEM MANAGEMENT
        // ==========================================

        public DbSet<Notification> Notifications
            => Set<Notification>();

        public DbSet<AuditLog> AuditLogs
            => Set<AuditLog>();


        // ==========================================
        // MODEL CONFIGURATION
        // ==========================================

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
        }


        // ==========================================
        // BORROWER CONFIGURATION
        // ==========================================

        private static void ConfigureBorrowers(ModelBuilder builder)
        {
            builder.Entity<Borrower>(entity =>
            {
                // Unique borrower number
                entity.HasIndex(x => x.BorrowerNumber)
                    .IsUnique();

                // Unique identification number per identification type
                entity.HasIndex(x => new
                {
                    x.IdentificationType,
                    x.NationalIdNumber
                }).IsUnique();

                // Phone number lookup
                entity.HasIndex(x => x.PhoneNumber);

                // One borrower profile per Identity account
                entity.HasIndex(x => x.ApplicationUserId)
                    .IsUnique()
                    .HasFilter("[ApplicationUserId] IS NOT NULL");


                // PERSONAL INFORMATION

                entity.Property(x => x.BorrowerNumber)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.MiddleName)
                    .HasMaxLength(100);

                entity.Property(x => x.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.NationalIdNumber)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.IdentificationType)
                    .HasDefaultValue(IdentificationType.NationalId);

                entity.Property(x => x.Email)
                    .HasMaxLength(150);


                // CONTACT INFORMATION

                entity.Property(x => x.PhoneNumber)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.AlternativePhoneNumber)
                    .HasMaxLength(20);


                // RESIDENTIAL INFORMATION

                entity.Property(x => x.County)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.SubCounty)
                    .HasMaxLength(100);

                entity.Property(x => x.Ward)
                    .HasMaxLength(100);

                entity.Property(x => x.EstateOrVillage)
                    .HasMaxLength(150);

                entity.Property(x => x.PhysicalAddress)
                    .HasMaxLength(250);


                // EMPLOYMENT INFORMATION

                entity.Property(x => x.EmployerOrBusinessName)
                    .HasMaxLength(200);

                entity.Property(x => x.Occupation)
                    .HasMaxLength(150);

                entity.Property(x => x.Industry)
                    .HasMaxLength(150);

                entity.Property(x => x.JobTitle)
                    .HasMaxLength(150);

                entity.Property(x => x.EmployerAddress)
                    .HasMaxLength(300);

                entity.Property(x => x.EmployerContactPerson)
                    .HasMaxLength(150);

                entity.Property(x => x.EmployerPhoneNumber)
                    .HasMaxLength(20);

                entity.Property(x => x.EmployerEmail)
                    .HasMaxLength(150);


                // FINANCIAL INFORMATION

                entity.Property(x => x.MonthlyIncome)
                    .HasPrecision(18, 2);

                entity.Property(x => x.GrossMonthlySalary)
                    .HasPrecision(18, 2);

                entity.Property(x => x.NetMonthlySalary)
                    .HasPrecision(18, 2);

                entity.Property(x => x.OtherMonthlyIncome)
                    .HasPrecision(18, 2);

                entity.Property(x => x.OtherIncomeSource)
                    .HasMaxLength(200);


                // PROFILE COMPLETION

                entity.Property(x => x.IsProfileComplete)
                    .HasDefaultValue(false);

                entity.Property(x => x.ProfileCompletionStep)
                    .HasDefaultValue(1);


                // IDENTITY ACCOUNT RELATIONSHIP

                entity.HasOne(x => x.ApplicationUser)
                    .WithOne(x => x.Borrower)
                    .HasForeignKey<Borrower>(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }


        // ==========================================
        // GUARANTOR CONFIGURATION
        // ==========================================

        private static void ConfigureGuarantors(ModelBuilder builder)
        {
            builder.Entity<Guarantor>(entity =>
            {
                entity.Property(x => x.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.NationalIdNumber)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.PhoneNumber)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(150);
            });
        }


        // ==========================================
        // LOAN PRODUCT CONFIGURATION
        // ==========================================

        private static void ConfigureLoanProducts(ModelBuilder builder)
        {
            builder.Entity<LoanProduct>(entity =>
            {
                entity.HasIndex(x => x.ProductCode)
                    .IsUnique();

                entity.Property(x => x.ProductCode)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.ProductName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Category)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.ImageUrl)
                    .HasMaxLength(500);

                entity.Property(x => x.MinimumAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.MaximumAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.AnnualInterestRate)
                    .HasPrecision(9, 4);

                entity.Property(x => x.ProcessingFeePercentage)
                    .HasPrecision(9, 4);

                entity.Property(x => x.ProcessingFeeFixed)
                    .HasPrecision(18, 2);

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_LoanProduct_AmountRange",
                        "[MinimumAmount] >= 0 AND [MaximumAmount] >= [MinimumAmount]");

                    table.HasCheckConstraint(
                        "CK_LoanProduct_InterestRate",
                        "[AnnualInterestRate] >= 0");

                    table.HasCheckConstraint(
                        "CK_LoanProduct_Term",
                        "[MinimumTermPeriods] > 0 AND [MaximumTermPeriods] >= [MinimumTermPeriods]");
                });
            });


            // Required documents for each loan product

            builder.Entity<LoanProductRequiredDocument>(entity =>
            {
                entity.Property(x => x.DocumentName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.HasOne(x => x.LoanProduct)
                    .WithMany(x => x.RequiredDocuments)
                    .HasForeignKey(x => x.LoanProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_LoanProductRequiredDocument_Period",
                        "[RequiredPeriodMonths] IS NULL OR [RequiredPeriodMonths] > 0");
                });
            });
        }


        // ==========================================
        // LOAN APPLICATION CONFIGURATION
        // ==========================================

        private static void ConfigureApplications(ModelBuilder builder)
        {
            builder.Entity<LoanApplication>(entity =>
            {
                entity.HasIndex(x => x.ApplicationNumber)
                    .IsUnique();

                entity.Property(x => x.ApplicationNumber)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(x => x.RequestedAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.LoanPurpose)
                    .HasMaxLength(1000)
                    .IsRequired();

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

                entity.HasIndex(x => new
                {
                    x.BorrowerId,
                    x.Status
                });
            });


            // Loan application status history

            builder.Entity<LoanApplicationStatusHistory>(entity =>
            {
                entity.Property(x => x.ChangedByUserId)
                    .HasMaxLength(450);

                entity.Property(x => x.Notes)
                    .HasMaxLength(2000);

                entity.HasIndex(x => new
                {
                    x.LoanApplicationId,
                    x.CreatedAt
                });

                entity.HasOne(x => x.LoanApplication)
                    .WithMany(x => x.StatusHistory)
                    .HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ChangedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // Loan application supporting documents

            builder.Entity<LoanApplicationDocument>(entity =>
            {
                entity.Property(x => x.OriginalFileName)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.StorageKey)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.ContentType)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.FileExtension)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.UploadedByUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(x => x.ReviewedByUserId)
                    .HasMaxLength(450);

                entity.HasOne(x => x.LoanApplication)
                    .WithMany(x => x.Documents)
                    .HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.RequiredDocument)
                    .WithMany()
                    .HasForeignKey(x => x.LoanProductRequiredDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.UploadedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // Loan application guarantors

            builder.Entity<LoanApplicationGuarantor>(entity =>
            {
                entity.HasIndex(x => new
                {
                    x.LoanApplicationId,
                    x.GuarantorId
                }).IsUnique();

                entity.Property(x => x.GuaranteedAmount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.LoanApplication)
                    .WithMany(x => x.Guarantors)
                    .HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Guarantor)
                    .WithMany(x => x.LoanApplicationGuarantors)
                    .HasForeignKey(x => x.GuarantorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // Credit assessment storage configuration

            builder.Entity<CreditAssessment>(entity =>
            {
                entity.Property(x => x.MonthlyIncome)
                    .HasPrecision(18, 2);

                entity.Property(x => x.MonthlyExpenses)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ExistingMonthlyLoanPayments)
                    .HasPrecision(18, 2);

                entity.Property(x => x.OtherMonthlyObligations)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DisposableIncome)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ProposedMonthlyInstallment)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DebtServiceRatio)
                    .HasPrecision(9, 4);

                entity.Property(x => x.AssessedByUserId)
                    .HasMaxLength(450)
                    .IsRequired();

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


        // ==========================================
        // LOAN PROCESSING CONFIGURATION
        // ==========================================

        private static void ConfigureLoanProcessing(ModelBuilder builder)
        {
            // Loan approvals

            builder.Entity<LoanApproval>(entity =>
            {
                entity.HasIndex(x => new
                {
                    x.LoanApplicationId,
                    x.ApprovalLevel
                });

                entity.Property(x => x.ReviewedByUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.HasOne(x => x.LoanApplication)
                    .WithMany(x => x.Approvals)
                    .HasForeignKey(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // Issued loans

            builder.Entity<Loan>(entity =>
            {
                entity.HasIndex(x => x.LoanNumber)
                    .IsUnique();

                entity.HasIndex(x => x.LoanApplicationId)
                    .IsUnique();

                entity.Property(x => x.LoanNumber)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(x => x.ProductNameSnapshot)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.ProductCodeSnapshot)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.PrincipalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.AnnualInterestRate)
                    .HasPrecision(9, 4);

                entity.Property(x => x.TotalInterest)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ProcessingFee)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TotalRepayable)
                    .HasPrecision(18, 2);

                entity.Property(x => x.OutstandingBalance)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CurrencyCode)
                    .HasMaxLength(3)
                    .IsRequired();

                entity.HasOne(x => x.LoanApplication)
                    .WithOne(x => x.Loan)
                    .HasForeignKey<Loan>(x => x.LoanApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Borrower)
                    .WithMany(x => x.Loans)
                    .HasForeignKey(x => x.BorrowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_Loan_FinancialAmounts",
                        "[PrincipalAmount] >= 0 AND [OutstandingBalance] >= 0 AND [TotalRepayable] >= 0");
                });
            });


            // Loan disbursements

            builder.Entity<LoanDisbursement>(entity =>
            {
                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TransactionReference)
                    .HasMaxLength(100);

                entity.Property(x => x.RecipientAccount)
                    .HasMaxLength(100);

                entity.Property(x => x.ProcessedByUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.HasOne(x => x.Loan)
                    .WithMany(x => x.Disbursements)
                    .HasForeignKey(x => x.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ProcessedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ProcessedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }


        // ==========================================
        // REPAYMENT CONFIGURATION
        // ==========================================

        private static void ConfigureRepayments(ModelBuilder builder)
        {
            // Repayment schedules

            builder.Entity<RepaymentSchedule>(entity =>
            {
                entity.HasIndex(x => new
                {
                    x.LoanId,
                    x.InstallmentNumber
                }).IsUnique();

                entity.Property(x => x.PrincipalDue)
                    .HasPrecision(18, 2);

                entity.Property(x => x.InterestDue)
                    .HasPrecision(18, 2);

                entity.Property(x => x.FeesDue)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TotalDue)
                    .HasPrecision(18, 2);

                entity.Property(x => x.AmountPaid)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Loan)
                    .WithMany(x => x.RepaymentSchedules)
                    .HasForeignKey(x => x.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // Repayments

            builder.Entity<Repayment>(entity =>
            {
                entity.HasIndex(x => x.ReceiptNumber)
                    .IsUnique();

                entity.HasIndex(x => x.ExternalTransactionId)
                    .IsUnique()
                    .HasFilter("[ExternalTransactionId] IS NOT NULL");

                entity.Property(x => x.ReceiptNumber)
                    .HasMaxLength(40)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TransactionReference)
                    .HasMaxLength(100);

                entity.Property(x => x.ExternalTransactionId)
                    .HasMaxLength(100);

                entity.Property(x => x.PayerPhoneNumber)
                    .HasMaxLength(20);

                entity.Property(x => x.ReceivedByUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.HasOne(x => x.Loan)
                    .WithMany(x => x.Repayments)
                    .HasForeignKey(x => x.LoanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ReceivedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ReceivedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // Repayment allocations

            builder.Entity<RepaymentAllocation>(entity =>
            {
                entity.HasIndex(x => new
                {
                    x.RepaymentId,
                    x.RepaymentScheduleId
                }).IsUnique();

                entity.Property(x => x.PrincipalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.InterestAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.FeesAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.AllocatedAmount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Repayment)
                    .WithMany(x => x.Allocations)
                    .HasForeignKey(x => x.RepaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.RepaymentSchedule)
                    .WithMany(x => x.Allocations)
                    .HasForeignKey(x => x.RepaymentScheduleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }


        // ==========================================
        // SYSTEM CONFIGURATION
        // ==========================================

        private static void ConfigureSystem(ModelBuilder builder)
        {
            // Notifications

            builder.Entity<Notification>(entity =>
            {
                entity.Property(x => x.UserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(x => x.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Message)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.IsRead
                });

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Notifications)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // Audit logs

            builder.Entity<AuditLog>(entity =>
            {
                entity.Property(x => x.UserId)
                    .HasMaxLength(450);

                entity.Property(x => x.Action)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.EntityName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.EntityId)
                    .HasMaxLength(100);

                entity.Property(x => x.IpAddress)
                    .HasMaxLength(50);

                entity.Property(x => x.UserAgent)
                    .HasMaxLength(500);

                entity.HasIndex(x => x.ActionDate);

                entity.HasOne(x => x.User)
                    .WithMany(x => x.AuditLogs)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }


        // ==========================================
        // BORROWER VERIFICATION DOCUMENTS
        // ==========================================

        private static void ConfigureBorrowerVerificationDocuments(
            ModelBuilder builder)
        {
            builder.Entity<BorrowerVerificationDocument>(entity =>
            {
                entity.Property(x => x.DocumentType)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.OriginalFileName)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.StorageKey)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.ContentType)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.FileExtension)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.UploadedByUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(x => x.ReviewComments)
                    .HasMaxLength(1000);

                entity.HasIndex(x => new
                {
                    x.BorrowerId,
                    x.DocumentType
                });

                entity.HasOne(x => x.Borrower)
                    .WithMany(x => x.VerificationDocuments)
                    .HasForeignKey(x => x.BorrowerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
