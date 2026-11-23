using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Services.Audit;
using Microsoft.EntityFrameworkCore;
using S = Kredvanta.Models.Enums.LendingEnums.LoanApplicationStatus;

namespace Kredvanta.Services.Workflow;

public class ApplicationWorkflowService : IApplicationWorkflowService
{
    private static readonly Dictionary<S, S[]> Allowed = new()
    {
        [S.Draft] = [S.Submitted, S.Cancelled],
        [S.Submitted] = [S.UnderReview, S.Cancelled],
        [S.UnderReview] = [S.DocumentsRequired, S.Recommended, S.Rejected, S.Cancelled],
        [S.DocumentsRequired] = [S.Submitted, S.UnderReview, S.Cancelled],
        [S.Recommended] = [S.Approved, S.Rejected, S.UnderReview, S.Cancelled],
        [S.Approved] = [S.Cancelled],
        [S.OfferIssued] = [S.AgreementPending, S.Approved, S.Cancelled],
        [S.AgreementPending] = [S.ReadyForDisbursement, S.Approved, S.Cancelled],
        [S.ReadyForDisbursement] = [S.Disbursed, S.Cancelled],
        [S.Rejected] = [],
        [S.Cancelled] = [],
    };

    // Who may move an application INTO each status.
    private static readonly Dictionary<S, string[]> RolesFor = new()
    {
        [S.Submitted] = [AppRoles.Borrower],
        [S.UnderReview] = [AppRoles.LoanOfficer, AppRoles.KycOfficer, AppRoles.VerificationOfficer,
                                AppRoles.CreditAnalyst, AppRoles.CreditManager],
        [S.DocumentsRequired] = [AppRoles.LoanOfficer, AppRoles.KycOfficer, AppRoles.VerificationOfficer,
                                AppRoles.CreditAnalyst],
        [S.Recommended] = [AppRoles.CreditAnalyst],
        [S.Approved] = [AppRoles.CreditManager],
        [S.Rejected] = [AppRoles.CreditManager],
        [S.OfferIssued] = [AppRoles.LoanOfficer],
        [S.AgreementPending] = [AppRoles.Borrower],
        [S.ReadyForDisbursement] = [AppRoles.Borrower],
        [S.Disbursed] = [AppRoles.FinanceOfficer],
        [S.Cancelled] = [AppRoles.Borrower, AppRoles.LoanOfficer, AppRoles.CreditManager],
    };

    private static readonly HashSet<S> ReasonRequired =
        [S.Rejected, S.Cancelled, S.DocumentsRequired];

    private readonly ApplicationDbContext _db;
    private readonly IUserRoleReader _roles;
    private readonly IAuditService _audit;
    private readonly IEnumerable<ITransitionGuard> _guards;

    public ApplicationWorkflowService(ApplicationDbContext db, IUserRoleReader roles,
        IAuditService audit, IEnumerable<ITransitionGuard> guards)
    {
        _db = db; _roles = roles; _audit = audit; _guards = guards;
    }

    public bool IsTransitionAllowed(S from, S to)
        => Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public async Task<TransitionResult> TransitionAsync(Guid applicationId, S to,
        string actingUserId, string? notes = null, CancellationToken ct = default)
    {
        var app = await _db.LoanApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is null) return TransitionResult.Fail("Application not found.");

        var from = app.Status;
        if (!IsTransitionAllowed(from, to))
            return TransitionResult.Fail($"Cannot move from {from} to {to}.");

        if (ReasonRequired.Contains(to) && string.IsNullOrWhiteSpace(notes))
            return TransitionResult.Fail($"A reason is required for {to}.");

        // --- role check ---
        var roles = await _roles.GetRolesAsync(actingUserId, ct);
        if (roles is null) return TransitionResult.Fail("Unknown user.");

        if (!RolesFor.TryGetValue(to, out var permitted) || !permitted.Any(p => roles.Contains(p)))
            return await Denied(app, to, "Role not permitted for this transition.");

        // --- borrowers may only act on their own application ---
        var isBorrowerOnly = roles.Contains(AppRoles.Borrower) && roles.All(r => r == AppRoles.Borrower);
        if (isBorrowerOnly)
        {
            var owns = await _db.Borrowers.AnyAsync(b =>
                b.Id == app.BorrowerId && b.ApplicationUserId == actingUserId, ct);
            if (!owns) return await Denied(app, to, "Not the application owner.");
        }

        // --- separation of duties: the recommender cannot give final approval ---
        if (to == S.Approved)
        {
            var recommender = await _db.LoanApplicationStatusHistories
                .Where(h => h.LoanApplicationId == app.Id && h.NewStatus == S.Recommended)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => h.ChangedByUserId)
                .FirstOrDefaultAsync(ct);
            if (recommender == actingUserId)
                return await Denied(app, to, "Recommender cannot give final approval.");
        }

        // --- pluggable business guards ---
        foreach (var guard in _guards)
        {
            var error = await guard.CheckAsync(app, to, ct);
            if (error is not null) return TransitionResult.Fail(error);
        }

        // --- apply: status + history + audit in ONE SaveChanges ---
        var now = DateTime.UtcNow;
        app.Status = to;

        if (to == S.Submitted) app.SubmittedAt = now;
        if (to is S.Approved or S.Rejected) app.DecisionAt = now;
        if (to == S.Cancelled) app.CancellationReason = notes;

        _db.LoanApplicationStatusHistories.Add(new LoanApplicationStatusHistory
        {
            LoanApplicationId = app.Id,
            PreviousStatus = from,
            NewStatus = to,
            ChangedAt = now,
            ChangedByUserId = actingUserId,
            Notes = notes
        });

        _audit.Record("StatusChanged", nameof(LoanApplication), app.Id.ToString(),
            notes, new { From = from.ToString(), To = to.ToString(), Roles = roles });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TransitionResult.Fail("The application was changed by someone else. Reload and retry.");
        }

        return TransitionResult.Ok();
    }

    private async Task<TransitionResult> Denied(LoanApplication app, S to, string why)
    {
        await _audit.LogAsync("TransitionDenied", nameof(LoanApplication), app.Id.ToString(),
            why, new { Attempted = to.ToString() });
        return TransitionResult.Fail("You are not authorised to perform this action.");
    }
}