using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

public record TransitionResult(bool Succeeded, string? Error = null)
{
    public static TransitionResult Ok() => new(true);
    public static TransitionResult Fail(string error) => new(false, error);
}

public interface IApplicationWorkflowService
{
    Task<TransitionResult> TransitionAsync(Guid applicationId, LoanApplicationStatus newStatus,
        string actingUserId, string? notes = null, CancellationToken ct = default);

    bool IsTransitionAllowed(LoanApplicationStatus from, LoanApplicationStatus to);
}