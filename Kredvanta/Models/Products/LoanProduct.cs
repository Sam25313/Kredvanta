
using Kredvanta.Models.Applications;
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Products
{
    public class LoanProduct : BaseEntity
    {
        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? Description { get; set; }

        public decimal MinimumAmount { get; set; }

        public decimal MaximumAmount { get; set; }

        // Annual nominal interest rate, expressed as a percentage.
        public decimal AnnualInterestRate { get; set; }

        public InterestMethod InterestMethod { get; set; }

        public RepaymentFrequency RepaymentFrequency { get; set; }

        // Number of instalments, not necessarily months.
        public int MinimumTermPeriods { get; set; }

        public int MaximumTermPeriods { get; set; }

        public decimal ProcessingFeePercentage { get; set; }

        public decimal ProcessingFeeFixed { get; set; }

        public int GracePeriodDays { get; set; }

        // Controls catalogue visibility.
        public bool IsActive { get; set; } = true;

        // Controls whether borrowers can submit applications.
        public bool IsAvailableForApplication { get; set; } = false;

        public ICollection<LoanProductRequiredDocument> RequiredDocuments
        { get; set; } = new List<LoanProductRequiredDocument>();

        public ICollection<LoanApplication> LoanApplications
        { get; set; } = new List<LoanApplication>();
    }
}
