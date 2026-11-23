using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Storage;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Documents.Screening;

public interface IDocumentScreeningService
{
    Task ScreenAsync(Guid documentId, CancellationToken ct = default);
}

public class DocumentScreeningService : IDocumentScreeningService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IDocumentTextExtractor _extractor;
    private readonly IAuditService _audit;
    private readonly ILogger<DocumentScreeningService> _log;

    public DocumentScreeningService(ApplicationDbContext db, IFileStorageService storage,
        IDocumentTextExtractor extractor, IAuditService audit, ILogger<DocumentScreeningService> log)
    {
        _db = db; _storage = storage; _extractor = extractor; _audit = audit; _log = log;
    }

    public async Task ScreenAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.LoanApplicationDocuments
            .Include(d => d.RequiredDocument)
            .Include(d => d.LoanApplication).ThenInclude(a => a.Borrower)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (doc is null || !doc.IsCurrent) return;   // replaced or removed while queued
        if (doc.ScreeningStatus is DocumentScreeningStatus.Passed
                                or DocumentScreeningStatus.Flagged
                                or DocumentScreeningStatus.Failed) return;

        doc.ScreeningStatus = DocumentScreeningStatus.Scanning;
        doc.ScreeningStartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var checks = new List<ScreeningCheck>();
        DocumentScreeningStatus status;
        string summary;

        try
        {
            (status, summary) = await RunChecksAsync(doc, checks, ct);
        }
        catch (OperationCanceledException)
        {
            throw;   // shutting down: stays Scanning and is re-queued on next start
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Screening crashed for document {DocumentId}", documentId);
            status = DocumentScreeningStatus.Failed;
            summary = "Automated screening could not be completed. Officer review required.";
        }

        doc.ScreeningStatus = status;
        doc.ScreenedAt = DateTime.UtcNow;
        doc.ScreeningSummary = summary.Length > 500 ? summary[..500] : summary;
        doc.ScreeningDetailsJson = ScreeningJson.Serialize(checks);

        // Audit holds outcomes only: never extracted text or document contents.
        _audit.Record("DocumentScreened", nameof(LoanApplicationDocument), doc.Id.ToString(), summary,
            new
            {
                Status = status.ToString(),
                Checks = checks.Select(c => new { c.Name, Outcome = c.Outcome.ToString(), c.Required })
            });

        await _db.SaveChangesAsync(ct);
    }

    private async Task<(DocumentScreeningStatus, string)> RunChecksAsync(
        LoanApplicationDocument doc, List<ScreeningCheck> checks, CancellationToken ct)
    {
        // ---- Stage 1: file integrity -------------------------------------------------
        byte[] bytes;
        await using (var stream = await _storage.OpenReadAsync(doc.StorageKey, ct))
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        var actualHash = Convert.ToHexString(SHA256.HashData(bytes));
        if (!string.Equals(actualHash, doc.Sha256Hash, StringComparison.OrdinalIgnoreCase))
        {
            checks.Add(new ScreeningCheck("File integrity", CheckOutcome.Mismatch,
                "The stored file differs from the hash recorded at upload."));
            return (DocumentScreeningStatus.Failed, "File integrity check failed. Manual review required.");
        }
        checks.Add(new ScreeningCheck("File integrity", CheckOutcome.Match,
            "The file matches the hash recorded at upload."));

        // ---- Stage 2: duplicate detection --------------------------------------------
        var borrowerId = doc.LoanApplication.BorrowerId;

        var reusedInApplication = await _db.LoanApplicationDocuments.AnyAsync(d =>
            d.Id != doc.Id && d.IsCurrent &&
            d.LoanApplicationId == doc.LoanApplicationId &&
            d.Sha256Hash == doc.Sha256Hash, ct);
        checks.Add(reusedInApplication
            ? new ScreeningCheck("Duplicate in application", CheckOutcome.Mismatch,
                "The same file is uploaded for more than one requirement.")
            : new ScreeningCheck("Duplicate in application", CheckOutcome.Match,
                "The file is not reused for another requirement."));

        var usedByOtherBorrower = await _db.LoanApplicationDocuments.AnyAsync(d =>
            d.LoanApplication.BorrowerId != borrowerId &&
            d.Sha256Hash == doc.Sha256Hash, ct);
        checks.Add(usedByOtherBorrower
            ? new ScreeningCheck("Duplicate across borrowers", CheckOutcome.Mismatch,
                "An identical file exists on another borrower's application.")
            : new ScreeningCheck("Duplicate across borrowers", CheckOutcome.Match,
                "No identical file exists on another borrower's application."));

        // ---- Stage 3: read the content -----------------------------------------------
        await using var contentStream = new MemoryStream(bytes);
        var extraction = await _extractor.ExtractAsync(contentStream, doc.FileExtension, ct);
        if (!extraction.Succeeded)
        {
            checks.Add(new ScreeningCheck("Content reading", CheckOutcome.NotFound,
                extraction.Reason ?? "The content could not be read."));
            return (DocumentScreeningStatus.Flagged,
                "The content could not be read automatically. Officer review required.");
        }
        checks.Add(new ScreeningCheck("Content reading", CheckOutcome.Match,
            "Text was read from the document."));

        // ---- Stage 4: cross-match with the borrower profile --------------------------
        var kind = ScreeningRules.Classify(doc.RequiredDocument?.DocumentName);
        checks.AddRange(ScreeningRules.Evaluate(kind, doc.LoanApplication.Borrower,
            doc.RequiredDocument, extraction.Text, DateOnly.FromDateTime(DateTime.UtcNow)));

        // ---- Stage 5: outcome --------------------------------------------------------
        var attention = checks.Where(c => c.Required && c.Outcome != CheckOutcome.Match).ToList();
        if (attention.Count > 0)
            return (DocumentScreeningStatus.Flagged,
                $"{attention.Count} check(s) need officer attention: {string.Join(", ", attention.Select(c => c.Name))}.");

        return kind == DocumentKind.Unknown
            ? (DocumentScreeningStatus.Passed,
                "File checks passed. There are no content rules for this document type, so officer review is still needed.")
            : (DocumentScreeningStatus.Passed, $"All {checks.Count} automated checks passed.");
    }
}