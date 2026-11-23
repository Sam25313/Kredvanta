using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Models.Loans;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Workflow;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Credit;

public enum ApprovalAction { Approve, ApproveWithConditions, Decline }

public record ApprovalResult(bool Succeeded, string? Message = null, string? Error = null)
{
    public static ApprovalResult Fail(string e) => new(false, null, e);
    public static ApprovalResult Ok(string m) => new(true, m);
}

public interface IApprovalService
{
    Task<ApprovalResult> DecideAsync(Guid applicationId, ApprovalAction action, decimal? approvedAmount,
        string? conditions, string? reason, string managerId, CancellationToken ct = default);
}

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _db;
    private readonly IApplicationWorkflowService _workflow;
    private readonly IAuditService _audit;

    public ApprovalService(ApplicationDbContext db, IApplicationWorkflowService workflow, IAuditService audit)
    { _db = db; _workflow = workflow; _audit = audit; }

    public async Task<ApprovalResult> DecideAsync(Guid applicationId, ApprovalAction action,
        decimal? approvedAmount, string? conditions, string? reason, string managerId,
        CancellationToken ct = default)
    {
        var app = await _db.LoanApplications.Include(a => a.Borrower).Include(a => a.LoanProduct)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is null) return ApprovalResult.Fail("Application not found.");

        if (app.Status != LoanApplicationStatus.Recommended)
            return ApprovalResult.Fail("Only recommended applications can be decided.");

        if (app.Borrower.ApplicationUserId == managerId)
            return ApprovalResult.Fail("You cannot decide your own application.");

        var recommender = await _db.LoanApplicationStatusHistories
            .Where(h => h.LoanApplicationId == applicationId && h.NewStatus == LoanApplicationStatus.Recommended)
            .OrderByDescending(h => h.ChangedAt).Select(h => h.ChangedByUserId).FirstOrDefaultAsync(ct);
        if (recommender == managerId)
            return ApprovalResult.Fail("The person who recommended this application cannot decide it.");

        var priorApprovals = await _db.LoanApprovals
            .Where(a => a.LoanApplicationId == applicationId && a.Decision == ApprovalDecision.Approved)
            .ToListAsync(ct);

        // ---------- Decline ----------
        if (action == ApprovalAction.Decline)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return ApprovalResult.Fail("A reason is required to decline.");

            AddRow(applicationId, managerId, priorApprovals.Count + 1, ApprovalDecision.Rejected,
                null, null, reason);

            var declined = await _workflow.TransitionAsync(applicationId, LoanApplicationStatus.Rejected,
                managerId, reason, ct);
            return Finish(declined, "Application declined.");
        }

        // ---------- Approve ----------
        var assessment = await _db.CreditAssessments.AsNoTracking()
            .Where(a => a.LoanApplicationId == applicationId && a.IsCurrent)
            .OrderByDescending(a => a.AssessedAt).FirstOrDefaultAsync(ct);

        if (assessment is null ||
            assessment.Recommendation is not (CreditRecommendation.Recommend or CreditRecommendation.RecommendWithConditions))
            return ApprovalResult.Fail("There is no supporting assessment. Decline, or send it back for reassessment.");

        if (assessment.AssessedAmount != app.RequestedAmount || assessment.AssessedTermPeriods != app.RequestedTermPeriods)
            return ApprovalResult.Fail("The request changed after the assessment. It must be reassessed.");

        var amount = approvedAmount ?? app.RequestedAmount;
        if (amount <= 0 || amount > app.RequestedAmount)
            return ApprovalResult.Fail("The approved amount cannot exceed the requested amount.");
        if (amount < app.LoanProduct.MinimumAmount)
            return ApprovalResult.Fail("The approved amount is below the product minimum.");

        if (action == ApprovalAction.ApproveWithConditions && string.IsNullOrWhiteSpace(conditions))
            return ApprovalResult.Fail("State the conditions of the approval.");

        var policy = await _db.CreditPolicyVersions.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive, ct);
        if (policy is null) return ApprovalResult.Fail("No credit policy is active.");

        var dualRequired = policy.DualApprovalAbove is { } limit && app.RequestedAmount > limit;

        if (priorApprovals.Any(a => a.ReviewedByUserId == managerId))
            return ApprovalResult.Fail("You have already approved this application. A different credit manager must give the second approval.");

        var cond = action == ApprovalAction.ApproveWithConditions ? conditions!.Trim() : null;

        // First of two approvals: record it, do not change the status yet.
        if (dualRequired && priorApprovals.Count == 0)
        {
            AddRow(applicationId, managerId, 1, ApprovalDecision.Approved, amount, cond, reason);
            _audit.Record("ApprovalFirstOfTwo", nameof(LoanApplication), applicationId.ToString(), null,
                new { amount, Policy = $"v{policy.VersionNumber}" });
            await _db.SaveChangesAsync(ct);
            return ApprovalResult.Ok("First approval recorded. A different credit manager must give the second approval.");
        }

        // The second approver must agree with the amount and conditions the first set.
        if (dualRequired)
        {
            var first = priorApprovals.OrderBy(a => a.ApprovalLevel).First();
            if (first.ApprovedAmount != amount)
                return ApprovalResult.Fail($"The first approver approved KES {first.ApprovedAmount:N2}. Match it, or decline.");
        }

        AddRow(applicationId, managerId, priorApprovals.Count + 1, ApprovalDecision.Approved, amount, cond, reason);

        var done = await _workflow.TransitionAsync(applicationId, LoanApplicationStatus.Approved, managerId,
            cond is null ? "Approved." : $"Approved with conditions: {cond}", ct);
        return Finish(done, cond is null ? "Application approved." : "Application approved with conditions.");
    }

    private void AddRow(Guid appId, string managerId, int level, ApprovalDecision decision,
        decimal? amount, string? conditions, string? comments)
        => _db.LoanApprovals.Add(new LoanApproval
        {
            LoanApplicationId = appId,
            ReviewedByUserId = managerId,
            ApprovalLevel = level,
            Decision = decision,
            ApprovedAmount = amount,
            Conditions = conditions,
            Comments = comments?.Trim(),
            DecidedAt = DateTime.UtcNow
        });

    private ApprovalResult Finish(TransitionResult t, string ok)
    {
        if (t.Succeeded) return ApprovalResult.Ok(ok);

        // The transition failed, so discard the approval row it would have saved.
        foreach (var e in _db.ChangeTracker.Entries<LoanApproval>()
                     .Where(e => e.State == EntityState.Added).ToList())
            e.State = EntityState.Detached;
        return ApprovalResult.Fail(t.Error ?? "The decision could not be recorded.");
    }
}