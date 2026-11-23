using Kredvanta.Models.Applications;
using Kredvanta.Models.Common;
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
        public int ApprovalLevel { get; set; } = 1;

        public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;

        // NEW: amount the manager approved (may be lower than requested)
        public decimal? ApprovedAmount { get; set; }

        // NEW: conditions attached to a conditional approval
        public string? Conditions { get; set; }

        public string? Comments { get; set; }

       // public DateTime? DecisionDate { get; set; }
        public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
    }
}