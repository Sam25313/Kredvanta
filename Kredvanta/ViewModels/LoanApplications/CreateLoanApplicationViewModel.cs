using System.ComponentModel.DataAnnotations;

namespace Kredvanta.ViewModels.LoanApplications
{
    public class CreateLoanApplicationViewModel
    {
        [Required(ErrorMessage = "Please select a loan product.")]
        public Guid LoanProductId { get; set; }

        [Required]
        [Range(1, double.MaxValue,
            ErrorMessage = "Requested amount must be greater than zero.")]
        public decimal RequestedAmount { get; set; }

        [Required]
        [Range(1, 120,
            ErrorMessage = "Please enter a valid repayment term.")]
        public int RequestedTermPeriods { get; set; }

        [Required(ErrorMessage = "Please explain the purpose of this loan.")]
        [StringLength(1000)]
        public string LoanPurpose { get; set; } = string.Empty;

        public List<LoanProductOptionViewModel> AvailableProducts { get; set; } = new();
    }

    public class LoanProductOptionViewModel
    {
        public Guid Id { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal MinimumAmount { get; set; }

        public decimal MaximumAmount { get; set; }

        public decimal AnnualInterestRate { get; set; }

        public int MinimumTermPeriods { get; set; }

        public int MaximumTermPeriods { get; set; }

        public string RepaymentFrequency { get; set; } = string.Empty;

        public string InterestMethod { get; set; } = string.Empty;
    }
}