using Kredvanta.Models.Common;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Products;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Applications;

public class LoanApplicationDocument : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public LoanApplication LoanApplication { get; set; } = null!;

    public Guid? LoanProductRequiredDocumentId { get; set; }
    public LoanProductRequiredDocument? RequiredDocument { get; set; }

    // File metadata (original name is display-only; never used for storage paths)
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;

    // Versioning: a replacement is a NEW row; the old one stays as evidence
    public int Version { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public Guid? ReplacesDocumentId { get; set; }
    public LoanApplicationDocument? ReplacesDocument { get; set; }

    public string UploadedByUserId { get; set; } = string.Empty;

    public ApplicationUser? UploadedByUser { get; set; }

    //Docment Screening 
    public DocumentScreeningStatus ScreeningStatus { get; set; } = DocumentScreeningStatus.NotStarted;
    public DateTime? ScreeningStartedAt { get; set; }
    public DateTime? ScreenedAt { get; set; }
    public string? ScreeningSummary { get; set; }
    public string? ScreeningDetailsJson { get; set; }

    // Review
    public DocumentReviewStatus ReviewStatus { get; set; } = DocumentReviewStatus.Pending;
    public string? RejectionReasonCode { get; set; }
    public string? ReviewComments { get; set; }
    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }

    
}