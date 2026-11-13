
using System.ComponentModel.DataAnnotations;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Borrowers
{
    public class BorrowerCreateViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date of Birth")]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        public Gender? Gender { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "National ID Number")]
        public string NationalIdNumber { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        [Display(Name = "Alternative Phone Number")]
        public string? AlternativePhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [Required]
        [StringLength(100)]
        public string County { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SubCounty { get; set; }

        [StringLength(300)]
        [Display(Name = "Physical Address")]
        public string? PhysicalAddress { get; set; }

        [Required]
        [Display(Name = "Employment Type")]
        public EmploymentType EmploymentType { get; set; }

        [StringLength(150)]
        [Display(Name = "Employer or Business Name")]
        public string? EmployerOrBusinessName { get; set; }

        [Range(0, 999999999999.99)]
        [Display(Name = "Monthly Income (KES)")]
        public decimal? MonthlyIncome { get; set; }
    }
}
