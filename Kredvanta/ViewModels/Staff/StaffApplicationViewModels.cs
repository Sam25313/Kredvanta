using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Staff;

public class StaffApplicationListItem
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = "";
    public string BorrowerName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal RequestedAmount { get; set; }
    public LoanApplicationStatus Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int PendingDocuments { get; set; }
}

public class StaffApplicationDetails
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = "";
    public LoanApplicationStatus Status { get; set; }
    public string ProductName { get; set; } = "";
    public decimal RequestedAmount { get; set; }
    public int RequestedTermPeriods { get; set; }
    public string LoanPurpose { get; set; } = "";
    public DateTime? SubmittedAt { get; set; }

    public string BorrowerName { get; set; } = "";
    public string BorrowerNumber { get; set; } = "";
    public string KycStatus { get; set; } = "";
    public string EmploymentStatus { get; set; } = "";
    public string Employer { get; set; } = "";
    public string ContractType { get; set; } = "";
    public DateOnly? ContractEndDate { get; set; }
    public decimal? GrossSalary { get; set; }
    public decimal? NetSalary { get; set; }

    public bool CanReviewDocuments { get; set; }
    public bool CanStartReview { get; set; }
    public bool CanRequestDocuments { get; set; }
    public bool CanResumeReview { get; set; }

    public List<StaffDocumentItem> Documents { get; set; } = new();
    public List<StaffHistoryItem> History { get; set; } = new();
}

public class StaffDocumentItem
{
    public Guid Id { get; set; }
    public string RequirementName { get; set; } = "";
    public string FileName { get; set; } = "";
    public int Version { get; set; }
    public bool IsCurrent { get; set; }
    public DocumentReviewStatus ReviewStatus { get; set; }
    public string? RejectionReasonCode { get; set; }
    public string? ReviewComments { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Sha256Hash { get; set; } = "";
    public bool UploadedByCurrentUser { get; set; }
}

public class StaffHistoryItem
{
    public LoanApplicationStatus? From { get; set; }
    public LoanApplicationStatus To { get; set; }
    public DateTime At { get; set; }
    public string By { get; set; } = "";
    public string? Notes { get; set; }
}