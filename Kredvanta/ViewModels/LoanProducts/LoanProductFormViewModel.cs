
using System.ComponentModel.DataAnnotations;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.LoanProducts
{
    public class LoanProductFormViewModel
    {
        [Required]
        [StringLength(20)]
        [Display(Name = "Product Code")]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Product Category")]
        public string Category { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Product Description")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Product Image URL")]
        public string? ImageUrl { get; set; }

        [Required]
        [Range(1, 100000000)]
        [Display(Name = "Minimum Loan Amount (KES)")]
        public decimal MinimumAmount { get; set; }

        [Required]
        [Range(1, 100000000)]
        [Display(Name = "Maximum Loan Amount (KES)")]
        public decimal MaximumAmount { get; set; }

        [Required]
        [Range(0, 100)]
        [Display(Name = "Annual Interest Rate (%)")]
        public decimal AnnualInterestRate { get; set; }

        [Display(Name = "Interest Calculation Method")]
        public InterestMethod InterestMethod { get; set; }

        [Display(Name = "Repayment Frequency")]
        public RepaymentFrequency RepaymentFrequency { get; set; }

        [Required]
        [Range(1, 120)]
        [Display(Name = "Minimum Repayment Periods")]
        public int MinimumTermPeriods { get; set; }

        [Required]
        [Range(1, 120)]
        [Display(Name = "Maximum Repayment Periods")]
        public int MaximumTermPeriods { get; set; }

        [Range(0, 100)]
        [Display(Name = "Processing Fee Percentage (%)")]
        public decimal ProcessingFeePercentage { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Fixed Processing Fee (KES)")]
        public decimal ProcessingFeeFixed { get; set; }

        [Range(0, 365)]
        [Display(Name = "Grace Period (Days)")]
        public int GracePeriodDays { get; set; }

        [Display(Name = "Product Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Allow Loan Applications")]
        public bool IsAvailableForApplication { get; set; } = false;
    }
}
