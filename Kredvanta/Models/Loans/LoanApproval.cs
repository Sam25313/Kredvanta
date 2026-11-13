
using Kredvanta.Models.Applications;
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Loans
{
    public class LoanApproval : BaseEntity
    {
        public Guid LoanApplicationId { get; set; }

        public LoanApplication LoanApplication { get; set; } = null!;

        public string ReviewedByUserId { get; set; } = string.Empty;

        public ApplicationUser? ReviewedByUser { get; set; }

        // Supports multiple approval levels
        public int ApprovalLevel { get; set; }

        public ApprovalDecision Decision { get; set; }
            = ApprovalDecision.Pending;

        public string? Comments { get; set; }

        public DateTime? DecisionDate { get; set; }
    }
}

