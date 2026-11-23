using System.ComponentModel.DataAnnotations;

namespace Kredvanta.ViewModels.Credit;

public class CreditPolicyDraftViewModel
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "1.00", ErrorMessage = "Enter a ratio from 0.01 to 1.00.")]
    public decimal MaxDebtServiceRatio { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal MinNetMonthlyIncome { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal MinDisposableAfterInstalment { get; set; }

    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Leave this blank or enter a positive amount.")]
    public decimal? DualApprovalAbove { get; set; }

    [Required]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required, StringLength(20)]
    public string CalculationVersion { get; set; } = "1.0";

    [StringLength(2000)]
    public string? Notes { get; set; }
}
