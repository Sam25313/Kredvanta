
using System.ComponentModel.DataAnnotations;

namespace Kredvanta.ViewModels.LoanProducts
{
    public class LoanProductRequirementViewModel
    {
        public Guid Id { get; set; }

        [Required]
        public Guid LoanProductId { get; set; }

        public string LoanProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Document Name")]
        public string DocumentName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Range(1, 120)]
        [Display(Name = "Required Period (Months)")]
        public int? RequiredPeriodMonths { get; set; }

        [Display(Name = "Mandatory Document")]
        public bool IsMandatory { get; set; } = true;

        [Display(Name = "Requirement Active")]
        public bool IsActive { get; set; } = true;
    }
}
