using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Borrowers
{
    public class BorrowerVerificationDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BorrowerId { get; set; }

        public Borrower Borrower { get; set; } = null!;

        public string DocumentType { get; set; } = string.Empty;

        public string OriginalFileName { get; set; } = string.Empty;

        // Store only the generated file name, not a user-supplied path.
        public string StorageKey { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public string FileExtension { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public string UploadedByUserId { get; set; } = string.Empty;

        public DocumentReviewStatus ReviewStatus { get; set; }
            = DocumentReviewStatus.Pending;

        public string? ReviewComments { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
