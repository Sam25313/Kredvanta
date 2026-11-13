
using Kredvanta.Models.Applications;
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Loans;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Borrowers
{
    public class Borrower : BaseEntity
    {
        // ==========================================
        // IDENTITY ACCOUNT
        // ==========================================

        public string? ApplicationUserId { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }


        // ==========================================
        // BORROWER IDENTIFICATION - STEP 1
        // ==========================================

        public string BorrowerNumber { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string LastName { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }

        // Optional during onboarding.
        public Gender? Gender { get; set; }

        public IdentificationType IdentificationType { get; set; }
            = IdentificationType.NationalId;

        // Stores the National ID or Passport number.
        // Retained for compatibility with existing records.
        public string NationalIdNumber { get; set; } = string.Empty;


        // ==========================================
        // CONTACT INFORMATION - STEP 2
        // ==========================================

        public string PhoneNumber { get; set; } = string.Empty;

        public string? AlternativePhoneNumber { get; set; }

        public string? Email { get; set; }


        // ==========================================
        // RESIDENTIAL INFORMATION - STEP 2
        // ==========================================

        public string County { get; set; } = string.Empty;

        public string? SubCounty { get; set; }

        public string? Ward { get; set; }

        public string? EstateOrVillage { get; set; }

        public ResidenceType? ResidenceType { get; set; }

        public string? PhysicalAddress { get; set; }


        // ==========================================
        // EMPLOYMENT CLASSIFICATION - STEP 3
        // ==========================================

        // Retained for compatibility with existing code.
        public EmploymentType EmploymentType { get; set; }

        public EmploymentStatus? EmploymentStatus { get; set; }

        public string? Occupation { get; set; }

        public string? Industry { get; set; }

        public string? EmployerOrBusinessName { get; set; }

        public int? BusinessYearsInOperation { get; set; }


        // ==========================================
        // EMPLOYMENT DETAILS
        // ==========================================

        public string? JobTitle { get; set; }

        public EmploymentContractType? EmploymentContractType { get; set; }

        public DateOnly? EmploymentStartDate { get; set; }

        // Retained for future employment verification.
        public DateOnly? EmploymentEndDate { get; set; }

        public DateOnly? ProbationEndDate { get; set; }


        // ==========================================
        // EMPLOYER CONTACT DETAILS
        // ==========================================

        public string? EmployerAddress { get; set; }

        public string? EmployerContactPerson { get; set; }

        public string? EmployerPhoneNumber { get; set; }

        public string? EmployerEmail { get; set; }


        // ==========================================
        // FINANCIAL INFORMATION - STEP 3
        // ==========================================

        // Retained for compatibility with existing code.
        public decimal? MonthlyIncome { get; set; }

        public decimal? GrossMonthlySalary { get; set; }

        public decimal? NetMonthlySalary { get; set; }

        public decimal? OtherMonthlyIncome { get; set; }

        public string? OtherIncomeSource { get; set; }

        public PayrollFrequency? PayrollFrequency { get; set; }


        // ==========================================
        // PROFILE ONBOARDING TRACKING
        // ==========================================

        public bool IsProfileComplete { get; set; } = false;

        public int ProfileCompletionStep { get; set; } = 1;

        public DateTime? ProfileCompletedAt { get; set; }


        // ==========================================
        // KYC AND ACCOUNT STATUS
        // ==========================================

        public KycStatus KycStatus { get; set; } = KycStatus.NotStarted;

        public bool IsActive { get; set; } = true;


        // ==========================================
        // RELATIONSHIPS
        // ==========================================

        public ICollection<BorrowerVerificationDocument> VerificationDocuments
        {
            get;
            set;
        } = new List<BorrowerVerificationDocument>();

        public ICollection<LoanApplication> LoanApplications { get; set; }
            = new List<LoanApplication>();

        public ICollection<Loan> Loans { get; set; }
            = new List<Loan>();
    }
}
