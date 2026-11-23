using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.LoanApplications;

/// <summary>The current uploaded file for one document requirement, as shown to the applicant.</summary>
public class UploadedDocumentSummary
{
    public string OriginalFileName { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTime UploadedAt { get; set; }

    public DocumentReviewStatus ReviewStatus { get; set; }

    public DocumentScreeningStatus ScreeningStatus { get; set; }

    public string? ReviewComments { get; set; }
}