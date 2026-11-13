
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Applications
{
    public class CreditAssessment : BaseEntity
    {
        public Guid LoanApplicationId { get; set; }

        public LoanApplication LoanApplication { get; set; } = null!;

        public string AssessedByUserId { get; set; } = string.Empty;

        public ApplicationUser? AssessedByUser { get; set; }

        public decimal MonthlyIncome { get; set; }

        public decimal MonthlyExpenses { get; set; }

        public decimal ExistingMonthlyLoanPayments { get; set; }

        public decimal OtherMonthlyObligations { get; set; }

        public decimal DisposableIncome { get; set; }

        public decimal ProposedMonthlyInstallment { get; set; }

        public decimal DebtServiceRatio { get; set; }

        public CreditRecommendation Recommendation { get; set; }
            = CreditRecommendation.Pending;

        public string? AssessmentNotes { get; set; }

        public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
    }
}
