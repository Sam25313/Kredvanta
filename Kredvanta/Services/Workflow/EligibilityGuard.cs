using Kredvanta.Models.Applications;
using Kredvanta.Services.Eligibility;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

public class EligibilityGuard : ITransitionGuard
{
    private readonly IEligibilityService _eligibility;
    public EligibilityGuard(IEligibilityService eligibility) => _eligibility = eligibility;

    public async Task<string?> CheckAsync(LoanApplication app, LoanApplicationStatus to,
                                          CancellationToken ct)
    {
        if (to != LoanApplicationStatus.Submitted) return null;

        var result = await _eligibility.CheckAsync(app.BorrowerId, ct);
        return result.IsEligible ? null : string.Join(" ", result.Reasons);
    }
}