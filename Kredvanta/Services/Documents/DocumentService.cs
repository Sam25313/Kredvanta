using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Documents.Screening;
using Kredvanta.Services.Storage;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Documents;

public class DocumentService : IDocumentService
{
    private static readonly LoanApplicationStatus[] UploadStatuses =
        { LoanApplicationStatus.Draft, LoanApplicationStatus.DocumentsRequired };

    private static readonly LoanApplicationStatus[] ReviewStatuses =
        { LoanApplicationStatus.UnderReview, LoanApplicationStatus.DocumentsRequired };

    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IAuditService _audit;
    private readonly IDocumentScreeningQueue _queue;

    public DocumentService(ApplicationDbContext db, IFileStorageService storage, IAuditService audit, IDocumentScreeningQueue queue)
    {
        _db = db; _storage = storage; _audit = audit; _queue = queue;
    }

    public async Task<DocumentResult> UploadAsync(Guid applicationId, Guid requiredDocumentId,
        Stream content, string originalFileName, string userId, CancellationToken ct = default)
    {
        var app = await _db.LoanApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is null) return DocumentResult.Fail("Application not found.");

        if (!UploadStatuses.Contains(app.Status))
            return DocumentResult.Fail("Documents can no longer be uploaded for this application.");

        var requirementValid = await _db.LoanProductRequiredDocuments.AnyAsync(r =>
            r.Id == requiredDocumentId &&
            r.LoanProductId == app.LoanProductId &&
            r.IsActive, ct);
        if (!requirementValid) return DocumentResult.Fail("Unknown document requirement.");

        // Validates type, size and file signature; stores under an opaque key.
        StoredFile stored;
        try
        {
            stored = await _storage.SaveAsync(content, originalFileName, ct);
        }
        catch (FileRejectedException ex)
        {
            await _audit.LogAsync("DocumentUploadRejected", nameof(LoanApplication),
                applicationId.ToString(), ex.Message,
                new { FileExtension = Path.GetExtension(originalFileName) }, ct);
            return DocumentResult.Fail(ex.Message);
        }

        // The filtered unique index allows ONE current row per requirement, so the
        // old row is retired first, inside the same transaction.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var previous = await _db.LoanApplicationDocuments.FirstOrDefaultAsync(d =>
            d.LoanApplicationId == applicationId &&
            d.LoanProductRequiredDocumentId == requiredDocumentId &&
            d.IsCurrent, ct);

        if (previous is not null)
        {
            previous.IsCurrent = false;
            await _db.SaveChangesAsync(ct);
        }

        var doc = new LoanApplicationDocument
        {
            LoanApplicationId = applicationId,
            LoanProductRequiredDocumentId = requiredDocumentId,
            OriginalFileName = Path.GetFileName(originalFileName),
            StorageKey = stored.StorageKey,
            ContentType = stored.ContentType,
            FileExtension = stored.Extension,
            FileSizeBytes = stored.SizeBytes,
            Sha256Hash = stored.Sha256Hash,
            Version = (previous?.Version ?? 0) + 1,
            IsCurrent = true,
            ReplacesDocumentId = previous?.Id,
            UploadedByUserId = userId,
            ReviewStatus = DocumentReviewStatus.Pending,
            ScreeningStatus = DocumentScreeningStatus.Queued,
        };
        _db.LoanApplicationDocuments.Add(doc);

        _audit.Record(previous is null ? "DocumentUploaded" : "DocumentReplaced",
            nameof(LoanApplicationDocument), doc.Id.ToString(), null,
            new { ApplicationId = applicationId, doc.Version, doc.Sha256Hash, doc.FileSizeBytes });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _queue.EnqueueAsync(doc.Id, ct);

        return DocumentResult.Ok(doc.Id);
    }

    public async Task<DocumentResult> ReviewAsync(Guid documentId, DocumentReviewStatus outcome,
        string? reasonCode, string? comments, string reviewerId, CancellationToken ct = default)
    {
        var doc = await _db.LoanApplicationDocuments
            .Include(d => d.LoanApplication)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc is null) return DocumentResult.Fail("Document not found.");

        if (!doc.IsCurrent)
            return DocumentResult.Fail("Only the current version of a document can be reviewed.");

        if (!ReviewStatuses.Contains(doc.LoanApplication.Status))
            return DocumentResult.Fail("This application is not open for document review.");

        if (outcome == DocumentReviewStatus.Pending)
            return DocumentResult.Fail("Choose an outcome.");

        if (doc.UploadedByUserId == reviewerId)
            return DocumentResult.Fail("You cannot review a document you uploaded.");

        if (outcome == DocumentReviewStatus.Rejected &&
            (string.IsNullOrWhiteSpace(reasonCode) || !DocumentRejectionReasons.All.Contains(reasonCode)))
            return DocumentResult.Fail("A valid rejection reason is required.");

        if (outcome == DocumentReviewStatus.ClarificationRequired && string.IsNullOrWhiteSpace(comments))
            return DocumentResult.Fail("Explain what clarification is needed.");

        var previous = doc.ReviewStatus;
        doc.ReviewStatus = outcome;
        doc.RejectionReasonCode = outcome == DocumentReviewStatus.Rejected ? reasonCode : null;
        doc.ReviewComments = comments?.Trim();
        doc.ReviewedByUserId = reviewerId;
        doc.ReviewedAt = DateTime.UtcNow;

        _audit.Record("DocumentReviewed", nameof(LoanApplicationDocument), doc.Id.ToString(), null,
            new { From = previous.ToString(), To = outcome.ToString(), Reason = doc.RejectionReasonCode });

        await _db.SaveChangesAsync(ct);
        return DocumentResult.Ok(doc.Id);
    }

    public async Task<(LoanApplicationDocument Document, Stream Content)?> OpenAsync(
        Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.LoanApplicationDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc is null) return null;

        var stream = await _storage.OpenReadAsync(doc.StorageKey, ct);
        return (doc, stream);
    }
}