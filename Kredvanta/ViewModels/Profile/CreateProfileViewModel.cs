
using System.ComponentModel.DataAnnotations;
using Kredvanta.Models.Enums;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Profile
{
    public class CreateProfileViewModel
    {
        // ==========================================
        // ONBOARDING CONTROL
        // ==========================================

        public int CurrentStep { get; set; } = 1;


        // ==========================================
        // STEP 1: PERSONAL IDENTIFICATION
        // ==========================================

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public Gender? Gender { get; set; }

        [Required(ErrorMessage = "Select your identification document type.")]
        [Display(Name = "Identification Type")]
        public IdentificationType? IdentificationType { get; set; }

        [Required(ErrorMessage = "Identification number is required.")]
        [StringLength(30)]
        [Display(Name = "National ID / Passport Number")]
        public string NationalIdNumber { get; set; } = string.Empty;


        // ==========================================
        // STEP 2: CONTACT INFORMATION
        // ==========================================

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        [Display(Name = "Primary Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Enter a valid alternative phone number.")]
        [StringLength(20)]
        [Display(Name = "Alternative Phone Number")]
        public string? AlternativePhoneNumber { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;


        // ==========================================
        // STEP 2: RESIDENTIAL INFORMATION
        // ==========================================

        [Required(ErrorMessage = "County is required.")]
        [StringLength(100)]
        public string County { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sub-county is required.")]
        [StringLength(100)]
        [Display(Name = "Sub-County")]
        public string? SubCounty { get; set; }

        [Required(ErrorMessage = "Ward is required.")]
        [StringLength(100)]
        public string? Ward { get; set; }

        [Required(ErrorMessage = "Estate or village is required.")]
        [StringLength(150)]
        [Display(Name = "Estate / Village")]
        public string? EstateOrVillage { get; set; }

        [Required(ErrorMessage = "Select your residence type.")]
        [Display(Name = "Residence Type")]
        public ResidenceType? ResidenceType { get; set; }

        [Required(ErrorMessage = "Physical address is required.")]
        [StringLength(250)]
        [Display(Name = "Physical Address")]
        public string? PhysicalAddress { get; set; }


        // ==========================================
        // STEP 3: EMPLOYMENT INFORMATION
        // ==========================================

        [Required(ErrorMessage = "Select your employment status.")]
        [Display(Name = "Employment Status")]
        public EmploymentStatus? EmploymentStatus { get; set; }

        [StringLength(150)]
        [Display(Name = "Occupation")]
        public string? Occupation { get; set; }

        [StringLength(150)]
        [Display(Name = "Industry / Business Sector")]
        public string? Industry { get; set; }

        [StringLength(200)]
        [Display(Name = "Employer or Business Name")]
        public string? EmployerOrBusinessName { get; set; }

        [Display(Name = "Employment Contract Type")]
        public EmploymentContractType? EmploymentContractType { get; set; }

        [Display(Name = "Employment Start Date")]
        [DataType(DataType.Date)]
        public DateOnly? EmploymentStartDate { get; set; }

        [Range(0, 100)]
        [Display(Name = "Years in Business")]
        public int? BusinessYearsInOperation { get; set; }


        // ==========================================
        // STEP 3: INCOME INFORMATION
        // ==========================================

        [Range(typeof(decimal), "0", "100000000",
            ErrorMessage = "Gross monthly income must be zero or greater.")]
        [Display(Name = "Gross Monthly Income (KES)")]
        public decimal? GrossMonthlySalary { get; set; }

        [Range(typeof(decimal), "0", "100000000",
            ErrorMessage = "Net monthly income must be zero or greater.")]
        [Display(Name = "Net Monthly Income (KES)")]
        public decimal? NetMonthlySalary { get; set; }

        [Range(typeof(decimal), "0", "100000000",
            ErrorMessage = "Other monthly income must be zero or greater.")]
        [Display(Name = "Other Monthly Income (KES)")]
        public decimal? OtherMonthlyIncome { get; set; }

        [StringLength(200)]
        [Display(Name = "Source of Other Income")]
        public string? OtherIncomeSource { get; set; }

        [Display(Name = "Payroll Frequency")]
        public PayrollFrequency? PayrollFrequency { get; set; }


        // ==========================================
        // LEGACY FIELDS - TEMPORARY COMPATIBILITY
        // ==========================================

        // Retain while older controller/view references are being updated.
        public EmploymentType EmploymentType { get; set; }

        [Range(typeof(decimal), "0", "100000000")]
        public decimal? MonthlyIncome { get; set; }

        public DateOnly? EmploymentEndDate { get; set; }

        public DateOnly? ProbationEndDate { get; set; }

        public string? EmployerAddress { get; set; }

        public string? EmployerContactPerson { get; set; }

        public string? EmployerPhoneNumber { get; set; }

        public string? EmployerEmail { get; set; }


        // ==========================================
        // STEP 4: REVIEW AND CONFIRMATION
        // ==========================================

        [Display(Name = "Privacy Notice Acknowledgement")]
        public bool PrivacyNoticeAccepted { get; set; }

        [Display(Name = "Information Accuracy Declaration")]
        public bool InformationAccurate { get; set; }
    }
}
