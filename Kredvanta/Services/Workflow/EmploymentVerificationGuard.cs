using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

public class EmploymentVerificationGuard : ITransitionGuard
{
    private readonly ApplicationDbContext _db;
    public EmploymentVerificationGuard(ApplicationDbContext db) => _db = db;

    public async Task<string?> CheckAsync(LoanApplication app, LoanApplicationStatus to,
                                          CancellationToken ct)
    {
        if (to != LoanApplicationStatus.Recommended) return null;

        var latest = await _db.EmploymentVerifications.AsNoTracking()
            .Where(v => v.LoanApplicationId == app.Id)
            .OrderByDescending(v => v.AttemptedAt)
            .Select(v => new { v.Outcome, v.ContactIndependentlySourced })
            .FirstOrDefaultAsync(ct);

        return latest is { Outcome: EmploymentVerificationOutcome.Verified, ContactIndependentlySourced: true }
            ? null
            : "Employment must be independently verified before a recommendation.";
    }
}