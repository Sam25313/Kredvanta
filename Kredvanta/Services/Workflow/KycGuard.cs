using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

public class KycGuard : ITransitionGuard
{
    private readonly ApplicationDbContext _db;
    public KycGuard(ApplicationDbContext db) => _db = db;

    public async Task<string?> CheckAsync(LoanApplication app, LoanApplicationStatus to,
                                          CancellationToken ct)
    {
        if (to != LoanApplicationStatus.Recommended) return null;

        var verified = await _db.Borrowers.AsNoTracking()
            .AnyAsync(b => b.Id == app.BorrowerId && b.KycStatus == KycStatus.Verified, ct);

        return verified ? null : "Identity verification (KYC) must be completed before a recommendation.";
    }
}