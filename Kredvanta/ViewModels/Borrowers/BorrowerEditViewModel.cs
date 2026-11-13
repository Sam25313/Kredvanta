
using System.ComponentModel.DataAnnotations;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Borrowers
{
    public class BorrowerEditViewModel
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        public Gender? Gender { get; set; }

        [Required]
        [StringLength(30)]
        public string NationalIdNumber { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
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
        public string? PhysicalAddress { get; set; }

        [Required]
        public EmploymentType EmploymentType { get; set; }

        [StringLength(150)]
        public string? EmployerOrBusinessName { get; set; }

        [Range(0, 999999999999.99)]
        public decimal? MonthlyIncome { get; set; }
    }
}
