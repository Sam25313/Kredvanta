using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

/// <summary>
/// Blocks submission while a current document is still being screened, so officers never
/// receive a half-checked application. Flagged documents do NOT block: the applicant may
/// replace them or submit anyway, and the officer decides.
/// Entries stuck for more than 10 minutes (e.g. worker down) are ignored so nobody is locked out.
/// </summary>
public class ScreeningCompleteGuard : ITransitionGuard
{
    private readonly ApplicationDbContext _db;
    public ScreeningCompleteGuard(ApplicationDbContext db) => _db = db;

    public async Task<string?> CheckAsync(LoanApplication app, LoanApplicationStatus to,
                                          CancellationToken ct)
    {
        if (to != LoanApplicationStatus.Submitted) return null;

        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        var stillChecking = await _db.LoanApplicationDocuments.AnyAsync(d =>
            d.LoanApplicationId == app.Id &&
            d.IsCurrent &&
            (d.ScreeningStatus == DocumentScreeningStatus.Queued ||
             d.ScreeningStatus == DocumentScreeningStatus.Scanning) &&
            d.CreatedAt > cutoff, ct);

        return stillChecking
            ? "Your documents are still being checked. Please wait for the checks to finish, then submit."
            : null;
    }
}