using Kredvanta.Models.Applications;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Documents;

public record DocumentResult(bool Succeeded, string? Error = null, Guid? DocumentId = null)
{
    public static DocumentResult Ok(Guid? id = null) => new(true, null, id);
    public static DocumentResult Fail(string error) => new(false, error);
}

public interface IDocumentService
{
    Task<DocumentResult> UploadAsync(Guid applicationId, Guid requiredDocumentId,
        Stream content, string originalFileName, string userId, CancellationToken ct = default);

    Task<DocumentResult> ReviewAsync(Guid documentId, DocumentReviewStatus outcome,
        string? reasonCode, string? comments, string reviewerId, CancellationToken ct = default);

    Task<(LoanApplicationDocument Document, Stream Content)?> OpenAsync(
        Guid documentId, CancellationToken ct = default);
}