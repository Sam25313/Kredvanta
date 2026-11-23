using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

// Checks the persisted state that must exist before the signature is recorded.
public class AgreementReadinessGuard : ITransitionGuard
{
    private readonly ApplicationDbContext _db;
    public AgreementReadinessGuard(ApplicationDbContext db) => _db = db;

    public async Task<string?> CheckAsync(LoanApplication app, LoanApplicationStatus to, CancellationToken ct)
    {
        if (to != LoanApplicationStatus.ReadyForDisbursement) return null;

        var agreement = await _db.LoanAgreements.AsNoTracking()
            .Include(a => a.LoanOffer)
            .Where(a => a.LoanApplicationId == app.Id && a.Status == AgreementStatus.Generated)
            .OrderByDescending(a => a.GeneratedAt).FirstOrDefaultAsync(ct);

        if (agreement is null) return "There is no agreement waiting for a signature.";
        if (agreement.LoanOffer.Status != OfferStatus.Accepted) return "The offer has not been accepted.";
        if (agreement.LoanOffer.ExpiresAt <= DateTime.UtcNow) return "The offer has expired.";
        if (OfferService.Hash(agreement.Text) != agreement.Sha256Hash) return "The agreement failed its integrity check.";
        return null;
    }
}