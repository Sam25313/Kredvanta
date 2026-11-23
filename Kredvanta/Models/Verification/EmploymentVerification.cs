using Kredvanta.Models.Applications;
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Verification;

/// One row per verification ATTEMPT. The latest row is the current state.
public class EmploymentVerification : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public LoanApplication LoanApplication { get; set; } = null!;

    public VerificationChannel Channel { get; set; }
    public string ContactedParty { get; set; } = string.Empty;      // department or person
    public string? ContactPersonName { get; set; }
    public string ContactDetailUsed { get; set; } = string.Empty;   // number or email used
    public bool ContactIndependentlySourced { get; set; }

    public EmploymentVerificationOutcome Outcome { get; set; }

    public string? ConfirmedJobTitle { get; set; }
    public EmploymentContractType? ConfirmedContractType { get; set; }
    public DateOnly? ConfirmedStartDate { get; set; }
    public decimal? ConfirmedNetSalary { get; set; }

    public string? Notes { get; set; }

    public string VerifiedByUserId { get; set; } = string.Empty;
    public ApplicationUser? VerifiedByUser { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}