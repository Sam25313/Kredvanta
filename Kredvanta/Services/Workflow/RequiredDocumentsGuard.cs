using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Workflow;

public class RequiredDocumentsGuard : ITransitionGuard
{
    private readonly ApplicationDbContext _db;
    public RequiredDocumentsGuard(ApplicationDbContext db) => _db = db;

    public async Task<string?> CheckAsync(LoanApplication app, LoanApplicationStatus to,
                                          CancellationToken ct)
    {
        if (to is not (LoanApplicationStatus.Submitted or LoanApplicationStatus.Recommended))
            return null;

        var requiredDocuments = await _db.LoanProductRequiredDocuments
            .Where(r => r.LoanProductId == app.LoanProductId && r.IsMandatory && r.IsActive)
            .Select(r => new { r.Id, r.DocumentName })
            .ToListAsync(ct);

        var provided = await _db.LoanApplicationDocuments
            .Where(d => d.LoanApplicationId == app.Id
                     && d.IsCurrent
                     && d.LoanProductRequiredDocumentId != null)
            .Select(d => new { Id = d.LoanProductRequiredDocumentId!.Value, d.ReviewStatus })
            .ToListAsync(ct);

        var missing = requiredDocuments
            .Where(required => !provided.Any(document =>
                document.Id == required.Id &&
                document.ReviewStatus is DocumentReviewStatus.Pending or DocumentReviewStatus.Verified))
            .Select(required => required.DocumentName)
            .ToList();
        if (missing.Count > 0)
            return $"Upload or replace these required documents before submitting: {string.Join(", ", missing)}.";

        if (to == LoanApplicationStatus.Recommended)
        {
            var unverified = requiredDocuments
                .Where(required => !provided.Any(document =>
                    document.Id == required.Id && document.ReviewStatus == DocumentReviewStatus.Verified))
                .Select(required => required.DocumentName)
                .ToList();
            if (unverified.Count > 0)
                return $"These required documents must be verified before the application can be recommended: {string.Join(", ", unverified)}.";
        }

        return null;
    }
}