using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Credit;

public class AppraisalInput
{
    public decimal ExistingMonthlyLoanPayments { get; set; }
    public decimal OtherMonthlyObligations { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public bool RecommendWithConditions { get; set; }
    public string? Notes { get; set; }
}

public class AppraisalPreview
{
    public bool Ready { get; set; }
    public string? BlockReason { get; set; }

    public string PolicyName { get; set; } = "";
    public int PolicyVersion { get; set; }
    public decimal? VerifiedNetIncome { get; set; }
    public decimal? DeclaredNetIncome { get; set; }

    public decimal RegularInstalment { get; set; }
    public decimal MonthlyEquivalent { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal TotalRepayable { get; set; }
    public decimal ProcessingFee { get; set; }
    public List<ScheduleLine> Schedule { get; set; } = new();

    public AffordabilityResult? Affordability { get; set; }
    public List<string> Flags { get; set; } = new();
}

public record AppraisalOutcome(bool Succeeded, string? Error = null, CreditRecommendation? Recommendation = null)
{
    public static AppraisalOutcome Fail(string e) => new(false, e);
}

public interface IAppraisalService
{
    Task<AppraisalPreview> PreviewAsync(Guid applicationId, AppraisalInput input, CancellationToken ct = default);
    Task<AppraisalOutcome> AssessAsync(Guid applicationId, AppraisalInput input, string analystId, CancellationToken ct = default);
}