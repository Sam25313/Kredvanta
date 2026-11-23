namespace Kredvanta.Services.Eligibility;

public record EligibilityResult(bool IsEligible, IReadOnlyList<string> Reasons)
{
    public static EligibilityResult Ok() => new(true, []);
    public static EligibilityResult Fail(IEnumerable<string> reasons) => new(false, reasons.ToList());
}

public interface IEligibilityService
{
    Task<EligibilityResult> CheckAsync(Guid borrowerId, CancellationToken ct = default);
}