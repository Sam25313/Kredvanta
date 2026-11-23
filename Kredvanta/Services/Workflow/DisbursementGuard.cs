using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

public class DisbursementGuard : ITransitionGuard
{
    private readonly ApplicationDbContext _db;
    public DisbursementGuard(ApplicationDbContext db) => _db = db;

    public async Task<string?> CheckAsync(LoanApplication app, LoanApplicationStatus to, CancellationToken ct)
    {
        if (to != LoanApplicationStatus.Disbursed) return null;

        var signed = await _db.AgreementSignatures.AsNoTracking().AnyAsync(s =>
            s.LoanAgreement.LoanApplicationId == app.Id &&
            s.LoanAgreement.Status == AgreementStatus.Signed, ct);

        return signed ? null : "The agreement has not been signed.";
    }
}