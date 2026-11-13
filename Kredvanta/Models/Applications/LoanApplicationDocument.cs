
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Products;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Applications
{
    public class LoanApplicationDocument : BaseEntity
    {
        public Guid LoanApplicationId { get; set; }

        public LoanApplication LoanApplication { get; set; } = null!;

        public Guid? LoanProductRequiredDocumentId { get; set; }

        public LoanProductRequiredDocument? RequiredDocument { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string StorageKey { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public string FileExtension { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public string UploadedByUserId { get; set; } = string.Empty;

        public ApplicationUser? UploadedByUser { get; set; }

        public DocumentReviewStatus ReviewStatus { get; set; }
            = DocumentReviewStatus.Pending;

        public string? ReviewComments { get; set; }

        public string? ReviewedByUserId { get; set; }

        public ApplicationUser? ReviewedByUser { get; set; }

        public DateTime? ReviewedAt { get; set; }
    }
}
