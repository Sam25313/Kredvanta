using Kredvanta.Models.Applications;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

public interface ITransitionGuard
{
    /// Return an error message to block the transition, or null to allow it.
    Task<string?> CheckAsync(LoanApplication application, LoanApplicationStatus to,
                             CancellationToken ct);
}