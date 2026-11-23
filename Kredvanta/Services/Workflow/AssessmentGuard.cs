using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

public class AssessmentGuard : ITransitionGuard
{
    private readonly ApplicationDbContext _db;
    public AssessmentGuard(ApplicationDbContext db) => _db = db;

    public async Task<string?> CheckAsync(LoanApplication app, LoanApplicationStatus to, CancellationToken ct)
    {
        if (to != LoanApplicationStatus.Recommended) return null;

        var a = await _db.CreditAssessments.AsNoTracking()
            .Where(x => x.LoanApplicationId == app.Id && x.IsCurrent)
            .OrderByDescending(x => x.AssessedAt).FirstOrDefaultAsync(ct);

        if (a is null) return "A credit assessment must be completed before a recommendation.";

        if (a.Recommendation is not (CreditRecommendation.Recommend or CreditRecommendation.RecommendWithConditions))
            return "The assessment does not support a recommendation.";

        // The assessment must still describe the request that is being recommended.
        if (a.AssessedAmount != app.RequestedAmount || a.AssessedTermPeriods != app.RequestedTermPeriods)
            return "The requested amount or term changed after the assessment. Reassess first.";

        return null;
    }
}