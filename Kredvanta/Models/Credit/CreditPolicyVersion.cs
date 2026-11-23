using Kredvanta.Models.Common;

namespace Kredvanta.Models.Credit;

public class CreditPolicyVersion : BaseEntity
{
    public int VersionNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public decimal? DualApprovalAbove { get; set; }

    // Maximum (existing + other + new instalment) / net income
    public decimal MaxDebtServiceRatio { get; set; }
    public decimal MinNetMonthlyIncome { get; set; }
    public decimal MinDisposableAfterInstalment { get; set; }

    public string CalculationVersion { get; set; } = "1.0";
    public string? ApprovedBy { get; set; }
    public string? Notes { get; set; }
}