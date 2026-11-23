using Kredvanta.Services.Credit;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Staff;

public class CurrentAssessmentView
{
    public CreditRecommendation Recommendation { get; set; }
    public decimal Ratio { get; set; }
    public decimal Income { get; set; }
    public decimal Instalment { get; set; }
    public decimal DisposableBefore { get; set; }
    public string? Flags { get; set; }
    public string? Notes { get; set; }
    public DateTime AssessedAt { get; set; }
    public bool Stale { get; set; }
}

public class AppraisalPage
{
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = "";
    public LoanApplicationStatus Status { get; set; }
    public string BorrowerName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Amount { get; set; }
    public int Term { get; set; }
    public AppraisalPreview Preview { get; set; } = new();
    public AppraisalInput Input { get; set; } = new();
    public CurrentAssessmentView? Current { get; set; }
    public bool CanAssess { get; set; }
    public bool CanRecommend { get; set; }
}