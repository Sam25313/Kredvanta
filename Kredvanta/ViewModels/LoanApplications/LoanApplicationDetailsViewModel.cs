using Kredvanta.Models.Enums;
using static Kredvanta.Models.Enums.LendingEnums;


namespace Kredvanta.ViewModels.LoanApplications
{
    public class LoanApplicationDetailsViewModel
    {
        public Guid Id { get; set; }

        public string ApplicationNumber { get; set; } = string.Empty;

        public string LoanProductName { get; set; } = string.Empty;

        public decimal RequestedAmount { get; set; }

        public int RequestedTermPeriods { get; set; }

        public string LoanPurpose { get; set; } = string.Empty;

        public LoanApplicationStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? DecisionAt { get; set; }

        public bool CanUploadDocuments { get; set; }

        public List<ApplicationHistoryItemViewModel> History
        { get; set; } = new();

        public List<ApplicationDocumentItemViewModel> Documents
        { get; set; } = new();
        public List<RequiredApplicationDocumentViewModel> RequiredDocuments { get; set; } = new();
    }

    public class ApplicationHistoryItemViewModel
    {
        public LoanApplicationStatus? PreviousStatus { get; set; }

        public LoanApplicationStatus NewStatus { get; set; }

        public DateTime ChangedAt { get; set; }

        public string? Notes { get; set; }

        public string? ChangedByName { get; set; }
    }

    public class ApplicationDocumentItemViewModel
    {
        public Guid Id { get; set; }

        public Guid? DocumentId { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string? RequiredDocumentName { get; set; }

        public DocumentReviewStatus ReviewStatus { get; set; }

        public string? ReviewComments { get; set; }

        public DateTime UploadedAt { get; set; }

        public int Version { get; set; } = 1;

        public bool IsCurrent { get; set; } = true;
    }
    public class RequiredApplicationDocumentViewModel
    {
        public Guid Id { get; set; }

        public Guid? DocumentId { get; set; }

        public string DocumentName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsMandatory { get; set; }

        public int? RequiredPeriodMonths { get; set; }

        public bool IsUploaded { get; set; }

        public DocumentReviewStatus? ReviewStatus { get; set; }

        public DateTime? UploadedAt { get; set; }

        public bool IsSatisfied { get; set; }

        public int Version { get; set; } = 1;

        public string? BorrowerMessage { get; set; }
        public bool CanUploadDocuments { get; set; }
    }


}