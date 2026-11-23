using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Staff;

public class VerificationQueueItem
{
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = "";
    public string BorrowerName { get; set; } = "";
    public LoanApplicationStatus Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public KycStatus KycStatus { get; set; }
    public string EmploymentState { get; set; } = "Not started";
}

public class VerificationCase
{
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = "";
    public LoanApplicationStatus Status { get; set; }

    public string BorrowerName { get; set; } = "";
    public string NationalId { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public KycStatus KycStatus { get; set; }

    public string Employer { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string ContractType { get; set; } = "";
    public DateOnly? StartDate { get; set; }
    public decimal? NetSalary { get; set; }

    public bool CanDoKyc { get; set; }
    public bool CanDoEmployment { get; set; }

    public List<(DateTime At, string Outcome, string By, string? Notes)> Kyc { get; set; } = new();
    public List<(DateTime At, string Outcome, string Channel, string Party, bool Independent, string By, string? Notes)> Attempts { get; set; } = new();
}